import { useEffect, useMemo, useState } from 'react';
import { apiFetch } from '../lib/api';
import type { MemberDto, PageResponse } from '../lib/types';

type ConversationListItemDto = {
  conversationId: string;
  participantUserIds: string[];
  lastMessagePreview: string | null;
  lastMessageAt: string | null;
  unreadCount: number;
};

type MessageDto = {
  messageId: string;
  conversationId: string;
  senderId: string;
  senderName: string;
  bodyText: string;
  sentAt: string;
};

export default function MessagesPage() {
  const [conversations, setConversations] = useState<ConversationListItemDto[]>([]);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [messages, setMessages] = useState<MessageDto[]>([]);
  const [error, setError] = useState<string | null>(null);

  const [newMessage, setNewMessage] = useState('');

  const [startUserId, setStartUserId] = useState('');

  const selected = useMemo(() => conversations.find((c) => c.conversationId === selectedId) || null, [conversations, selectedId]);

  async function loadConversations() {
    try {
      const data = await apiFetch<PageResponse<ConversationListItemDto>>('/v1/messages/conversations?page=1&pageSize=50');
      setConversations(data.items);
      if (!selectedId && data.items.length) setSelectedId(data.items[0].conversationId);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed');
    }
  }

  async function loadMessages(conversationId: string) {
    try {
      const data = await apiFetch<PageResponse<MessageDto>>(`/v1/messages/conversations/${conversationId}/messages?page=1&pageSize=200`);
      setMessages(data.items);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed');
    }
  }

  useEffect(() => {
    void loadConversations();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    if (selectedId) void loadMessages(selectedId);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedId]);

  async function startConversation() {
    setError(null);
    try {
      const convo = await apiFetch<ConversationListItemDto>('/v1/messages/conversations', {
        method: 'POST',
        body: JSON.stringify({ otherUserId: startUserId }),
      });
      await loadConversations();
      setSelectedId(convo.conversationId);
      setStartUserId('');
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed');
    }
  }

  async function send() {
    if (!selectedId || !newMessage.trim()) return;
    setError(null);
    try {
      await apiFetch<MessageDto>(`/v1/messages/conversations/${selectedId}/messages`, {
        method: 'POST',
        body: JSON.stringify({ bodyText: newMessage.trim() }),
      });
      setNewMessage('');
      await loadMessages(selectedId);
      await loadConversations();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed');
    }
  }

  return (
    <div data-testid="messages-page" style={{ padding: 16, fontFamily: 'system-ui, sans-serif', maxWidth: 1200, margin: '0 auto' }}>
      <h1 data-testid="messages-title">Messages</h1>

      {error ? (
        <div data-testid="messages-error" style={{ color: 'crimson', whiteSpace: 'pre-wrap' }}>
          {error}
        </div>
      ) : null}

      <div data-testid="messages-layout" style={{ display: 'grid', gridTemplateColumns: '320px 1fr', gap: 12 }}>
        <div data-testid="conversations-panel" style={{ border: '1px solid #e5e7eb', borderRadius: 8, padding: 10 }}>
          <div data-testid="start-conversation" style={{ display: 'grid', gap: 8, marginBottom: 12 }}>
            <div style={{ fontWeight: 800 }}>Start conversation</div>
            <input
              data-testid="start-conversation-userid-input"
              value={startUserId}
              onChange={(e) => setStartUserId(e.target.value)}
              placeholder="Other userId (GUID)"
            />
            <button data-testid="start-conversation-button" onClick={() => void startConversation()} disabled={!startUserId.trim()}>
              Start
            </button>
          </div>

          <div data-testid="conversations-list" style={{ display: 'grid', gap: 8 }}>
            {conversations.map((c) => (
              <button
                key={c.conversationId}
                data-testid={`conversation-select-${c.conversationId}`}
                onClick={() => setSelectedId(c.conversationId)}
                style={{
                  textAlign: 'left',
                  border: '1px solid #e5e7eb',
                  background: selectedId === c.conversationId ? '#eef2ff' : 'white',
                  borderRadius: 8,
                  padding: 8,
                }}
              >
                <div style={{ fontWeight: 700 }}>{c.conversationId.slice(0, 8)}…</div>
                <div style={{ color: '#6b7280', fontSize: 12 }}>unread: {c.unreadCount}</div>
                {c.lastMessagePreview ? <div style={{ marginTop: 4 }}>{c.lastMessagePreview}</div> : null}
              </button>
            ))}
            {!conversations.length ? <div data-testid="conversations-empty">No conversations</div> : null}
          </div>
        </div>

        <div data-testid="chat-panel" style={{ border: '1px solid #e5e7eb', borderRadius: 8, padding: 10 }}>
          <div data-testid="chat-header" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 10 }}>
            <div style={{ fontWeight: 900 }}>{selected ? `Conversation ${selected.conversationId.slice(0, 8)}…` : 'Select a conversation'}</div>
            <button data-testid="chat-refresh-button" onClick={() => selectedId && void loadMessages(selectedId)} disabled={!selectedId}>
              Refresh
            </button>
          </div>

          <div
            data-testid="chat-messages-list"
            style={{ height: 420, overflow: 'auto', border: '1px solid #f3f4f6', borderRadius: 8, padding: 10, display: 'grid', gap: 8 }}
          >
            {messages.map((m) => (
              <div key={m.messageId} data-testid={`chat-message-${m.messageId}`} style={{ padding: 8, borderRadius: 8, background: '#f9fafb' }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', gap: 10 }}>
                  <div style={{ fontWeight: 700 }}>{m.senderName}</div>
                  <div style={{ color: '#6b7280', fontSize: 12 }}>{new Date(m.sentAt).toLocaleString()}</div>
                </div>
                <div style={{ marginTop: 6, whiteSpace: 'pre-wrap' }}>{m.bodyText}</div>
              </div>
            ))}
            {!messages.length ? <div data-testid="chat-empty">No messages</div> : null}
          </div>

          <div data-testid="chat-composer" style={{ marginTop: 10, display: 'flex', gap: 10 }}>
            <input
              data-testid="chat-message-input"
              value={newMessage}
              onChange={(e) => setNewMessage(e.target.value)}
              placeholder="Type a message…"
              style={{ flex: 1 }}
              disabled={!selectedId}
            />
            <button data-testid="chat-send-button" onClick={() => void send()} disabled={!selectedId || !newMessage.trim()}>
              Send
            </button>
          </div>
        </div>
      </div>

      <p data-testid="messages-note" style={{ marginTop: 12, color: '#6b7280' }}>
        Note: start a conversation by pasting another userId from Members page.
      </p>
    </div>
  );
}
