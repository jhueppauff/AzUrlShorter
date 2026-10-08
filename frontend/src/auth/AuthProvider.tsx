import { useCallback, useEffect, useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import { getClientPrincipal } from '../api/client';
import type { ClientPrincipal } from '../types';
import { AuthContext } from './authContext';

/** The identity provider configured for this Static Web App. */
const IDENTITY_PROVIDER = 'aad';

/**
 * Keeps the redirect target inside this app so a crafted link cannot bounce the
 * user to an external site after signing in.
 */
function toSafeRedirect(target: string | undefined): string {
  if (!target || !target.startsWith('/') || target.startsWith('//')) {
    return '/';
  }

  return target;
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<ClientPrincipal | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  const load = useCallback(async (signal?: AbortSignal) => {
    const principal = await getClientPrincipal(signal);

    if (!signal?.aborted) {
      setUser(principal);
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    const controller = new AbortController();

    void (async () => {
      await load(controller.signal);
    })();

    return () => controller.abort();
  }, [load]);

  const value = useMemo(
    () => ({
      user,
      isLoading,
      refresh: async () => {
        await load();
      },
      signIn: (redirectTo?: string) => {
        const target = toSafeRedirect(redirectTo);
        window.location.assign(
          `/.auth/login/${IDENTITY_PROVIDER}?post_login_redirect_uri=${encodeURIComponent(target)}`,
        );
      },
      signOut: () => {
        window.location.assign('/.auth/logout?post_logout_redirect_uri=/');
      },
    }),
    [isLoading, load, user],
  );

  return <AuthContext value={value}>{children}</AuthContext>;
}
