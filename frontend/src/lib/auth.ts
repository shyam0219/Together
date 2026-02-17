import { apiFetch } from './api';
import type { MeResponse } from './types';

export function getJwt(): string | null {
  return localStorage.getItem('communityos_jwt');
}

export function logout() {
  localStorage.removeItem('communityos_jwt');
}

export async function fetchMe(): Promise<MeResponse> {
  return await apiFetch<MeResponse>('/v1/me');
}
