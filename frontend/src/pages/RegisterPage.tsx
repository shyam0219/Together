import { useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { useAppSession } from '../lib/AppContext';
import { apiFetch } from '../lib/api';

type AuthResponse = { token: string };

export default function RegisterPage() {
  const navigate = useNavigate();
  const loc = useLocation() as { state?: { from?: string } };
  const { refreshMe } = useAppSession();
  const [tenantCode, setTenantCode] = useState<'SE' | 'IT'>('SE');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      const data = await apiFetch<AuthResponse>('/v1/auth/register', {
        method: 'POST',
        body: JSON.stringify({ email, password, firstName, lastName, tenantCode }),
      });
      localStorage.setItem('communityos_jwt', data.token);
      window.dispatchEvent(new Event('communityos:auth-changed'));
      navigate('/');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Registration failed');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div data-testid="register-page" style={{ padding: 24, fontFamily: 'system-ui, sans-serif', maxWidth: 520 }}>
      <h1 data-testid="register-title">Register</h1>

      <form data-testid="register-form" onSubmit={onSubmit} style={{ display: 'grid', gap: 12 }}>
        <label>
          Tenant
          <select
            data-testid="register-tenant-select"
            value={tenantCode}
            onChange={(e) => setTenantCode(e.target.value as 'SE' | 'IT')}
          >
            <option value="SE">Sweden (SE)</option>
            <option value="IT">Italy (IT)</option>
          </select>
        </label>

        <label>
          First name
          <input
            data-testid="register-first-name-input"
            value={firstName}
            onChange={(e) => setFirstName(e.target.value)}
            type="text"
            autoComplete="given-name"
          />
        </label>

        <label>
          Last name
          <input
            data-testid="register-last-name-input"
            value={lastName}
            onChange={(e) => setLastName(e.target.value)}
            type="text"
            autoComplete="family-name"
          />
        </label>

        <label>
          Email
          <input
            data-testid="register-email-input"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            type="email"
            autoComplete="email"
          />
        </label>

        <label>
          Password
          <input
            data-testid="register-password-input"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            type="password"
            autoComplete="new-password"
          />
        </label>

        {error ? (
          <div data-testid="register-error" style={{ color: 'crimson', whiteSpace: 'pre-wrap' }}>
            {error}
          </div>
        ) : null}

        <button data-testid="register-submit-button" type="submit" disabled={loading}>
          {loading ? 'Creating…' : 'Create account'}
        </button>
      </form>

      <div style={{ marginTop: 16, display: 'flex', gap: 12 }}>
        <Link data-testid="register-login-link" to="/login">
          Back to Login
        </Link>
        <Link data-testid="register-home-link" to="/">
          Home
        </Link>
      </div>
    </div>
  );
}
