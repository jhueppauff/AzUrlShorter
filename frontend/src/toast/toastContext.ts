import { createContext } from 'react';

export type ToastTone = 'success' | 'error' | 'info';

export interface Toast {
  id: number;
  tone: ToastTone;
  message: string;
}

export interface ToastContextValue {
  /** Shows a toast and returns its id. */
  notify: (message: string, tone?: ToastTone) => number;
  /** Dismisses a toast early. */
  dismiss: (id: number) => void;
}

export const ToastContext = createContext<ToastContextValue | undefined>(undefined);
