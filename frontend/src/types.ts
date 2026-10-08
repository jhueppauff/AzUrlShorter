/** A short link as stored in Azure Table Storage. */
export interface ShortUrl {
  /** The short key, e.g. `docs` in `https://example.com/docs`. */
  partitionKey: string;
  /** The domain the short link is served from, e.g. `example.com`. */
  rowKey: string;
  /** The target the short link redirects to. */
  url: string;
  /** Creation/modification timestamp assigned by Table Storage. */
  timestamp?: string;
}

/** Best-effort resolutions recorded by analytics over the last 30 days. */
export interface LinkUsage {
  partitionKey: string;
  rowKey: string;
  uses: number;
  lastUsed?: string;
}

/** A configuration entry; used for the list of selectable domains. */
export interface ConfigurationEntry {
  partitionKey: string;
  rowKey: string;
  value?: string;
}

/** The user information exposed by Azure Static Web Apps on `/.auth/me`. */
export interface ClientPrincipal {
  identityProvider: string;
  userId: string;
  userDetails: string;
  userRoles: string[];
}
