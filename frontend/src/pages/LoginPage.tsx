import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { apiFetch } from '../lib/api';

type AuthResponse = { token: string };

export default function LoginPage() {
  const navigate = useNavigate();
  const [tenantCode, setTenantCode] = useState<'SE' | 'IT'>('SE');
  const [email, setEmail] = useState('admin.se@community.local');
  const [password, setPassword] = useState('Password123!');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      const data = await apiFetch<AuthResponse>('/api/v1/auth/login', {
        method: 'POST',
        body: JSON.stringify({ email, password, tenantCode }),
      });
      localStorage.setItem('communityos_jwt', data.token);
      navigate('/');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Login failed');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div data-testid="login-page" style={{ padding: 24, fontFamily: 'system-ui, sans-serif', maxWidth: 420 }}>
      <h1 data-testid="login-title">Login</h1>

      <form data-testid="login-form" onSubmit={onSubmit} style={{ display: 'grid', gap: 12 }}>
        <label>
          Tenant
          <select
            data-testid="login-tenant-select"
            value={tenantCode}
            onChange={(e) => setTenantCode(e.target.value as 'SE' | 'IT')}
          >
            <option value="SE">Sweden (SE)</option>
            <option value="IT">Italy (IT)</option>
          </select>
        </label>

        <label>
          Email
          <input
            data-testid="login-email-input"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            type="email"
            autoComplete="email"
          />
        </label>

        <label>
          Password
          <input
            data-testid="login-password-input"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            type="password"
            autoComplete="current-password"
          />
        </label>

        {error ? (
          <div data-testid="login-error" style={{ color: 'crimson', whiteSpace: 'pre-wrap' }}>
            {error}
          </div>
        ) : null}

        <button data-testid="login-submit-button" type="submit" disabled={loading}>
          {loading ? 'Logging in…' : 'Login'}
        </button>
      </form>

      <div style={{ marginTop: 16 }}>
        <Link data-testid="login-home-link" to="/">
          Back to Home
        </Link>
      </div>
    </div>
  );
}
