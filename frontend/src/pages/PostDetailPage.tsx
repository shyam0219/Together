import { useEffect, useMemo, useState } from 'react';
import { useParams } from 'react-router-dom';
import { apiFetch } from '../lib/api';
import type { PostDto } from '../lib/types';

type CommentDto = {
  commentId: string;
  postId: string;
  authorId: string;
  authorName: string;
  parentCommentId: string | null;
  text: string;
  createdAt: string;
};

type CreateCommentBody = { text: string; parentCommentId: string | null };

export default function PostDetailPage() {
  const { id } = useParams();
  const postId = id as string;

  const [post, setPost] = useState<PostDto | null>(null);
  const [comments, setComments] = useState<CommentDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [text, setText] = useState('');
  const [replyTo, setReplyTo] = useState<string | null>(null);

  const topLevel = useMemo(() => comments.filter((c) => !c.parentCommentId), [comments]);
  const repliesByParent = useMemo(() => {
    const m = new Map<string, CommentDto[]>();
    for (const c of comments) {
      if (!c.parentCommentId) continue;
      const arr = m.get(c.parentCommentId) || [];
      arr.push(c);
      m.set(c.parentCommentId, arr);
    }
    for (const arr of m.values()) arr.sort((a, b) => a.createdAt.localeCompare(b.createdAt));
    return m;
  }, [comments]);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      const p = await apiFetch<PostDto>(`/v1/posts/${postId}`);
      setPost(p);

      const page1 = await apiFetch<{ items: CommentDto[]; page: number; pageSize: number; hasMore: boolean }>(
        `/v1/posts/${postId}/comments?page=1&pageSize=200`
      );
      setComments(page1.items);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [postId]);

  async function addComment() {
    if (!text.trim()) return;
    setError(null);
    try {
      const body: CreateCommentBody = { text: text.trim(), parentCommentId: replyTo };
      const c = await apiFetch<CommentDto>(`/v1/posts/${postId}/comments`, {
        method: 'POST',
        body: JSON.stringify(body),
      });
      setComments((prev) => [...prev, c].sort((a, b) => a.createdAt.localeCompare(b.createdAt)));
      setText('');
      setReplyTo(null);
      // refresh post to update commentCount
      const p = await apiFetch<PostDto>(`/v1/posts/${postId}`);
      setPost(p);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed');
    }
  }

  async function deleteComment(commentId: string) {
    setError(null);
    try {
      await apiFetch<void>(`/v1/comments/${commentId}`, { method: 'DELETE' });
      setComments((prev) => prev.filter((c) => c.commentId !== commentId));
      const p = await apiFetch<PostDto>(`/v1/posts/${postId}`);
      setPost(p);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed');
    }
  }

  return (
    <div data-testid="post-detail-page" style={{ padding: 16, fontFamily: 'system-ui, sans-serif', maxWidth: 900, margin: '0 auto' }}>
      <h1 data-testid="post-detail-title">Post detail</h1>

      {loading ? <div data-testid="post-detail-loading">Loading…</div> : null}
      {error ? (
        <div data-testid="post-detail-error" style={{ color: 'crimson', whiteSpace: 'pre-wrap' }}>
          {error}
        </div>
      ) : null}

      {post ? (
        <div data-testid="post-detail-card" style={{ border: '1px solid #e5e7eb', borderRadius: 8, padding: 12, marginBottom: 16 }}>
          <div data-testid="post-detail-author" style={{ fontWeight: 600 }}>
            {post.authorName}
          </div>
          <div data-testid="post-detail-body" style={{ marginTop: 8, whiteSpace: 'pre-wrap' }}>
            {post.bodyText}
          </div>
          <div data-testid="post-detail-stats" style={{ marginTop: 10, display: 'flex', gap: 14 }}>
            <span data-testid="post-detail-like-count">👍 {post.likeCount}</span>
            <span data-testid="post-detail-comment-count">💬 {post.commentCount}</span>
          </div>
        </div>
      ) : null}

      <div data-testid="comments-section">
        <h2 data-testid="comments-title">Comments</h2>

        <div data-testid="add-comment-card" style={{ border: '1px solid #e5e7eb', borderRadius: 8, padding: 12 }}>
          {replyTo ? (
            <div data-testid="replying-indicator" style={{ marginBottom: 8, color: '#374151' }}>
              Replying to {replyTo}{' '}
              <button data-testid="reply-cancel-button" onClick={() => setReplyTo(null)}>
                Cancel
              </button>
            </div>
          ) : null}

          <textarea
            data-testid="comment-text-input"
            value={text}
            onChange={(e) => setText(e.target.value)}
            rows={3}
            placeholder="Write a comment…"
          />
          <div style={{ marginTop: 8 }}>
            <button data-testid="comment-submit-button" onClick={() => void addComment()} disabled={!text.trim()}>
              Comment
            </button>
          </div>
        </div>

        <div data-testid="comments-list" style={{ marginTop: 12, display: 'grid', gap: 10 }}>
          {topLevel.map((c) => (
            <div key={c.commentId} data-testid={`comment-card-${c.commentId}`} style={{ border: '1px solid #e5e7eb', borderRadius: 8, padding: 10 }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', gap: 8 }}>
                <div>
                  <div data-testid={`comment-author-${c.commentId}`} style={{ fontWeight: 600 }}>
                    {c.authorName}
                  </div>
                  <div data-testid={`comment-text-${c.commentId}`} style={{ whiteSpace: 'pre-wrap', marginTop: 6 }}>
                    {c.text}
                  </div>
                </div>
                <div style={{ display: 'flex', gap: 8, alignItems: 'start' }}>
                  <button data-testid={`comment-reply-button-${c.commentId}`} onClick={() => setReplyTo(c.commentId)}>
                    Reply
                  </button>
                  <button data-testid={`comment-delete-button-${c.commentId}`} onClick={() => void deleteComment(c.commentId)}>
                    Delete
                  </button>
                </div>
              </div>

              {(repliesByParent.get(c.commentId) || []).length ? (
                <div data-testid={`comment-replies-${c.commentId}`} style={{ marginTop: 10, paddingLeft: 12, borderLeft: '3px solid #e5e7eb', display: 'grid', gap: 8 }}>
                  {(repliesByParent.get(c.commentId) || []).map((r) => (
                    <div key={r.commentId} data-testid={`comment-reply-card-${r.commentId}`} style={{ padding: 8, border: '1px solid #f3f4f6', borderRadius: 8 }}>
                      <div data-testid={`comment-author-${r.commentId}`} style={{ fontWeight: 600 }}>
                        {r.authorName}
                      </div>
                      <div data-testid={`comment-text-${r.commentId}`} style={{ whiteSpace: 'pre-wrap', marginTop: 6 }}>
                        {r.text}
                      </div>
                    </div>
                  ))}
                </div>
              ) : null}
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
