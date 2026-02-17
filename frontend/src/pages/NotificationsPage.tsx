import { useEffect, useState } from 'react';
import { apiFetch } from '../lib/api';
import type { NotificationDto, PageResponse } from '../lib/types';

export default function NotificationsPage() {
  const [page, setPage] = useState(1);
  const [pageSize] = useState(20);
  const [items, setItems] = useState<NotificationDto[]>([]);
  const [hasMore, setHasMore] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      const data = await apiFetch<PageResponse<NotificationDto>>(`/v1/notifications?page=${page}&pageSize=${pageSize}`);
      setItems(data.items);
      setHasMore(data.hasMore);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page]);

  async function markRead(id: string) {
    setError(null);
    try {
      await apiFetch<void>(`/v1/notifications/${id}/read`, { method: 'POST' });
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed');
    }
  }

  return (
    <div data-testid="notifications-page" style={{ padding: 16, fontFamily: 'system-ui, sans-serif', maxWidth: 900, margin: '0 auto' }}>
      <h1 data-testid="notifications-title">Notifications</h1>

      <div data-testid="notifications-controls" style={{ display: 'flex', gap: 12, alignItems: 'center', marginBottom: 10 }}>
        <button data-testid="notifications-prev-page-button" onClick={() => setPage((p) => Math.max(1, p - 1))} disabled={page <= 1}>
          Prev
        </button>
        <div data-testid="notifications-page-indicator">Page {page}</div>
        <button data-testid="notifications-next-page-button" onClick={() => setPage((p) => p + 1)} disabled={!hasMore}>
          Next
        </button>
        <button data-testid="notifications-refresh-button" onClick={() => void load()} disabled={loading}>
          Refresh
        </button>
      </div>

      {error ? (
        <div data-testid="notifications-error" style={{ color: 'crimson', whiteSpace: 'pre-wrap' }}>
          {error}
        </div>
      ) : null}
      {loading ? <div data-testid="notifications-loading">Loading…</div> : null}

      <div data-testid="notifications-list" style={{ display: 'grid', gap: 10 }}>
        {items.map((n) => (
          <div key={n.notificationId} data-testid={`notification-card-${n.notificationId}`} style={{ border: '1px solid #e5e7eb', borderRadius: 8, padding: 12 }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', gap: 12 }}>
              <div>
                <div data-testid={`notification-type-${n.notificationId}`} style={{ fontWeight: 800 }}>
                  {n.type}
                </div>
                <div data-testid={`notification-created-${n.notificationId}`} style={{ color: '#6b7280', fontSize: 12 }}>
                  {new Date(n.createdAt).toLocaleString()}
                </div>
              </div>
              <div>
                <span data-testid={`notification-read-${n.notificationId}`}>read={String(n.isRead)}</span>
              </div>
            </div>

            <pre data-testid={`notification-payload-${n.notificationId}`} style={{ marginTop: 8, background: '#f9fafb', padding: 8, borderRadius: 8, overflow: 'auto' }}>
              {n.payloadJson}
            </pre>

            {!n.isRead ? (
              <button data-testid={`notification-mark-read-${n.notificationId}`} onClick={() => void markRead(n.notificationId)}>
                Mark read
              </button>
            ) : null}
          </div>
        ))}
      </div>
    </div>
  );
}
