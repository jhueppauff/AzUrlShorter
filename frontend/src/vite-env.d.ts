/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** Optional absolute base URL of the API. Defaults to the current origin. */
  readonly VITE_API_BASE_URL?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
