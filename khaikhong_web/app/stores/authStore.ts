"use client";

import { create } from "zustand";
import { persist } from "zustand/middleware"; // นำเข้า persist

export type UserProfile = {
  firstName: string;
  lastName: string;
  role: string;
};

type AuthState = {
  accessToken: string | null;
  refreshToken: string | null;
  user: UserProfile | null;
  setTokens: (tokens: { accessToken: string; refreshToken: string }) => void;
  setUser: (user: UserProfile | null) => void;
  clearAuth: () => void;
};

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      accessToken: null,
      refreshToken: null,
      user: null,

      setTokens: ({ accessToken, refreshToken }) =>
        set(() => ({ accessToken, refreshToken })),
      setUser: (user) => set(() => ({ user })),
      clearAuth: () =>
        set(() => ({ accessToken: null, refreshToken: null, user: null })),
    }),
    {
      name: "auth-storage",
    }
  )
);
