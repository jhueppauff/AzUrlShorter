const SAFE_PROTOCOLS = ['http:', 'https:'];

/**
 * Returns the URL when it is a well-formed `http(s)` URL, otherwise `null`.
 * Used to make sure values coming from the API are never rendered as
 * `javascript:` or `data:` links.
 */
export function safeExternalUrl(value: string | undefined | null): string | null {
  if (!value) {
    return null;
  }

  try {
    const parsed = new URL(value);
    return SAFE_PROTOCOLS.includes(parsed.protocol) ? parsed.toString() : null;
  } catch {
    return null;
  }
}

/**
 * Adds an `https://` scheme when the user typed a bare host such as
 * `example.com/docs`.
 */
export function withScheme(value: string): string {
  const trimmed = value.trim();

  if (!trimmed || /^[a-z][a-z0-9+.-]*:/i.test(trimmed)) {
    return trimmed;
  }

  return `https://${trimmed}`;
}

/** Builds the public short link for a domain and key. */
export function buildShortLink(domain: string, key: string): string {
  if (!domain || !key) {
    return '';
  }

  return `https://${domain}/${encodeURIComponent(key)}`;
}

/** Short links may only contain characters that survive a URL path segment. */
export const SHORT_KEY_PATTERN = /^[A-Za-z0-9_-]+$/;

export const SHORT_KEY_MAX_LENGTH = 64;

/** Generates a random, URL-safe short key. */
export function generateShortKey(length = 6): string {
  const alphabet = 'abcdefghijkmnopqrstuvwxyz23456789';
  const values = new Uint32Array(length);
  crypto.getRandomValues(values);

  return Array.from(values, (value) => alphabet[value % alphabet.length]).join('');
}

/** Formats a Table Storage timestamp for display; falls back to an em dash. */
export function formatTimestamp(timestamp: string | undefined): string {
  if (!timestamp) {
    return '—';
  }

  const parsed = new Date(timestamp);

  if (Number.isNaN(parsed.getTime())) {
    return '—';
  }

  return parsed.toLocaleDateString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  });
}
