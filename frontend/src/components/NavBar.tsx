import { Link, useNavigate } from 'react-router-dom';
import { isModOrAdmin, useAppSession } from '../lib/AppContext';

export default function NavBar() {
  const nav = useNavigate();
  const { me, jwt, logout, refreshMe } = useAppSession();

  return (
    <div
      data-testid="app-navbar"
      style={{
        display: 'flex',
        gap: 12,
        alignItems: 'center',
        padding: '12px 16px',
        borderBottom: '1px solid #e5e7eb',
        fontFamily: 'system-ui, sans-serif',
      }}
    >
      <Link data-testid="nav-home-link" to="/">
        CommunityOS
      </Link>
      <Link data-testid="nav-feed-link" to="/feed">
        Feed
      </Link>
      <Link data-testid="nav-groups-link" to="/groups">
        Groups
      </Link>
      <Link data-testid="nav-members-link" to="/members">
        Members
      </Link>
      <Link data-testid="nav-notifications-link" to="/notifications">
        Notifications
      </Link>

      {isModOrAdmin(me?.role) ? (
        <Link data-testid="nav-moderation-link" to="/moderation">
          Moderation
        </Link>
      ) : null}

      <div style={{ marginLeft: 'auto', display: 'flex', gap: 10, alignItems: 'center' }}>
        {jwt && me ? (
          <>
            <span data-testid="nav-me-label" style={{ color: '#374151' }}>
              {me.firstName} {me.lastName} ({me.role})
            </span>
            <button data-testid="nav-refresh-me-button" onClick={() => void refreshMe()}>
              Refresh
            </button>
            <button
              data-testid="nav-logout-button"
              onClick={() => {
                logout();
                nav('/login');
              }}
            >
              Logout
            </button>
          </>
        ) : (
          <>
            <Link data-testid="nav-login-link" to="/login">
              Login
            </Link>
            <Link data-testid="nav-register-link" to="/register">
              Register
            </Link>
          </>
        )}
      </div>
    </div>
  );
}
