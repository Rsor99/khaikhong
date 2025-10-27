"use client";

import { ReactNode, useEffect } from "react";

type ModalProps = {
  open: boolean;
  title: string;
  description?: string;
  onClose: () => void;
  children: ReactNode;
  footer?: ReactNode;
};

export function Modal({ open, title, description, onClose, children, footer }: ModalProps) {
  useEffect(() => {
    if (!open) return;
    const original = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    return () => {
      document.body.style.overflow = original;
    };
  }, [open]);

  useEffect(() => {
    if (!open) return;
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        event.preventDefault();
        onClose();
      }
    };
    window.addEventListener("keydown", handleKeyDown);
    return () => {
      window.removeEventListener("keydown", handleKeyDown);
    };
  }, [open, onClose]);

  if (!open) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/70 px-4 py-8 backdrop-blur-lg">
      <div className="relative w-full max-w-5xl rounded-3xl border border-white/10 bg-slate-900/95 text-white shadow-2xl">
        <button
          type="button"
          onClick={onClose}
          className="absolute right-4 top-4 inline-flex h-9 w-9 items-center justify-center rounded-full border border-white/10 bg-white/5 text-sm text-white/60 transition hover:border-white/30 hover:text-white"
        >
          ✕
        </button>
        <header className="space-y-2 border-b border-white/10 px-8 py-6 pr-16">
          <h2 className="text-2xl font-semibold">{title}</h2>
          {description ? <p className="text-sm text-white/70">{description}</p> : null}
        </header>
        <div className="max-h-[70vh] overflow-y-auto px-8 py-6">{children}</div>
        {footer ? <footer className="border-t border-white/10 px-8 py-4">{footer}</footer> : null}
      </div>
    </div>
  );
}
