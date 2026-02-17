import { useEffect, useState } from 'react';
import { apiFetch } from '../lib/api';
import { isModOrAdmin, useAppSession } from '../lib/AppContext';
import type { PageResponse, ReportDto } from '../lib/types';

type ModActionBody = { actionType: string; targetType: string; targetId: string; notes: string | null };

export default function ModerationPage() {
  const { me } = useAppSession();
  const allowed = isModOrAdmin(me?.role);

  const [page, setPage] = useState(1);
  const [pageSize] = useState(50);
  const [items, setItems] = useState<ReportDto[]>([]);
  const [hasMore, setHasMore] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      const data = await apiFetch<PageResponse<ReportDto>>(`/v1/mod/reports?page=${page}&pageSize=${pageSize}`);
      setItems(data.items);
      setHasMore(data.hasMore);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    if (!allowed) return;
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [allowed, page]);

  async function action(r: ReportDto, actionType: string) {
    setError(null);
    const payload: ModActionBody = {
      actionType,
      targetType: r.targetType,
      targetId: r.targetId,
      notes: null,
    };

    try {
      await apiFetch<void>(`/v1/mod/reports/${r.reportId}/action`, { method: 'POST', body: JSON.stringify(payload) });
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed');
    }
  }

  if (!allowed) {
    return (
      <div data-testid="moderation-page" style={{ padding: 16, fontFamily: 'system-ui, sans-serif' }}>
        <h1 data-testid="moderation-title">Moderation</h1>
        <div data-testid="moderation-forbidden">You do not have access.</div>
      </div>
    );
  }

  return (
    <div data-testid="moderation-page" style={{ padding: 16, fontFamily: 'system-ui, sans-serif', maxWidth: 1100, margin: '0 auto' }}>
      <h1 data-testid="moderation-title">Moderation</h1>

      <div data-testid="moderation-controls" style={{ display: 'flex', gap: 12, alignItems: 'center', marginBottom: 10 }}>
        <button data-testid="moderation-prev-page-button" onClick={() => setPage((p) => Math.max(1, p - 1))} disabled={page <= 1}>
          Prev
        </button>
        <div data-testid="moderation-page-indicator">Page {page}</div>
        <button data-testid="moderation-next-page-button" onClick={() => setPage((p) => p + 1)} disabled={!hasMore}>
          Next
        </button>
        <button data-testid="moderation-refresh-button" onClick={() => void load()} disabled={loading}>
          Refresh
        </button>
      </div>

      {error ? (
        <div data-testid="moderation-error" style={{ color: 'crimson', whiteSpace: 'pre-wrap' }}>
          {error}
        </div>
      ) : null}
      {loading ? <div data-testid="moderation-loading">Loading…</div> : null}

      <div data-testid="reports-list" style={{ display: 'grid', gap: 10 }}>
        {items.map((r) => (
          <div key={r.reportId} data-testid={`report-card-${r.reportId}`} style={{ border: '1px solid #e5e7eb', borderRadius: 8, padding: 12 }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', gap: 12 }}>
              <div>
                <div data-testid={`report-status-${r.reportId}`} style={{ fontWeight: 800 }}>
                  {r.status}
                </div>
                <div data-testid={`report-target-${r.reportId}`} style={{ color: '#6b7280', fontSize: 12 }}>
                  {r.targetType} • {r.targetId}
                </div>
                <div data-testid={`report-reason-${r.reportId}`} style={{ marginTop: 6 }}>
                  {r.reason}
                </div>
              </div>
              <div data-testid={`report-created-${r.reportId}`} style={{ color: '#6b7280', fontSize: 12 }}>
                {new Date(r.createdAt).toLocaleString()}
              </div>
            </div>

            <div style={{ marginTop: 10, display: 'flex', gap: 10, flexWrap: 'wrap' }}>
              <button data-testid={`report-action-reviewed-${r.reportId}`} onClick={() => void action(r, 'Reviewed')}>
                Mark reviewed
              </button>
              <button data-testid={`report-action-actioned-${r.reportId}`} onClick={() => void action(r, 'Actioned')}>
                Mark actioned
              </button>
              {r.targetType === 'Post' ? (
                <>
                  <button data-testid={`report-action-hide-${r.reportId}`} onClick={() => void action(r, 'Hide')}>
                    Hide post
                  </button>
                  <button data-testid={`report-action-remove-${r.reportId}`} onClick={() => void action(r, 'Remove')}>
                    Remove post
                  </button>
                </>
              ) : null}
            </div>
          </div>
        ))}
      </div>

      <div data-testid="admin-user-actions" style={{ marginTop: 20, borderTop: '1px solid #e5e7eb', paddingTop: 12 }}>
        <h2 data-testid="admin-user-actions-title">Suspend/Ban user (by userId)</h2>
        <UserSuspendBan />
      </div>
    </div>
  );
}

function UserSuspendBan() {
  const [userId, setUserId] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [ok, setOk] = useState<string | null>(null);

  async function suspend() {
    setError(null);
    setOk(null);
    try {
      await apiFetch<void>(`/v1/mod/users/${userId}/suspend`, { method: 'POST' });
      setOk('Suspended');
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed');
    }
  }

  async function ban() {
    setError(null);
    setOk(null);
    try {
      await apiFetch<void>(`/v1/mod/users/${userId}/ban`, { method: 'POST' });
      setOk('Banned');
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed');
    }
  }

  return (
    <div data-testid="suspend-ban-card" style={{ display: 'grid', gap: 10, maxWidth: 520 }}>
      <input data-testid="suspend-ban-userid-input" value={userId} onChange={(e) => setUserId(e.target.value)} placeholder="UserId (GUID)" />
      <div style={{ display: 'flex', gap: 10 }}>
        <button data-testid="suspend-user-button" onClick={() => void suspend()} disabled={!userId.trim()}>
          Suspend
        </button>
        <button data-testid="ban-user-button" onClick={() => void ban()} disabled={!userId.trim()}>
          Ban
        </button>
      </div>
      {error ? (
        <div data-testid="suspend-ban-error" style={{ color: 'crimson', whiteSpace: 'pre-wrap' }}>
          {error}
        </div>
      ) : null}
      {ok ? <div data-testid="suspend-ban-success">{ok}</div> : null}
    </div>
  );
}
