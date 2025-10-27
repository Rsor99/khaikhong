"use client";

import { FormEvent, useMemo, useState } from "react";
import Link from "next/link";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { login, logout, getUserProfile } from "./lib/authApi";
import { useAuthStore } from "./stores/authStore";
import { UserOrderPanel } from "./components/order/UserOrderPanel";
import { AdminDashboard } from "./components/admin/AdminDashboard";

export default function Home() {
  const queryClient = useQueryClient();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [formError, setFormError] = useState<string | null>(null);

  const accessToken = useAuthStore((state) => state.accessToken);
  const user = useAuthStore((state) => state.user);
  const setTokens = useAuthStore((state) => state.setTokens);
  const clearAuth = useAuthStore((state) => state.clearAuth);

  const { status: profileStatus } = useQuery({
    queryKey: ["userProfile"],
    queryFn: getUserProfile,
    enabled: Boolean(accessToken),
    retry: 1,
    staleTime: 0,
  });

  const isProfileLoading = profileStatus === "pending";

  const loginMutation = useMutation({
    mutationFn: login,
    onMutate: () => {
      setFormError(null);
    },
    onSuccess: (tokens) => {
      setTokens(tokens);
      queryClient.invalidateQueries({ queryKey: ["userProfile"] });
    },
    onError: (error) => {
      if (error instanceof Error) {
        setFormError(error.message);
      }
    },
  });

  const logoutMutation = useMutation({
    mutationFn: logout,
    onSuccess: () => {
      clearAuth();
      queryClient.removeQueries({ queryKey: ["userProfile"] });
      setFormError(null);
    },
    onError: (error) => {
      if (error instanceof Error) {
        setFormError(error.message);
      }
    },
  });

  const isSubmitting = useMemo(
    () => loginMutation.isPending || logoutMutation.isPending,
    [loginMutation.isPending, logoutMutation.isPending]
  );

  const handleLogin = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    loginMutation.mutate({ email, password });
  };

  const handleLogout = () => {
    logoutMutation.mutate();
  };

  return (
    <div className="relative min-h-screen bg-gradient-to-br from-slate-900 via-slate-950 to-slate-900">
      <div className="pointer-events-none absolute inset-0">
        <div className="absolute left-1/2 top-16 h-64 w-64 -translate-x-1/2 rounded-full bg-indigo-500/20 blur-3xl" />
        <div className="absolute bottom-0 right-12 h-56 w-56 rounded-full bg-purple-500/20 blur-3xl" />
      </div>

      <div className="relative z-10 flex min-h-screen items-center justify-center px-4 py-16">
        {!user ? (
          <main className="w-full max-w-md rounded-3xl border border-white/10 bg-white/10 p-10 shadow-2xl backdrop-blur-xl">
            <form className="space-y-8" onSubmit={handleLogin}>
              <header className="space-y-2 text-white">
                <h1 className="text-3xl font-semibold">เข้าสู่ระบบ</h1>
                <p className="text-sm text-white/70">
                  กรอกอีเมลและรหัสผ่านเพื่อเข้าใช้งานระบบ
                </p>
              </header>

              <div className="space-y-4">
                <label className="block space-y-2">
                  <span className="text-xs font-medium uppercase tracking-wider text-white/70">
                    Email
                  </span>
                  <input
                    id="email"
                    value={email}
                    onChange={(event) => setEmail(event.target.value)}
                    type="email"
                    className="w-full rounded-2xl border border-white/20 bg-white/10 px-4 py-3 text-sm text-white outline-none transition focus:border-indigo-300 focus:ring-2 focus:ring-indigo-400/40"
                    placeholder="you@example.com"
                    required
                  />
                </label>

                <label className="block space-y-2">
                  <span className="text-xs font-medium uppercase tracking-wider text-white/70">
                    Password
                  </span>
                  <input
                    id="password"
                    value={password}
                    onChange={(event) => setPassword(event.target.value)}
                    type="password"
                    className="w-full rounded-2xl border border-white/20 bg-white/10 px-4 py-3 text-sm text-white outline-none transition focus:border-indigo-300 focus:ring-2 focus:ring-indigo-400/40"
                    placeholder="••••••••"
                    required
                  />
                </label>
              </div>

              {formError ? (
                <p className="rounded-2xl border border-rose-300/60 bg-rose-500/10 px-4 py-2 text-sm text-rose-100">
                  {formError}
                </p>
              ) : null}

              <button
                type="submit"
                className="flex w-full items-center justify-center gap-2 rounded-2xl bg-gradient-to-r from-indigo-500 to-purple-500 px-4 py-3 text-sm font-semibold text-white shadow-lg shadow-indigo-500/30 transition hover:from-indigo-600 hover:to-purple-500 focus:outline-none focus:ring-2 focus:ring-indigo-300 disabled:cursor-not-allowed disabled:opacity-60"
                disabled={isSubmitting}
              >
                {loginMutation.isPending ? "กำลังเข้าสู่ระบบ..." : "เข้าสู่ระบบ"}
              </button>
            </form>
            <p className="mt-6 text-center text-sm text-white/70">
              ยังไม่มีบัญชี?{" "}
              <Link
                href="/register"
                className="font-semibold text-indigo-300 hover:text-indigo-200"
              >
                สมัครสมาชิก
              </Link>
            </p>
          </main>
        ) : (
          <div className="flex w-full max-w-5xl flex-col gap-8">
            <header className="flex items-center justify-between rounded-3xl border border-white/10 bg-white/10 px-8 py-6 text-white shadow-2xl backdrop-blur-xl">
              <div>
                <p className="text-xs uppercase tracking-widest text-white/60">บัญชีผู้ใช้</p>
                <h1 className="text-2xl font-semibold">
                  {user.firstName} {user.lastName}
                </h1>
              </div>
              <div className="flex items-center gap-4">
                {isProfileLoading ? (
                  <span className="flex items-center gap-2 rounded-full bg-white/10 px-3 py-1 text-xs font-medium text-indigo-100">
                    <span className="h-1.5 w-1.5 animate-ping rounded-full bg-indigo-200" />
                    โหลดข้อมูล...
                  </span>
                ) : null}
                <span className="rounded-full bg-white/10 px-3 py-1 text-xs font-semibold uppercase tracking-wide text-indigo-200">
                  {user.role}
                </span>
                <button
                  type="button"
                  onClick={handleLogout}
                  className="rounded-2xl border border-white/20 bg-white/10 px-4 py-2 text-sm font-semibold text-white transition hover:border-rose-200 hover:bg-rose-500/20 focus:outline-none focus:ring-2 focus:ring-rose-300/40 disabled:cursor-not-allowed disabled:opacity-60"
                  disabled={isSubmitting}
                >
                  {logoutMutation.isPending ? "กำลังออกจากระบบ..." : "ออกจากระบบ"}
                </button>
              </div>
            </header>

            {user.role === "Admin" ? (
              <AdminDashboard />
            ) : user.role === "User" ? (
              <UserOrderPanel />
            ) : (
              <main className="flex flex-1 items-center justify-center rounded-3xl border border-dashed border-white/20 bg-white/5 p-12 text-white/60">
                ส่วนเนื้อหาหลักของระบบ
              </main>
            )}
          </div>
        )}
      </div>
    </div>
  );
}
