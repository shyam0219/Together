import { Navigate, useLocation } from 'react-router-dom';
import { useAppSession } from '../lib/AppContext';

export default function RequireAuth({ children }: { children: React.ReactNode }) {
  const { jwt, loadingMe } = useAppSession();
  const loc = useLocation();

  if (loadingMe) {
    return <div data-testid="auth-loading">Loading…</div>;
  }

  if (!jwt) {
    return <Navigate to="/login" replace state={{ from: loc.pathname }} />;
  }

  return <>{children}</>;
}
