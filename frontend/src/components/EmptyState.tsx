import type { ReactNode } from 'react';
import { Icon } from './Icon';
import type { IconName } from './Icon';

interface EmptyStateProps {
  icon?: IconName;
  title: string;
  description?: string;
  children?: ReactNode;
}

export function EmptyState({ icon = 'link', title, description, children }: EmptyStateProps) {
  return (
    <div className="state">
      <span className="state__icon">
        <Icon name={icon} size={24} />
      </span>
      <p className="state__title">{title}</p>
      {description && <p>{description}</p>}
      {children && <div className="state__actions">{children}</div>}
    </div>
  );
}
