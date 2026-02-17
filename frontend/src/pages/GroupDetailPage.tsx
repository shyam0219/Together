import { useEffect, useMemo, useState } from 'react';
import { useParams } from 'react-router-dom';
import { apiFetch } from '../lib/api';
import type { GroupDto, PageResponse, PostDto } from '../lib/types';

export default function GroupDetailPage() {
  const { id } = useParams();
  const groupId = id as string;

  const [group, setGroup] = useState<GroupDto | null>(null);
  const [posts, setPosts] = useState<PostDto[]>([]);
  const [error, setError] = useState<string | null>(null);

  const groupPosts = useMemo(() => posts, [posts]);

  async function load() {
    setError(null);
    try {
      const g = await apiFetch<GroupDto>(`/v1/groups/${groupId}`);
      setGroup(g);

      const feed = await apiFetch<PageResponse<PostDto>>(`/v1/groups/${groupId}/posts?page=1&pageSize=50`);
      setPosts(feed.items);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed');
    }
  }

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [groupId]);

  if (error) {
    return (
      <div data-testid="group-detail-page" style={{ padding: 16, fontFamily: 'system-ui, sans-serif' }}>
        <h1 data-testid="group-detail-title">Group</h1>
        <div data-testid="group-detail-error" style={{ color: 'crimson', whiteSpace: 'pre-wrap' }}>
          {error}
        </div>
      </div>
    );
  }

  return (
    <div data-testid="group-detail-page" style={{ padding: 16, fontFamily: 'system-ui, sans-serif', maxWidth: 900, margin: '0 auto' }}>
      <h1 data-testid="group-detail-title">Group detail</h1>

      {group ? (
        <div data-testid="group-detail-card" style={{ border: '1px solid #e5e7eb', borderRadius: 8, padding: 12 }}>
          <div data-testid="group-detail-name" style={{ fontWeight: 800 }}>
            {group.name}
          </div>
          <div data-testid="group-detail-meta" style={{ color: '#6b7280', fontSize: 12 }}>
            {group.visibility} • {group.memberCount} members • isMember={String(group.isMember)}
          </div>
          {group.description ? (
            <div data-testid="group-detail-description" style={{ marginTop: 8 }}>
              {group.description}
            </div>
          ) : null}
        </div>
      ) : null}

      <h2 data-testid="group-detail-posts-title" style={{ marginTop: 16 }}>
        Recent posts (privacy enforced)
      </h2>
      <div data-testid="group-detail-posts-list" style={{ display: 'grid', gap: 10 }}>
        {groupPosts.map((p) => (
          <div key={p.postId} data-testid={`group-detail-post-${p.postId}`} style={{ border: '1px solid #e5e7eb', borderRadius: 8, padding: 10 }}>
            <div style={{ fontWeight: 700 }}>{p.authorName}</div>
            <div style={{ marginTop: 6, whiteSpace: 'pre-wrap' }}>{p.bodyText}</div>
          </div>
        ))}
      </div>
    </div>
  );
}
