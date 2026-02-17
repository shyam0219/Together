import { useEffect, useState } from 'react';
import { apiFetch } from '../lib/api';

type PollOptionDto = {
  pollOptionId: string;
  text: string;
  sortOrder: number;
  voteCount: number;
};

type PollDto = {
  pollId: string;
  postId: string;
  question: string;
  options: PollOptionDto[];
  totalVotes: number;
  myVotedOptionId: string | null;
};

export default function PollWidget({ postId }: { postId: string }) {
  const [poll, setPoll] = useState<PollDto | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function load() {
    setError(null);
    try {
      const p = await apiFetch<PollDto>(`/v1/posts/${postId}/poll`);
      setPoll(p);
    } catch {
      // poll may not exist
      setPoll(null);
    }
  }

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [postId]);

  async function vote(optionId: string) {
    if (!poll) return;
    setError(null);
    try {
      await apiFetch<void>(`/v1/polls/${poll.pollId}/vote`, { method: 'POST', body: JSON.stringify({ optionId }) });
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed');
    }
  }

  if (!poll) return null;

  return (
    <div data-testid={`poll-widget-${postId}`} style={{ marginTop: 10, padding: 10, border: '1px solid #e5e7eb', borderRadius: 8 }}>
      <div data-testid={`poll-question-${postId}`} style={{ fontWeight: 800 }}>
        {poll.question}
      </div>
      <div data-testid={`poll-total-${postId}`} style={{ color: '#6b7280', fontSize: 12, marginTop: 4 }}>
        Total votes: {poll.totalVotes}
      </div>

      {error ? (
        <div data-testid={`poll-error-${postId}`} style={{ color: 'crimson', whiteSpace: 'pre-wrap', marginTop: 6 }}>
          {error}
        </div>
      ) : null}

      <div data-testid={`poll-options-${postId}`} style={{ display: 'grid', gap: 8, marginTop: 10 }}>
        {poll.options
          .slice()
          .sort((a, b) => a.sortOrder - b.sortOrder)
          .map((o) => (
            <button
              key={o.pollOptionId}
              data-testid={`poll-option-${postId}-${o.pollOptionId}`}
              onClick={() => void vote(o.pollOptionId)}
              disabled={!!poll.myVotedOptionId}
              style={{
                textAlign: 'left',
                padding: 8,
                borderRadius: 8,
                border: '1px solid #e5e7eb',
                background: poll.myVotedOptionId === o.pollOptionId ? '#ecfeff' : 'white',
              }}
            >
              <div style={{ display: 'flex', justifyContent: 'space-between', gap: 10 }}>
                <span>{o.text}</span>
                <span data-testid={`poll-option-count-${postId}-${o.pollOptionId}`} style={{ color: '#6b7280' }}>
                  {o.voteCount}
                </span>
              </div>
            </button>
          ))}
      </div>

      {poll.myVotedOptionId ? (
        <div data-testid={`poll-voted-${postId}`} style={{ marginTop: 10, color: '#065f46' }}>
          You voted.
        </div>
      ) : (
        <div data-testid={`poll-not-voted-${postId}`} style={{ marginTop: 10, color: '#6b7280' }}>
          Select an option to vote.
        </div>
      )}
    </div>
  );
}
