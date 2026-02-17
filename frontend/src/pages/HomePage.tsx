import { Link } from 'react-router-dom';
import { useAppSession } from '../lib/AppContext';

export default function HomePage() {
  const { me } = useAppSession();

  return (
    <div data-testid="home-page" style={{ padding: 24, fontFamily: 'system-ui, sans-serif' }}>
      <h1 data-testid="home-title">CommunityOS UI running</h1>
      {me ? (
        <p data-testid="home-subtitle">Logged in as {me.firstName} {me.lastName} ({me.role})</p>
      ) : (
        <p data-testid="home-subtitle">Not logged in. Use Login to obtain a JWT.</p>
      )}

      <div style={{ display: 'flex', gap: 12, marginTop: 12 }}>
        <Link data-testid="home-feed-link" to="/feed">
          Go to Feed
        </Link>
        <Link data-testid="home-login-link" to="/login">
          Login
        </Link>
      </div>
    </div>
  );
}
