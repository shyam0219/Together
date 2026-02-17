import { Navigate, useLocation } from 'react-router-dom';
import { useAppSession } from '../lib/AppContext';

export default function RequireAuth({ children }: { children: React.ReactNode }) {
  const { jwt, loadingMe } = useAppSession();
  const loc = useLocation();

  // Use localStorage fallback so post-login navigation doesn’t race React state.
  const effectiveJwt = jwt ?? localStorage.getItem('communityos_jwt');

  if (loadingMe) {
    return <div data-testid="auth-loading">Loading…</div>;
  }

  if (!effectiveJwt) {
    return <Navigate to="/login" replace state={{ from: loc.pathname }} />;
  }

  return <>{children}</>;
}
