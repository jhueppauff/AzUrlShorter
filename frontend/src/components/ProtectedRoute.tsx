import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from '../auth/useAuth';

/** Renders nested routes only for signed-in users. */
export function ProtectedRoute() {
  const { user, isLoading } = useAuth();
  const location = useLocation();

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

  if (!user) {
    return <Navigate to="/login" replace state={{ from: location.pathname + location.search }} />;
  }

  return <Outlet />;
}
