"use client";

import { ChangeEvent, FormEvent, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useMutation } from "@tanstack/react-query";
import { toast } from "react-toastify";
import {
  registerUser,
  type RegisterPayload,
} from "../lib/authApi";

const roles: RegisterPayload["role"][] = ["Admin", "User"];

export default function RegisterPage() {
  const router = useRouter();
  const [formError, setFormError] = useState<string | null>(null);
  const [formState, setFormState] = useState<RegisterPayload>({
    email: "",
    password: "",
    firstName: "",
    lastName: "",
    role: "Admin",
  });

  const registerMutation = useMutation({
    mutationFn: registerUser,
    onMutate: () => {
      setFormError(null);
    },
    onSuccess: () => {
      toast.success("Register successful");
      router.push("/");
    },
    onError: (error) => {
      if (error instanceof Error) {
        setFormError(error.message);
      } else {
        setFormError("Something went wrong");
      }
    },
  });

  const isSubmitting = registerMutation.isPending;

  const handleChange =
    (field: keyof RegisterPayload) =>
    (event: ChangeEvent<HTMLInputElement>) => {
      const value =
        field === "role"
          ? (event.target.value as RegisterPayload["role"])
          : event.target.value;
      setFormState((prev) => ({ ...prev, [field]: value }));
    };

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    registerMutation.mutate(formState);
  };

  return (
    <div className="relative flex min-h-screen items-center justify-center bg-gradient-to-br from-slate-900 via-slate-950 to-slate-900 px-4 py-16">
      <div className="pointer-events-none absolute inset-0">
        <div className="absolute left-1/2 top-12 h-64 w-64 -translate-x-1/2 rounded-full bg-indigo-500/20 blur-3xl" />
        <div className="absolute bottom-4 right-16 h-56 w-56 rounded-full bg-purple-500/20 blur-3xl" />
      </div>

      <main className="relative z-10 w-full max-w-xl rounded-3xl border border-white/10 bg-white/10 p-10 shadow-2xl backdrop-blur-xl">
        <header className="space-y-2 text-white">
          <h1 className="text-3xl font-semibold">สมัครสมาชิก</h1>
          <p className="text-sm text-white/70">
            สร้างบัญชีใหม่เพื่อเข้าใช้งานระบบ
          </p>
        </header>

        <form className="mt-8 space-y-6" onSubmit={handleSubmit}>
          <div className="grid gap-4 sm:grid-cols-2">
            <label className="block space-y-2">
              <span className="text-xs font-medium uppercase tracking-wider text-white/70">
                First name
              </span>
              <input
                value={formState.firstName}
                onChange={handleChange("firstName")}
                type="text"
                className="w-full rounded-2xl border border-white/20 bg-white/10 px-4 py-3 text-sm text-white outline-none transition focus:border-indigo-300 focus:ring-2 focus:ring-indigo-400/40"
                placeholder="First name"
                required
              />
            </label>
            <label className="block space-y-2">
              <span className="text-xs font-medium uppercase tracking-wider text-white/70">
                Last name
              </span>
              <input
                value={formState.lastName}
                onChange={handleChange("lastName")}
                type="text"
                className="w-full rounded-2xl border border-white/20 bg-white/10 px-4 py-3 text-sm text-white outline-none transition focus:border-indigo-300 focus:ring-2 focus:ring-indigo-400/40"
                placeholder="Last name"
                required
              />
            </label>
          </div>

          <label className="block space-y-2">
            <span className="text-xs font-medium uppercase tracking-wider text-white/70">
              Email
            </span>
            <input
              value={formState.email}
              onChange={handleChange("email")}
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
              value={formState.password}
              onChange={handleChange("password")}
              type="password"
              className="w-full rounded-2xl border border-white/20 bg-white/10 px-4 py-3 text-sm text-white outline-none transition focus:border-indigo-300 focus:ring-2 focus:ring-indigo-400/40"
              placeholder="••••••••"
              required
            />
          </label>

          <fieldset className="space-y-3">
            <legend className="text-xs font-medium uppercase tracking-wider text-white/70">
              Role
            </legend>
            <div className="flex flex-wrap gap-3">
              {roles.map((role) => (
                <label
                  key={role}
                  className={`flex items-center gap-2 rounded-2xl border px-4 py-3 text-sm transition ${
                    formState.role === role
                      ? "border-indigo-300 bg-indigo-500/10 text-white"
                      : "border-white/20 bg-white/5 text-white/70 hover:border-indigo-300/60"
                  }`}
                >
                  <input
                    type="radio"
                    name="role"
                    value={role}
                    checked={formState.role === role}
                    onChange={handleChange("role")}
                    className="h-4 w-4 accent-indigo-400"
                  />
                  <span className="capitalize">{role}</span>
                </label>
              ))}
            </div>
          </fieldset>

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
            {isSubmitting ? "กำลังสมัครสมาชิก..." : "สมัครสมาชิก"}
          </button>
        </form>

        <p className="mt-6 text-center text-sm text-white/70">
          มีบัญชีอยู่แล้วใช่ไหม?{" "}
          <Link
            href="/"
            className="font-semibold text-indigo-300 hover:text-indigo-200"
          >
            เข้าสู่ระบบ
          </Link>
        </p>
      </main>
    </div>
  );
}
