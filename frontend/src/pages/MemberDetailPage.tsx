import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { apiFetch } from '../lib/api';
import type { MemberDto } from '../lib/types';

export default function MemberDetailPage() {
  const { id } = useParams();
  const userId = id as string;

  const [member, setMember] = useState<MemberDto | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function load() {
      setError(null);
      try {
        const m = await apiFetch<MemberDto>(`/v1/members/${userId}`);
        setMember(m);
      } catch (e) {
        setError(e instanceof Error ? e.message : 'Failed');
      }
    }

    void load();
  }, [userId]);

  return (
    <div data-testid="member-detail-page" style={{ padding: 16, fontFamily: 'system-ui, sans-serif', maxWidth: 700, margin: '0 auto' }}>
      <h1 data-testid="member-detail-title">Member profile</h1>

      {error ? (
        <div data-testid="member-detail-error" style={{ color: 'crimson', whiteSpace: 'pre-wrap' }}>
          {error}
        </div>
      ) : null}

      {member ? (
        <div data-testid="member-detail-card" style={{ border: '1px solid #e5e7eb', borderRadius: 8, padding: 12 }}>
          <div data-testid="member-detail-name" style={{ fontWeight: 900, fontSize: 18 }}>
            {member.firstName} {member.lastName}
          </div>
          <div data-testid="member-detail-email" style={{ color: '#6b7280' }}>
            {member.email}
          </div>

          <div style={{ marginTop: 10, display: 'grid', gap: 6 }}>
            <div data-testid="member-detail-role">Role: {member.role}</div>
            <div data-testid="member-detail-status">Status: {member.status}</div>
            <div data-testid="member-detail-city">City: {member.city ?? '-'}</div>
            <div data-testid="member-detail-bio">Bio: {member.bio ?? '-'}</div>
            <div data-testid="member-detail-avatar">AvatarUrl: {member.avatarUrl ?? '-'}</div>
          </div>
        </div>
      ) : null}
    </div>
  );
}
