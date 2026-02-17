import { useState } from 'react';
import { apiFetch } from '../lib/api';
import type { GroupDto, MemberDto, PageResponse, PostDto } from '../lib/types';
import { Link } from 'react-router-dom';

export default function SearchPage() {
  const [q, setQ] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const [posts, setPosts] = useState<PostDto[]>([]);
  const [members, setMembers] = useState<MemberDto[]>([]);
  const [groups, setGroups] = useState<GroupDto[]>([]);

  async function search() {
    const query = q.trim();
    if (!query) return;

    setLoading(true);
    setError(null);

    try {
      const [p, m, g] = await Promise.all([
        apiFetch<PageResponse<PostDto>>(`/v1/search/posts?q=${encodeURIComponent(query)}&page=1&pageSize=10`),
        apiFetch<PageResponse<MemberDto>>(`/v1/search/members?q=${encodeURIComponent(query)}&page=1&pageSize=10`),
        apiFetch<PageResponse<GroupDto>>(`/v1/search/groups?q=${encodeURIComponent(query)}&page=1&pageSize=10`),
      ]);

      setPosts(p.items);
      setMembers(m.items);
      setGroups(g.items);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Search failed');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div data-testid="search-page" style={{ padding: 16, fontFamily: 'system-ui, sans-serif', maxWidth: 1100, margin: '0 auto' }}>
      <h1 data-testid="search-title">Search</h1>

      <div data-testid="search-controls" style={{ display: 'flex', gap: 10, alignItems: 'center', marginBottom: 12 }}>
        <input data-testid="search-input" value={q} onChange={(e) => setQ(e.target.value)} placeholder="Search posts, members, groups" />
        <button data-testid="search-submit-button" onClick={() => void search()} disabled={loading || !q.trim()}>
          Search
        </button>
      </div>

      {error ? (
        <div data-testid="search-error" style={{ color: 'crimson', whiteSpace: 'pre-wrap' }}>
          {error}
        </div>
      ) : null}

      <div style={{ display: 'grid', gap: 16 }}>
        <section data-testid="search-posts-section">
          <h2 data-testid="search-posts-title">Posts</h2>
          <div data-testid="search-posts-list" style={{ display: 'grid', gap: 10 }}>
            {posts.map((p) => (
              <div key={p.postId} data-testid={`search-post-${p.postId}`} style={{ border: '1px solid #e5e7eb', borderRadius: 8, padding: 10 }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', gap: 10 }}>
                  <div style={{ fontWeight: 700 }}>{p.authorName}</div>
                  <Link data-testid={`search-post-open-${p.postId}`} to={`/posts/${p.postId}`}>
                    Open
                  </Link>
                </div>
                <div style={{ marginTop: 6, whiteSpace: 'pre-wrap' }}>{p.bodyText}</div>
              </div>
            ))}
            {!posts.length ? <div data-testid="search-posts-empty">No matches</div> : null}
          </div>
        </section>

        <section data-testid="search-members-section">
          <h2 data-testid="search-members-title">Members</h2>
          <div data-testid="search-members-list" style={{ display: 'grid', gap: 10 }}>
            {members.map((m) => (
              <div key={m.userId} data-testid={`search-member-${m.userId}`} style={{ border: '1px solid #e5e7eb', borderRadius: 8, padding: 10 }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', gap: 10 }}>
                  <div style={{ fontWeight: 700 }}>
                    {m.firstName} {m.lastName}
                  </div>
                  <Link data-testid={`search-member-open-${m.userId}`} to={`/members/${m.userId}`}>
                    View
                  </Link>
                </div>
                <div style={{ color: '#6b7280', fontSize: 12 }}>{m.email}</div>
              </div>
            ))}
            {!members.length ? <div data-testid="search-members-empty">No matches</div> : null}
          </div>
        </section>

        <section data-testid="search-groups-section">
          <h2 data-testid="search-groups-title">Groups</h2>
          <div data-testid="search-groups-list" style={{ display: 'grid', gap: 10 }}>
            {groups.map((g) => (
              <div key={g.groupId} data-testid={`search-group-${g.groupId}`} style={{ border: '1px solid #e5e7eb', borderRadius: 8, padding: 10 }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', gap: 10 }}>
                  <div style={{ fontWeight: 700 }}>{g.name}</div>
                  <Link data-testid={`search-group-open-${g.groupId}`} to={`/groups/${g.groupId}`}>
                    Open
                  </Link>
                </div>
                <div style={{ color: '#6b7280', fontSize: 12 }}>
                  {g.visibility} • {g.memberCount} members
                </div>
              </div>
            ))}
            {!groups.length ? <div data-testid="search-groups-empty">No matches</div> : null}
          </div>
        </section>
      </div>
    </div>
  );
}
