import { createContext, useContext, useEffect, useMemo, useState } from 'react';
import { fetchMe, getJwt, logout } from './auth';
import type { MeResponse } from './types';

type AppSession = {
  jwt: string | null;
  me: MeResponse | null;
  loadingMe: boolean;
  refreshMe: () => Promise<void>;
  logout: () => void;
};

const Ctx = createContext<AppSession | null>(null);

export function AppProvider({ children }: { children: React.ReactNode }) {
  const [jwt, setJwt] = useState<string | null>(getJwt());
  const [me, setMe] = useState<MeResponse | null>(null);
  const [loadingMe, setLoadingMe] = useState(false);

  async function refreshMe() {
    const current = getJwt();
    setJwt(current);
    if (!current) {
      setMe(null);
      return;
    }

    setLoadingMe(true);
    try {
      const data = await fetchMe();
      setMe(data);
    } catch {
      // token invalid
      logout();
      setJwt(null);
      setMe(null);
    } finally {
      setLoadingMe(false);
    }
  }

  useEffect(() => {
    void refreshMe();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const value = useMemo<AppSession>(
    () => ({
      jwt,
      me,
      loadingMe,
      refreshMe,
      logout: () => {
        logout();
        setJwt(null);
        setMe(null);
      },
    }),
    [jwt, me, loadingMe]
  );

  return <Ctx.Provider value={value}>{children}</Ctx.Provider>;
}

export function useAppSession() {
  const v = useContext(Ctx);
  if (!v) throw new Error('AppProvider missing');
  return v;
}

export function isModOrAdmin(role?: string | null): boolean {
  if (!role) return false;
  return ['Admin', 'Moderator', 'TenantOwner', 'PlatformOwner'].includes(role);
}
