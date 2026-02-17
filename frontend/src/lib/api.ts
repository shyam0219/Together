export const API_BASE_URL = import.meta.env.VITE_BACKEND_URL as string | undefined;

if (!API_BASE_URL) {
  // eslint-disable-next-line no-console
  console.warn('VITE_BACKEND_URL is not set; API calls will fail.');
}

export async function apiFetch<T>(path: string, init?: RequestInit): Promise<T> {
  const url = `${API_BASE_URL}${path}`;
  const res = await fetch(url, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      ...(init?.headers || {}),
    },
  });

  if (!res.ok) {
    const text = await res.text();
    throw new Error(text || `HTTP ${res.status}`);
  }

  return (await res.json()) as T;
}
