import { Navigate, useLocation } from 'react-router-dom';
import { Icon } from '../components/Icon';
import { useAuth } from '../auth/useAuth';

interface LocationState {
  from?: string;
}

export function LoginPage() {
  const { user, isLoading, signIn } = useAuth();
  const location = useLocation();
  const from = (location.state as LocationState | null)?.from ?? '/';

  if (isLoading) {
    return (
      <div className="centered" role="status" aria-live="polite">
        <div className="centered__card">
          <span className="spinner" style={{ fontSize: '1.5rem' }} aria-hidden="true" />
          <p className="centered__text">Checking your session…</p>
        </div>
      </div>
    );
  }

  if (user) {
    return <Navigate to={from} replace />;
  }

  return (
    <div className="centered">
      <div className="centered__card">
        <span className="state__icon">
          <Icon name="lock" size={24} />
        </span>
        <h1 className="page-header__title">Sign in to continue</h1>
        <p className="centered__text">
          Short links are tied to your account, so you need to sign in before creating or managing
          them.
        </p>
        <button type="button" className="btn btn--primary" onClick={() => signIn(from)}>
          Sign in with Microsoft Entra ID
        </button>
      </div>
    </div>
  );
}
