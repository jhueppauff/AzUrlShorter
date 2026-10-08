import { useEffect, useRef, useState } from 'react';
import { Icon } from './Icon';
import { useToast } from '../toast/useToast';

interface CopyButtonProps {
  value: string;
  label?: string;
  className?: string;
}

/** Copies a value to the clipboard and confirms with an inline state change. */
export function CopyButton({ value, label = 'Copy', className }: CopyButtonProps) {
  const [copied, setCopied] = useState(false);
  const timer = useRef<ReturnType<typeof setTimeout> | undefined>(undefined);
  const { notify } = useToast();

  useEffect(() => () => clearTimeout(timer.current), []);

  const copy = async () => {
    try {
      if (!navigator.clipboard) {
        throw new Error('Clipboard unavailable');
      }

      await navigator.clipboard.writeText(value);
      setCopied(true);
      clearTimeout(timer.current);
      timer.current = setTimeout(() => setCopied(false), 2000);
    } catch {
      notify('Copying is blocked by your browser. Select the link and copy it manually.', 'error');
    }
  };

  return (
    <button
      type="button"
      className={className ?? 'btn btn--secondary btn--small'}
      onClick={copy}
      disabled={!value}
    >
      <Icon name={copied ? 'check' : 'copy'} size={16} />
      {copied ? 'Copied' : label}
    </button>
  );
}
