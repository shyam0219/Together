import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { apiFetch } from '../lib/api';
import type { GroupDto } from '../lib/types';

type CreateGroupBody = { name: string; description: string | null; visibility: string };

export default function GroupsPage() {
  const [groups, setGroups] = useState<GroupDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [visibility, setVisibility] = useState<'Public' | 'Private'>('Public');

  async function load() {
    setLoading(true);
    setError(null);
    try {
      const data = await apiFetch<GroupDto[]>('/v1/groups');
      setGroups(data);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
  }, []);

  async function create() {
    setError(null);
    const payload: CreateGroupBody = { name: name.trim(), description: description.trim() ? description.trim() : null, visibility };
    try {
      await apiFetch<GroupDto>('/v1/groups', { method: 'POST', body: JSON.stringify(payload) });
      setName('');
      setDescription('');
      setVisibility('Public');
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed');
    }
  }

  async function join(g: GroupDto) {
    try {
      await apiFetch<void>(`/v1/groups/${g.groupId}/join`, { method: 'POST' });
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed');
    }
  }

  async function leave(g: GroupDto) {
    try {
      await apiFetch<void>(`/v1/groups/${g.groupId}/leave`, { method: 'POST' });
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed');
    }
  }

  return (
    <div data-testid="groups-page" style={{ padding: 16, fontFamily: 'system-ui, sans-serif', maxWidth: 900, margin: '0 auto' }}>
      <h1 data-testid="groups-title">Groups</h1>

      <div data-testid="create-group-card" style={{ border: '1px solid #e5e7eb', borderRadius: 8, padding: 12, marginBottom: 16 }}>
        <h2 data-testid="create-group-title" style={{ marginTop: 0 }}>
          Create group
        </h2>
        <div style={{ display: 'grid', gap: 10 }}>
          <input data-testid="create-group-name-input" value={name} onChange={(e) => setName(e.target.value)} placeholder="Name" />
          <input
            data-testid="create-group-description-input"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            placeholder="Description"
          />
          <label style={{ display: 'grid', gap: 6 }}>
            Visibility
            <select data-testid="create-group-visibility-select" value={visibility} onChange={(e) => setVisibility(e.target.value as 'Public' | 'Private')}>
              <option value="Public">Public</option>
              <option value="Private">Private</option>
            </select>
          </label>
          <button data-testid="create-group-submit-button" onClick={() => void create()} disabled={!name.trim()}>
            Create
          </button>
        </div>
      </div>

      {error ? (
        <div data-testid="groups-error" style={{ color: 'crimson', whiteSpace: 'pre-wrap' }}>
          {error}
        </div>
      ) : null}

      {loading ? <div data-testid="groups-loading">Loading…</div> : null}

      <div data-testid="groups-list" style={{ display: 'grid', gap: 10 }}>
        {groups.map((g) => (
          <div key={g.groupId} data-testid={`group-card-${g.groupId}`} style={{ border: '1px solid #e5e7eb', borderRadius: 8, padding: 12 }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', gap: 10 }}>
              <div>
                <div data-testid={`group-name-${g.groupId}`} style={{ fontWeight: 700 }}>
                  {g.name}
                </div>
                <div data-testid={`group-visibility-${g.groupId}`} style={{ color: '#6b7280', fontSize: 12 }}>
                  {g.visibility} • {g.memberCount} members
                </div>
                {g.description ? (
                  <div data-testid={`group-description-${g.groupId}`} style={{ marginTop: 6 }}>
                    {g.description}
                  </div>
                ) : null}
              </div>

              <Link data-testid={`group-open-link-${g.groupId}`} to={`/groups/${g.groupId}`}>
                Open
              </Link>
            </div>

            <div style={{ marginTop: 10, display: 'flex', gap: 10 }}>
              {g.isMember ? (
                <button data-testid={`group-leave-button-${g.groupId}`} onClick={() => void leave(g)}>
                  Leave
                </button>
              ) : (
                <button data-testid={`group-join-button-${g.groupId}`} onClick={() => void join(g)} disabled={g.visibility === 'Private'}>
                  Join
                </button>
              )}
              {g.visibility === 'Private' && !g.isMember ? (
                <span data-testid={`group-private-note-${g.groupId}`} style={{ color: '#6b7280', fontSize: 12 }}>
                  Private groups require invite (not implemented)
                </span>
              ) : null}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
