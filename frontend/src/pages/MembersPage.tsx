import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { apiFetch } from '../lib/api';
import type { MemberDto } from '../lib/types';

export default function MembersPage() {
  const [q, setQ] = useState('');
  const [members, setMembers] = useState<MemberDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      const path = q.trim() ? `/v1/members?q=${encodeURIComponent(q.trim())}` : '/v1/members';
      const data = await apiFetch<MemberDto[]>(path);
      setMembers(data);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return (
    <div data-testid="members-page" style={{ padding: 16, fontFamily: 'system-ui, sans-serif', maxWidth: 900, margin: '0 auto' }}>
      <h1 data-testid="members-title">Members</h1>

      <div data-testid="members-search" style={{ display: 'flex', gap: 10, alignItems: 'center', marginBottom: 12 }}>
        <input data-testid="members-search-input" value={q} onChange={(e) => setQ(e.target.value)} placeholder="Search by name or email" />
        <button data-testid="members-search-button" onClick={() => void load()} disabled={loading}>
          Search
        </button>
      </div>

      {error ? (
        <div data-testid="members-error" style={{ color: 'crimson', whiteSpace: 'pre-wrap' }}>
          {error}
        </div>
      ) : null}

      {loading ? <div data-testid="members-loading">Loading…</div> : null}

      <div data-testid="members-list" style={{ display: 'grid', gap: 10 }}>
        {members.map((m) => (
          <div key={m.userId} data-testid={`member-card-${m.userId}`} style={{ border: '1px solid #e5e7eb', borderRadius: 8, padding: 12 }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', gap: 10 }}>
              <div>
                <div data-testid={`member-name-${m.userId}`} style={{ fontWeight: 800 }}>
                  {m.firstName} {m.lastName}
                </div>
                <div data-testid={`member-email-${m.userId}`} style={{ color: '#6b7280', fontSize: 12 }}>
                  {m.email}
                </div>
              </div>
              <Link data-testid={`member-open-link-${m.userId}`} to={`/members/${m.userId}`}>
                View
              </Link>
            </div>
            <div style={{ marginTop: 8, display: 'flex', gap: 12, flexWrap: 'wrap' }}>
              <span data-testid={`member-role-${m.userId}`}>Role: {m.role}</span>
              <span data-testid={`member-status-${m.userId}`}>Status: {m.status}</span>
              {m.city ? <span data-testid={`member-city-${m.userId}`}>City: {m.city}</span> : null}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
