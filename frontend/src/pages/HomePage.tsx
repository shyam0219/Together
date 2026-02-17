import { Link } from 'react-router-dom';

export default function HomePage() {
  return (
    <div data-testid="home-page" style={{ padding: 24, fontFamily: 'system-ui, sans-serif' }}>
      <h1 data-testid="home-title">CommunityOS UI running</h1>
      <p data-testid="home-subtitle">Frontend is live. Use Login to obtain a JWT.</p>
      <Link data-testid="home-login-link" to="/login">
        Go to Login
      </Link>
    </div>
  );
}
