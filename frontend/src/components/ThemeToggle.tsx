import { useTheme } from '../hooks/useTheme';
import { Icon } from './Icon';

export function ThemeToggle() {
  const [theme, toggle] = useTheme();
  const nextTheme = theme === 'dark' ? 'light' : 'dark';

  return (
    <button
      type="button"
      className="btn btn--ghost btn--icon"
      onClick={toggle}
      title={`Switch to ${nextTheme} theme`}
      aria-label={`Switch to ${nextTheme} theme`}
    >
      <Icon name={theme === 'dark' ? 'sun' : 'moon'} />
    </button>
  );
}
