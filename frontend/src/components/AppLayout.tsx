import { NavLink, Link, Outlet } from 'react-router-dom';
import { Icon } from './Icon';
import { ThemeToggle } from './ThemeToggle';
import { useAuth } from '../auth/useAuth';

function initials(name: string): string {
  const cleaned = name.replace(/@.*$/, '').replace(/[^A-Za-z0-9 .-]/g, ' ');
  const parts = cleaned.split(/[\s.-]+/).filter(Boolean);

  if (parts.length === 0) {
    return '?';
  }

  return (parts[0][0] + (parts[1]?.[0] ?? '')).toUpperCase();
}

export function AppLayout() {
  const { user, signIn, signOut } = useAuth();

  return (
    <div className="app-shell">
      <a className="skip-link" href="#main-content">
        Skip to content
      </a>

      <header className="app-header">
        <div className="app-header__inner">
          <Link className="brand" to="/">
            <span className="brand__mark" aria-hidden="true">
              <Icon name="link" size={18} />
            </span>
            Url Shorter
          </Link>

          {user && (
            <nav className="app-nav" aria-label="Main">
              <NavLink className="app-nav__link" to="/" end>
                <Icon name="plus" size={16} className="app-nav__icon" />
                <span>Create</span>
              </NavLink>
              <NavLink className="app-nav__link" to="/links">
                <Icon name="list" size={16} className="app-nav__icon" />
                <span>My links</span>
              </NavLink>
            </nav>
          )}

          <div className="app-header__spacer" />

          <div className="app-header__actions">
            <ThemeToggle />
            {user ? (
              <>
                <span className="user-chip">
                  <span className="user-chip__avatar" aria-hidden="true">
                    {initials(user.userDetails)}
                  </span>
                  <span className="user-chip__name" title={user.userDetails}>
                    {user.userDetails}
                  </span>
                </span>
                <button type="button" className="btn btn--secondary btn--small" onClick={signOut}>
                  Sign out
                </button>
              </>
            ) : (
              <button
                type="button"
                className="btn btn--primary btn--small"
                onClick={() => signIn('/')}
              >
                Sign in
              </button>
            )}
          </div>
        </div>
      </header>

      <main className="app-main" id="main-content">
        <Outlet />
      </main>

      <footer className="app-footer">
        <div className="app-footer__inner">
          <span>Url Shorter — Azure Static Web Apps &amp; Table Storage</span>
          <a href="https://github.com/jhueppauff/AzUrlShorter" target="_blank" rel="noreferrer">
            Source on GitHub
          </a>
        </div>
      </footer>
    </div>
  );
}
