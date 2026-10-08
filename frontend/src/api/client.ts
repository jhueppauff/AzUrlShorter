import type { ClientPrincipal, ConfigurationEntry, LinkUsage, ShortUrl } from '../types';

/**
 * Base address of the API. Defaults to the current origin so the app works with
 * the Azure Static Web Apps managed functions without extra configuration.
 */
const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL ?? '').replace(/\/+$/, '');

export class ApiError extends Error {
  readonly status: number;

  constructor(message: string, status: number) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
  }

  /** True when the caller is not signed in or lacks access to the API. */
  get isAuthError(): boolean {
    return this.status === 401 || this.status === 403;
  }
}

function readString(source: Record<string, unknown>, key: string): string {
  // The API has been served by both System.Text.Json (camelCase) and
  // Newtonsoft.Json (PascalCase), so accept either spelling.
  const value = source[key] ?? source[key.charAt(0).toUpperCase() + key.slice(1)];
  return typeof value === 'string' ? value : '';
}

function toShortUrl(raw: unknown): ShortUrl {
  const source = (raw ?? {}) as Record<string, unknown>;
  const timestamp = readString(source, 'timestamp');

  return {
    partitionKey: readString(source, 'partitionKey'),
    rowKey: readString(source, 'rowKey'),
    url: readString(source, 'url'),
    timestamp: timestamp || undefined,
  };
}

function toConfigurationEntry(raw: unknown): ConfigurationEntry {
  const source = (raw ?? {}) as Record<string, unknown>;

  return {
    partitionKey: readString(source, 'partitionKey'),
    rowKey: readString(source, 'rowKey'),
    value: readString(source, 'value') || undefined,
  };
}

async function readErrorMessage(response: Response): Promise<string | undefined> {
  // The API reports actionable failures (missing configuration, a taken short key)
  // as the response body, so show that instead of a bare status code.
  try {
    const text = (await response.text()).trim();

    if (!text) {
      return undefined;
    }

    if (text.startsWith('{') || text.startsWith('[')) {
      const parsed: unknown = JSON.parse(text);
      const detail = (parsed as Record<string, unknown>)?.detail ?? (parsed as Record<string, unknown>)?.message;
      return typeof detail === 'string' && detail.trim() ? detail.trim() : undefined;
    }

    return text.length > 300 ? undefined : text;
  } catch {
    return undefined;
  }
}

async function request(path: string, init?: RequestInit): Promise<Response> {
  let response: Response;

  try {
    response = await fetch(`${API_BASE_URL}${path}`, {
      credentials: 'include',
      ...init,
      headers: { Accept: 'application/json', ...init?.headers },
    });
  } catch {
    throw new ApiError('Unable to reach the server. Check your connection and try again.', 0);
  }

  if (!response.ok) {
    if (response.status === 401 || response.status === 403) {
      throw new ApiError('Your session has expired. Please sign in again.', response.status);
    }

    const detail = await readErrorMessage(response);

    throw new ApiError(detail ?? `The server responded with ${response.status}.`, response.status);
  }

  return response;
}

async function requestJson<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await request(path, init);

  try {
    return (await response.json()) as T;
  } catch {
    throw new ApiError('The server returned an unexpected response.', response.status);
  }
}

/** Lists the domains that short links can be created on. */
export async function getDomains(signal?: AbortSignal): Promise<ConfigurationEntry[]> {
  const data = await requestJson<unknown>('/api/Domains', { signal });
  return Array.isArray(data) ? data.map(toConfigurationEntry) : [];
}

/** Lists the short links owned by the signed-in user. */
export async function getLinks(signal?: AbortSignal): Promise<ShortUrl[]> {
  const data = await requestJson<unknown>('/api/Links', { signal });
  return Array.isArray(data) ? data.map(toShortUrl) : [];
}

/** Reads usage separately so analytics never delays loading or managing links. */
export async function getLinksUsage(signal?: AbortSignal): Promise<LinkUsage[]> {
  const data = await requestJson<unknown>('/api/Links/Usage', { signal });

  if (!Array.isArray(data)) {
    throw new ApiError('The server returned unexpected usage data.', 502);
  }

  return data.map((raw: unknown) => {
    const source = (raw ?? {}) as Record<string, unknown>;
    const uses = source.uses ?? source.Uses;
    const partitionKey = readString(source, 'partitionKey');
    const rowKey = readString(source, 'rowKey');

    if (!partitionKey || !rowKey || typeof uses !== 'number' || !Number.isFinite(uses) || uses < 0) {
      throw new ApiError('The server returned unexpected usage data.', 502);
    }

    return {
      partitionKey,
      rowKey,
      uses,
      lastUsed: readString(source, 'lastUsed') || undefined,
    };
  });
}

/** Creates a new short link. */
export async function createLink(shortUrl: ShortUrl, signal?: AbortSignal): Promise<void> {
  await request('/api/Links', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      partitionKey: shortUrl.partitionKey,
      rowKey: shortUrl.rowKey,
      url: shortUrl.url,
    }),
    signal,
  });
}

/** Deletes an existing short link. */
export async function deleteLink(shortUrl: ShortUrl, signal?: AbortSignal): Promise<void> {
  const path = `/api/Links/${encodeURIComponent(shortUrl.partitionKey)}/${encodeURIComponent(shortUrl.rowKey)}`;
  await request(path, { method: 'DELETE', signal });
}

/**
 * Reads the current user from the Azure Static Web Apps auth endpoint.
 * Returns `null` when the user is anonymous or when the endpoint is unavailable
 * (for example when running `vite dev` without the Static Web Apps CLI).
 */
export async function getClientPrincipal(signal?: AbortSignal): Promise<ClientPrincipal | null> {
  let response: Response;

  try {
    response = await fetch('/.auth/me', {
      credentials: 'include',
      headers: { Accept: 'application/json' },
      signal,
    });
  } catch {
    return null;
  }

  if (!response.ok) {
    return null;
  }

  try {
    const payload = (await response.json()) as { clientPrincipal?: ClientPrincipal | null };
    return payload?.clientPrincipal ?? null;
  } catch {
    return null;
  }
}
