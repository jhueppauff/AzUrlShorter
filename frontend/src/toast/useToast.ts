import { use } from 'react';
import { ToastContext } from './toastContext';
import type { ToastContextValue } from './toastContext';

export function useToast(): ToastContextValue {
  const context = use(ToastContext);

  if (!context) {
    throw new Error('useToast must be used within a ToastProvider.');
  }

  return context;
}
