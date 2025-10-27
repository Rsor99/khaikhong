"use client";

import { useState } from "react";
import type { Product, ProductVariant } from "../../lib/orderApi";

type ProductListProps = {
  products: Product[] | undefined;
  isLoading: boolean;
  onAddVariant: (product: Product, variant: ProductVariant, quantity: number) => void;
  onAddBase: (product: Product, quantity: number) => void;
};

export function ProductList({
  products,
  isLoading,
  onAddVariant,
  onAddBase,
}: ProductListProps) {
  const [quantities, setQuantities] = useState<Record<string, number>>({});
  const [optionSelections, setOptionSelections] = useState<
    Record<string, Record<string, string>>
  >({});

  const handleOptionSelect = (productId: string, optionId: string, valueId: string) => {
    setOptionSelections((prev) => ({
      ...prev,
      [productId]: {
        ...(prev[productId] ?? {}),
        [optionId]: valueId,
      },
    }));
  };

  const handleQuantityChange = (productId: string, next: number) => {
    if (!Number.isFinite(next)) return;
    const quantity = Math.max(1, Math.floor(next));
    setQuantities((prev) => ({ ...prev, [productId]: quantity }));
  };

  const handleAddVariant = (
    product: Product,
    variant: ProductVariant | undefined,
    quantity: number
  ) => {
    if (!variant) return;
    onAddVariant(product, variant, quantity);
    setQuantities((prev) => ({ ...prev, [product.id]: 1 }));
  };

  const handleAddBase = (product: Product, quantity: number) => {
    onAddBase(product, quantity);
    setQuantities((prev) => ({ ...prev, [product.id]: 1 }));
  };

  if (isLoading) {
    return (
      <div className="grid gap-4 sm:grid-cols-2">
        {Array.from({ length: 4 }).map((_, index) => (
          <div
            key={index}
            className="animate-pulse rounded-2xl border border-white/10 bg-white/10 p-6"
          >
            <div className="h-6 w-2/3 rounded bg-white/20" />
            <div className="mt-4 h-4 w-full rounded bg-white/10" />
            <div className="mt-2 h-4 w-1/2 rounded bg-white/10" />
            <div className="mt-6 h-10 w-full rounded-xl bg-white/10" />
          </div>
        ))}
      </div>
    );
  }

  if (!products?.length) {
    return (
      <div className="rounded-2xl border border-white/10 bg-white/5 p-8 text-center text-sm text-white/60">
        ไม่พบรายการสินค้า
      </div>
    );
  }

  return (
    <div className="grid gap-4 sm:grid-cols-2">
      {products.map((product) => {
        const quantity = quantities[product.id] ?? 1;
        const optionValueToOptionId = product.options.reduce<Record<string, string>>(
          (acc, option) => {
            option.values.forEach((value) => {
              acc[value.id] = option.id;
            });
            return acc;
          },
          {}
        );

        const defaultSelection = (() => {
          const selection: Record<string, string> = {};
          const seedVariant =
            product.variants.find((variant) => variant.combinations.length > 0) ??
            product.variants[0];

          if (seedVariant) {
            seedVariant.combinations.forEach((combo) => {
              const optionId = optionValueToOptionId[combo.optionValueId];
              if (optionId) selection[optionId] = combo.optionValueId;
            });
          }

          product.options.forEach((option) => {
            if (!selection[option.id] && option.values[0]) {
              selection[option.id] = option.values[0].id;
            }
          });

          return selection;
        })();

        const selectedOptions = {
          ...defaultSelection,
          ...(optionSelections[product.id] ?? {}),
        };

        const resolveVariant = (selection: Record<string, string>) => {
          if (!product.variants.length) return undefined;
          return product.variants.find((variant) => {
            if (!variant.combinations.length) return true;
            const combinationSet = new Set(
              variant.combinations.map((combo) => combo.optionValueId)
            );
            return product.options.every((option) => {
              const valueId = selection[option.id];
              if (!valueId) return false;
              return combinationSet.has(valueId);
            });
          });
        };

        const selectedVariant = resolveVariant(selectedOptions);

        const isSelectionValid = !product.variants.length || Boolean(selectedVariant);

        const isValueSelectable = (optionId: string, valueId: string) => {
          if (!product.variants.length) return true;
          const tentativeSelection = { ...selectedOptions, [optionId]: valueId };
          return Boolean(resolveVariant(tentativeSelection));
        };

        const hasVariants = product.variants.length > 0;
        const displayPrice = selectedVariant?.price ?? product.basePrice;
        const stockLabel = hasVariants
          ? selectedVariant?.stock ?? undefined
          : product.baseStock ?? undefined;

        return (
          <article
            key={product.id}
            className="flex h-full flex-col justify-between rounded-2xl border border-white/10 bg-white/10 p-6 text-white shadow-lg transition hover:border-indigo-400/40 hover:shadow-indigo-400/20"
          >
            <div className="space-y-2">
              <header>
                <h3 className="text-lg font-semibold">{product.name}</h3>
                {product.description ? (
                  <p className="mt-1 text-sm text-white/70">{product.description}</p>
                ) : null}
              </header>

              <p className="text-sm text-white/50">SKU: {selectedVariant?.sku ?? product.sku}</p>
              <p className="text-xl font-semibold text-indigo-200">
                ฿{displayPrice.toLocaleString("th-TH", { minimumFractionDigits: 2 })}
              </p>

              {hasVariants ? (
                <div className="space-y-3">
                  {product.options.map((option) => (
                    <div key={option.id} className="space-y-2">
                      <p className="text-xs font-semibold uppercase tracking-wider text-white/60">
                        {option.name}
                      </p>
                      <div className="flex flex-wrap gap-2">
                        {option.values.map((value) => {
                          const isActive = selectedOptions[option.id] === value.id;
                          const isAvailable = isValueSelectable(option.id, value.id);
                          return (
                            <button
                              key={value.id}
                              type="button"
                              onClick={() =>
                                handleOptionSelect(product.id, option.id, value.id)
                              }
                              disabled={!isAvailable}
                              className={[
                                "rounded-full px-4 py-2 text-xs font-semibold transition",
                                "border",
                                isActive
                                  ? "border-indigo-300 bg-indigo-500/20 text-white shadow-sm shadow-indigo-500/40"
                                  : "border-white/20 bg-white/5 text-white/70 hover:border-indigo-300/60 hover:text-white",
                                !isAvailable ? "opacity-40" : "",
                              ].join(" ")}
                            >
                              {value.value}
                            </button>
                          );
                        })}
                      </div>
                    </div>
                  ))}
                  {!isSelectionValid ? (
                    <p className="rounded-xl border border-amber-300/40 bg-amber-500/10 px-3 py-2 text-xs text-amber-100">
                      ไม่มีสินค้าในตัวเลือกนี้ กรุณาเปลี่ยนตัวเลือกอื่น
                    </p>
                  ) : null}
                </div>
              ) : null}
            </div>

            <footer className="mt-6 space-y-4">
              <div className="flex items-center justify-between gap-3">
                <label className="flex items-center gap-2 text-xs uppercase tracking-wider text-white/60">
                  จำนวน
                  <input
                    type="number"
                    min={1}
                    value={quantity}
                    onChange={(event) =>
                      handleQuantityChange(product.id, Number(event.target.value))
                    }
                    className="w-24 rounded-xl border border-white/20 bg-white/10 px-3 py-2 text-sm text-white outline-none transition focus:border-indigo-300 focus:ring-2 focus:ring-indigo-400/40"
                  />
                </label>
                {typeof stockLabel === "number" ? (
                  <span className="text-xs text-white/60">คงเหลือ {stockLabel}</span>
                ) : null}
              </div>

              <button
                type="button"
                onClick={() =>
                  hasVariants
                    ? handleAddVariant(product, selectedVariant, quantity)
                    : handleAddBase(product, quantity)
                }
                className="flex w-full items-center justify-center rounded-xl bg-indigo-500 px-4 py-2 text-sm font-semibold text-white shadow-lg shadow-indigo-500/30 transition hover:bg-indigo-600 focus:outline-none focus:ring-2 focus:ring-indigo-300 disabled:cursor-not-allowed disabled:bg-indigo-200"
                disabled={hasVariants && !selectedVariant}
              >
                เพิ่มลงตะกร้า
              </button>
            </footer>
          </article>
        );
      })}
    </div>
  );
}
