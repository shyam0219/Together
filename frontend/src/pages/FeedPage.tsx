import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { apiFetch } from '../lib/api';
import type { GroupDto, PageResponse, PostDto } from '../lib/types';

type CreatePostBody = {
  bodyText: string;
  imageUrls: string[];
  linkUrl: string | null;
  linkTitle: string | null;
  linkDescription: string | null;
  linkImageUrl: string | null;
  groupId: string | null;
};

export default function FeedPage() {
  const [page, setPage] = useState(1);
  const [pageSize] = useState(20);
  const [posts, setPosts] = useState<PostDto[]>([]);
  const [hasMore, setHasMore] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // create post form
  const [bodyText, setBodyText] = useState('');
  const [imageUrlsText, setImageUrlsText] = useState('');
  const [linkUrl, setLinkUrl] = useState('');
  const [linkTitle, setLinkTitle] = useState('');
  const [linkDescription, setLinkDescription] = useState('');
  const [linkImageUrl, setLinkImageUrl] = useState('');
  const [groupId, setGroupId] = useState<string>('');

  const [groups, setGroups] = useState<GroupDto[]>([]);

  const imageUrls = useMemo(() => {
    return imageUrlsText
      .split('\n')
      .map((s) => s.trim())
      .filter(Boolean);
  }, [imageUrlsText]);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      const data = await apiFetch<PageResponse<PostDto>>(`/v1/posts?page=${page}&pageSize=${pageSize}`);
      setPosts(data.items);
      setHasMore(data.hasMore);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load posts');
    } finally {
      setLoading(false);
    }
  }

  async function loadGroups() {
    try {
      const g = await apiFetch<GroupDto[]>('/v1/groups');
      setGroups(g);
    } catch {
      // ignore
    }
  }

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page]);

  useEffect(() => {
    void loadGroups();
  }, []);

  async function createPost() {
    setError(null);
    const payload: CreatePostBody = {
      bodyText,
      imageUrls,
      linkUrl: linkUrl || null,
      linkTitle: linkTitle || null,
      linkDescription: linkDescription || null,
      linkImageUrl: linkImageUrl || null,
      groupId: groupId || null,
    };

    try {
      await apiFetch<PostDto>('/v1/posts', { method: 'POST', body: JSON.stringify(payload) });
      setBodyText('');
      setImageUrlsText('');
      setLinkUrl('');
      setLinkTitle('');
      setLinkDescription('');
      setLinkImageUrl('');
      setGroupId('');
      setPage(1);
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to create post');
    }
  }

  async function toggleLike(p: PostDto) {
    const optimistic = posts.map((x) =>
      x.postId === p.postId
        ? { ...x, likedByMe: !x.likedByMe, likeCount: x.likeCount + (x.likedByMe ? -1 : 1) }
        : x
    );
    setPosts(optimistic);

    try {
      if (p.likedByMe) {
        await apiFetch<void>(`/v1/posts/${p.postId}/like`, { method: 'DELETE' });
      } else {
        await apiFetch<void>(`/v1/posts/${p.postId}/like`, { method: 'POST' });
      }
    } catch {
      await load();
    }
  }

  async function toggleBookmark(p: PostDto) {
    const optimistic = posts.map((x) =>
      x.postId === p.postId ? { ...x, bookmarkedByMe: !x.bookmarkedByMe } : x
    );
    setPosts(optimistic);

    try {
      if (p.bookmarkedByMe) {
        await apiFetch<void>(`/v1/posts/${p.postId}/bookmark`, { method: 'DELETE' });
      } else {
        await apiFetch<void>(`/v1/posts/${p.postId}/bookmark`, { method: 'POST' });
      }
    } catch {
      await load();
    }
  }

  return (
    <div data-testid="feed-page" style={{ padding: 16, fontFamily: 'system-ui, sans-serif', maxWidth: 1000, margin: '0 auto' }}>
      <h1 data-testid="feed-title">Feed</h1>

      <div data-testid="create-post-card" style={{ border: '1px solid #e5e7eb', borderRadius: 8, padding: 12, marginBottom: 16 }}>
        <h2 data-testid="create-post-title" style={{ marginTop: 0 }}>
          Create post
        </h2>

        <div style={{ display: 'grid', gap: 10 }}>
          <textarea
            data-testid="create-post-body-input"
            value={bodyText}
            onChange={(e) => setBodyText(e.target.value)}
            placeholder="Write something…"
            rows={4}
          />

          <textarea
            data-testid="create-post-images-input"
            value={imageUrlsText}
            onChange={(e) => setImageUrlsText(e.target.value)}
            placeholder="Image URLs (one per line, max 10)"
            rows={3}
          />

          <div style={{ display: 'grid', gap: 8, gridTemplateColumns: '1fr 1fr' }}>
            <input
              data-testid="create-post-link-url-input"
              value={linkUrl}
              onChange={(e) => setLinkUrl(e.target.value)}
              placeholder="Link URL"
            />
            <input
              data-testid="create-post-link-title-input"
              value={linkTitle}
              onChange={(e) => setLinkTitle(e.target.value)}
              placeholder="Link title"
            />
            <input
              data-testid="create-post-link-description-input"
              value={linkDescription}
              onChange={(e) => setLinkDescription(e.target.value)}
              placeholder="Link description"
            />
            <input
              data-testid="create-post-link-image-input"
              value={linkImageUrl}
              onChange={(e) => setLinkImageUrl(e.target.value)}
              placeholder="Link image URL"
            />
          </div>

          <label style={{ display: 'grid', gap: 6 }}>
            Optional group
            <select data-testid="create-post-group-select" value={groupId} onChange={(e) => setGroupId(e.target.value)}>
              <option value="">(none)</option>
              {groups.map((g) => (
                <option key={g.groupId} value={g.groupId}>
                  {g.name} ({g.visibility})
                </option>
              ))}
            </select>
          </label>

          {error ? (
            <div data-testid="feed-error" style={{ color: 'crimson', whiteSpace: 'pre-wrap' }}>
              {error}
            </div>
          ) : null}

          <button data-testid="create-post-submit-button" onClick={() => void createPost()} disabled={loading || !bodyText.trim()}>
            Post
          </button>
        </div>
      </div>

      <div data-testid="feed-controls" style={{ display: 'flex', gap: 12, alignItems: 'center', marginBottom: 10 }}>
        <button data-testid="feed-prev-page-button" onClick={() => setPage((p) => Math.max(1, p - 1))} disabled={page <= 1}>
          Prev
        </button>
        <div data-testid="feed-page-indicator">Page {page}</div>
        <button data-testid="feed-next-page-button" onClick={() => setPage((p) => p + 1)} disabled={!hasMore}>
          Next
        </button>
        <button data-testid="feed-refresh-button" onClick={() => void load()} disabled={loading}>
          Refresh
        </button>
      </div>

      {loading ? <div data-testid="feed-loading">Loading…</div> : null}

      <div data-testid="posts-list" style={{ display: 'grid', gap: 12 }}>
        {posts.map((p) => (
          <div key={p.postId} data-testid={`post-card-${p.postId}`} style={{ border: '1px solid #e5e7eb', borderRadius: 8, padding: 12 }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', gap: 12 }}>
              <div>
                <div data-testid={`post-author-${p.postId}`} style={{ fontWeight: 600 }}>
                  {p.authorName}
                </div>
                <div data-testid={`post-created-${p.postId}`} style={{ fontSize: 12, color: '#6b7280' }}>
                  {new Date(p.createdAt).toLocaleString()}
                </div>
              </div>
              <Link data-testid={`post-detail-link-${p.postId}`} to={`/posts/${p.postId}`}>
                Open
              </Link>
            </div>

            <div data-testid={`post-body-${p.postId}`} style={{ marginTop: 8, whiteSpace: 'pre-wrap' }}>
              {p.bodyText}
            </div>

            {p.images?.length ? (
              <div data-testid={`post-images-${p.postId}`} style={{ marginTop: 8, display: 'flex', gap: 8, flexWrap: 'wrap' }}>
                {p.images.map((img) => (
                  <a key={img.postImageId} href={img.url} target="_blank" rel="noreferrer" data-testid={`post-image-link-${p.postId}-${img.postImageId}`}>
                    <img src={img.url} alt="post" style={{ width: 110, height: 70, objectFit: 'cover', borderRadius: 6 }} />
                  </a>
                ))}
              </div>
            ) : null}

            <div data-testid={`post-stats-${p.postId}`} style={{ marginTop: 10, display: 'flex', gap: 14, alignItems: 'center' }}>
              <span data-testid={`post-like-count-${p.postId}`}>👍 {p.likeCount}</span>
              <span data-testid={`post-comment-count-${p.postId}`}>💬 {p.commentCount}</span>
              <span data-testid={`post-liked-by-me-${p.postId}`}>likedByMe: {String(p.likedByMe)}</span>
              <span data-testid={`post-bookmarked-by-me-${p.postId}`}>bookmarkedByMe: {String(p.bookmarkedByMe)}</span>
            </div>

            <div style={{ marginTop: 10, display: 'flex', gap: 10 }}>
              <button data-testid={`post-like-toggle-${p.postId}`} onClick={() => void toggleLike(p)}>
                {p.likedByMe ? 'Unlike' : 'Like'}
              </button>
              <button data-testid={`post-bookmark-toggle-${p.postId}`} onClick={() => void toggleBookmark(p)}>
                {p.bookmarkedByMe ? 'Unbookmark' : 'Bookmark'}
              </button>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
