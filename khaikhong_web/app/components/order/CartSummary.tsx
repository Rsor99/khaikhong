"use client";

import type { OrderItemType } from "../../lib/orderApi";

export type CartItem = {
  id: string;
  type: OrderItemType;
  name: string;
  sku?: string;
  price: number;
  quantity: number;
  stock?: number | null;
};

type CartSummaryProps = {
  items: CartItem[];
  isSubmitting: boolean;
  onUpdateQuantity: (item: CartItem, quantity: number) => void;
  onRemove: (item: CartItem) => void;
  onSubmit: () => void;
};

export function CartSummary({
  items,
  isSubmitting,
  onUpdateQuantity,
  onRemove,
  onSubmit,
}: CartSummaryProps) {
  const total = items.reduce((sum, item) => sum + item.price * item.quantity, 0);

  if (!items.length) {
    return (
      <div className="rounded-3xl border border-dashed border-white/20 bg-white/5 p-10 text-center text-sm text-white/60">
        เลือกสินค้าเพื่อเริ่มสั่งซื้อ
      </div>
    );
  }

  return (
    <section className="space-y-6 rounded-3xl border border-white/10 bg-white/10 p-8 text-white shadow-xl backdrop-blur-xl">
      <header className="space-y-1">
        <h3 className="text-xl font-semibold">ตะกร้าสินค้า</h3>
        <p className="text-sm text-white/60">
          ตรวจสอบจำนวนและยืนยันการสั่งซื้อ
        </p>
      </header>

      <ul className="space-y-4">
        {items.map((item) => (
          <li
            key={`${item.type}-${item.id}`}
            className="flex flex-col gap-4 rounded-2xl border border-white/10 bg-white/5 p-4 sm:flex-row sm:items-center sm:justify-between"
          >
            <div>
              <p className="text-sm font-semibold text-white">
                {item.name}
              </p>
              {item.sku ? (
                <p className="text-xs text-white/60">SKU: {item.sku}</p>
              ) : null}
              <p className="mt-1 text-sm text-indigo-200">
                ฿{item.price.toLocaleString("th-TH", { minimumFractionDigits: 2 })}
              </p>
              {typeof item.stock === "number" ? (
                <p className="text-xs text-white/50">คงเหลือ {item.stock}</p>
              ) : null}
            </div>

            <div className="flex flex-col items-end gap-3 sm:flex-row sm:items-center">
              <div className="flex items-center gap-2">
                <button
                  type="button"
                  className="flex h-8 w-8 items-center justify-center rounded-full border border-white/20 text-lg text-white transition hover:border-white/40"
                  onClick={() => onUpdateQuantity(item, Math.max(1, item.quantity - 1))}
                  disabled={item.quantity <= 1}
                >
                  −
                </button>
                <span className="w-10 text-center text-sm font-semibold">{item.quantity}</span>
                <button
                  type="button"
                  className="flex h-8 w-8 items-center justify-center rounded-full border border-white/20 text-lg text-white transition hover:border-white/40 disabled:opacity-40"
                  onClick={() => {
                    const next = item.quantity + 1;
                    if (typeof item.stock === "number" && next > item.stock) return;
                    onUpdateQuantity(item, next);
                  }}
                  disabled={typeof item.stock === "number" && item.quantity >= item.stock}
                >
                  +
                </button>
              </div>
              <button
                type="button"
                onClick={() => onRemove(item)}
                className="text-xs font-semibold text-rose-300 hover:text-rose-200"
              >
                ลบออก
              </button>
            </div>
          </li>
        ))}
      </ul>

      <div className="flex items-center justify-between">
        <span className="text-sm text-white/60">ยอดรวม</span>
        <span className="text-2xl font-semibold text-emerald-200">
          ฿{total.toLocaleString("th-TH", { minimumFractionDigits: 2 })}
        </span>
      </div>

      <button
        type="button"
        onClick={onSubmit}
        className="flex w-full items-center justify-center gap-2 rounded-2xl bg-gradient-to-r from-emerald-500 to-teal-500 px-4 py-3 text-sm font-semibold text-white shadow-lg shadow-emerald-500/30 transition hover:from-emerald-600 hover:to-teal-500 focus:outline-none focus:ring-2 focus:ring-emerald-300 disabled:cursor-not-allowed disabled:opacity-60"
        disabled={isSubmitting}
      >
        {isSubmitting ? "กำลังสร้างคำสั่งซื้อ..." : "ยืนยันคำสั่งซื้อ"}
      </button>
    </section>
  );
}
