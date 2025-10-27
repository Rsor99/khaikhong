"use client";

import { useState } from "react";
import type { Bundle } from "../../lib/orderApi";

type BundleListProps = {
  bundles: Bundle[] | undefined;
  isLoading: boolean;
  onAdd: (bundle: Bundle, quantity: number) => void;
};

export function BundleList({ bundles, isLoading, onAdd }: BundleListProps) {
  const [quantities, setQuantities] = useState<Record<string, number>>({});

  const handleQuantityChange = (bundleId: string, next: number) => {
    if (!Number.isFinite(next)) return;
    const rawQuantity = Math.max(1, Math.floor(next));
    const bundle = bundles?.find((item) => item.id === bundleId);
    const limit =
      typeof bundle?.availableBundles === "number" && bundle.availableBundles >= 1
        ? bundle.availableBundles
        : undefined;
    const quantity = limit ? Math.min(rawQuantity, limit) : rawQuantity;
    setQuantities((prev) => ({ ...prev, [bundleId]: quantity }));
  };

  if (isLoading) {
    return (
      <div className="grid gap-4 sm:grid-cols-2">
        {Array.from({ length: 2 }).map((_, index) => (
          <div
            key={index}
            className="animate-pulse rounded-2xl border border-white/10 bg-white/10 p-6"
          >
            <div className="h-6 w-2/3 rounded bg-white/20" />
            <div className="mt-4 h-4 w-full rounded bg-white/10" />
            <div className="mt-2 h-4 w-3/4 rounded bg-white/10" />
            <div className="mt-6 h-10 w-full rounded-xl bg-white/10" />
          </div>
        ))}
      </div>
    );
  }

  if (!bundles?.length) {
    return (
      <div className="rounded-2xl border border-white/10 bg-white/5 p-8 text-center text-sm text-white/60">
        ไม่พบชุดสินค้า
      </div>
    );
  }

  return (
    <div className="grid gap-4 sm:grid-cols-2">
      {bundles.map((bundle) => {
        const quantity = quantities[bundle.id] ?? 1;
        const maxBundle = bundle.availableBundles ?? undefined;
        const isOutOfStock = typeof maxBundle === "number" && maxBundle < 1;
        const savingsPositive =
          typeof bundle.savings === "number" && bundle.savings > 0;
        return (
          <article
            key={bundle.id}
            className="flex h-full flex-col justify-between rounded-2xl border border-white/10 bg-white/10 p-6 text-white shadow-lg transition hover:border-emerald-400/40 hover:shadow-emerald-400/20"
          >
            <div className="space-y-3">
              <header>
                <h3 className="text-lg font-semibold">{bundle.name}</h3>
                {bundle.description ? (
                  <p className="mt-1 text-sm text-white/70">{bundle.description}</p>
                ) : null}
              </header>
              <p className="text-xl font-semibold text-emerald-200">
                ฿{bundle.price.toLocaleString("th-TH", { minimumFractionDigits: 2 })}
              </p>
              <ul className="space-y-2 rounded-xl border border-white/10 bg-white/5 p-3 text-xs text-white/70">
                {bundle.products.map((product) => (
                  <li key={product.productId} className="space-y-1">
                    <div className="font-medium text-white/80">{product.name}</div>
                    {product.variants?.length ? (
                      <ul className="space-y-1 pl-3">
                        {product.variants.map((variant) => (
                          <li key={variant.variantId} className="list-disc">
                            SKU: {variant.sku}
                            {typeof variant.quantity === "number" ? ` • ${variant.quantity} ชิ้น` : null}
                          </li>
                        ))}
                      </ul>
                    ) : null}
                    {typeof product.quantity === "number" ? (
                      <div>จำนวน {product.quantity} ชิ้น</div>
                    ) : null}
                  </li>
                ))}
              </ul>
              <div className="flex flex-wrap items-center gap-3 text-xs text-white/60">
                {typeof maxBundle === "number" ? (
                  <span className="rounded-full border border-white/20 px-3 py-1">
                    เหลือ {maxBundle} ชุด
                  </span>
                ) : null}
                {savingsPositive ? (
                  <span className="rounded-full border border-emerald-300/60 bg-emerald-500/10 px-3 py-1 text-emerald-200">
                    ประหยัด ฿{bundle.savings!.toLocaleString("th-TH", { minimumFractionDigits: 2 })}
                  </span>
                ) : null}
              </div>
            </div>

            <footer className="mt-6 space-y-4">
              <label className="flex items-center gap-2 text-xs uppercase tracking-wider text-white/60">
                จำนวน
                <input
                  type="number"
                  min={1}
                  max={maxBundle}
                  value={quantity}
                  onChange={(event) =>
                    handleQuantityChange(bundle.id, Number(event.target.value))
                  }
                  className="w-24 rounded-xl border border-white/20 bg-white/10 px-3 py-2 text-sm text-white outline-none transition focus:border-emerald-300 focus:ring-2 focus:ring-emerald-300/40"
                />
                {typeof maxBundle === "number" ? (
                  <span className="text-[10px] text-white/50">สูงสุด {maxBundle}</span>
                ) : null}
              </label>

              <button
                type="button"
                onClick={() => {
                  onAdd(bundle, quantity);
                  setQuantities((prev) => ({ ...prev, [bundle.id]: 1 }));
                }}
                className="flex w-full items-center justify-center rounded-xl bg-emerald-500 px-4 py-2 text-sm font-semibold text-white shadow-lg shadow-emerald-500/30 transition hover:bg-emerald-600 focus:outline-none focus:ring-2 focus:ring-emerald-300"
                disabled={isOutOfStock}
              >
                {isOutOfStock ? "สินค้าหมดชั่วคราว" : "เพิ่มชุดสินค้าลงตะกร้า"}
              </button>
            </footer>
          </article>
        );
      })}
    </div>
  );
}
