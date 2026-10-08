import { createContext } from 'react';
import type { ClientPrincipal } from '../types';

export interface AuthContextValue {
  /** The signed-in user, or `null` when anonymous. */
  user: ClientPrincipal | null;
  /** True while the initial `/.auth/me` lookup is in flight. */
  isLoading: boolean;
  /** Re-reads the current user from the auth endpoint. */
  refresh: () => Promise<void>;
  /** Starts the Static Web Apps sign-in flow. */
  signIn: (redirectTo?: string) => void;
  /** Starts the Static Web Apps sign-out flow. */
  signOut: () => void;
}

export const AuthContext = createContext<AuthContextValue | undefined>(undefined);
