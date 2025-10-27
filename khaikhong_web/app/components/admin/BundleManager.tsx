"use client";

import { ChangeEvent, FormEvent, useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "react-toastify";
import type { Bundle, Product } from "../../lib/orderApi";
import {
  createBundle,
  deleteBundle,
  fetchAdminBundles,
  fetchAdminProducts,
  updateBundle,
  type BundleItemPayload,
  type BundlePayload,
} from "../../lib/adminApi";
import { Modal } from "../common/Modal";

type VariantSelection = {
  variantId: string;
  quantity: number | null;
};

type BundleProductForm = {
  productId: string;
  quantity: number | null;
  variants: VariantSelection[];
};

type BundleFormState = {
  bundleId?: string;
  name: string;
  description: string;
  price: number;
  products: BundleProductForm[];
};

const emptyForm: BundleFormState = {
  name: "",
  description: "",
  price: 0,
  products: [],
};

function mapBundleToForm(bundle: Bundle): BundleFormState {
  return {
    bundleId: bundle.id,
    name: bundle.name,
    description: bundle.description ?? "",
    price: bundle.price,
    products: bundle.products.map((product) => ({
      productId: product.productId,
      quantity: product.quantity,
      variants:
        product.variants?.map((variant) => ({
          variantId: variant.variantId,
          quantity: variant.quantity,
        })) ?? [],
    })),
  };
}

function findProduct(products: Product[], productId: string) {
  return products.find((product) => product.id === productId);
}

export function BundleManager() {
  const queryClient = useQueryClient();

  const productsQuery = useQuery({
    queryKey: ["admin-products"],
    queryFn: fetchAdminProducts,
  });

  const bundlesQuery = useQuery({
    queryKey: ["admin-bundles"],
    queryFn: fetchAdminBundles,
  });

  const [form, setForm] = useState<BundleFormState>(emptyForm);
  const [isEditing, setIsEditing] = useState(false);
  const [isModalOpen, setIsModalOpen] = useState(false);

  const closeModal = () => {
    setForm(emptyForm);
    setIsEditing(false);
    setIsModalOpen(false);
  };

  const createMutation = useMutation({
    mutationFn: createBundle,
    onSuccess: () => {
      toast.success("สร้าง Bundle สำเร็จ");
      queryClient.invalidateQueries({ queryKey: ["admin-bundles"] });
      queryClient.invalidateQueries({ queryKey: ["admin-products"] });
      closeModal();
    },
    onError: (error) => {
      toast.error(error instanceof Error ? error.message : "ไม่สามารถสร้าง Bundle ได้");
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ bundleId, payload }: { bundleId: string; payload: BundlePayload }) =>
      updateBundle(bundleId, payload),
    onSuccess: () => {
      toast.success("อัปเดต Bundle สำเร็จ");
      queryClient.invalidateQueries({ queryKey: ["admin-bundles"] });
      queryClient.invalidateQueries({ queryKey: ["admin-products"] });
      closeModal();
    },
    onError: (error) => {
      toast.error(error instanceof Error ? error.message : "ไม่สามารถอัปเดต Bundle ได้");
    },
  });

  const deleteMutation = useMutation({
    mutationFn: deleteBundle,
    onSuccess: () => {
      toast.success("ลบ Bundle สำเร็จ");
      queryClient.invalidateQueries({ queryKey: ["admin-bundles"] });
      queryClient.invalidateQueries({ queryKey: ["admin-products"] });
    },
    onError: (error) => {
      toast.error(error instanceof Error ? error.message : "ไม่สามารถลบ Bundle ได้");
    },
  });

  const isSaving = createMutation.isPending || updateMutation.isPending;

  const products = productsQuery.data ?? [];

  const handleFieldChange =
    (field: keyof BundleFormState) =>
    (event: ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
      const value = field === "price" ? Number(event.target.value) : event.target.value;
      setForm((prev) => ({ ...prev, [field]: value }));
    };

  const handleProductChange = (index: number, value: Partial<BundleProductForm>) => {
    setForm((prev) => {
      const productsState = [...prev.products];
      const current = productsState[index];
      let next: BundleProductForm = { ...current, ...value };
      if (value.productId && value.productId !== current.productId) {
        next = {
          productId: value.productId,
          quantity: null,
          variants: [],
        };
      }
      productsState[index] = next;
      return { ...prev, products: productsState };
    });
  };

  const handleVariantChange = (
    productIndex: number,
    variantIndex: number,
    value: Partial<VariantSelection>
  ) => {
    setForm((prev) => {
      const productsState = [...prev.products];
      const variants = [...productsState[productIndex].variants];
      variants[variantIndex] = { ...variants[variantIndex], ...value };
      productsState[productIndex] = { ...productsState[productIndex], variants };
      return { ...prev, products: productsState };
    });
  };

  const addProductToBundle = () => {
    if (!products.length) return;
    setForm((prev) => ({
      ...prev,
      products: [
        ...prev.products,
        {
          productId: products[0].id,
          quantity: null,
          variants: [],
        },
      ],
    }));
  };

  const removeProductFromBundle = (index: number) => {
    setForm((prev) => ({
      ...prev,
      products: prev.products.filter((_, i) => i !== index),
    }));
  };

  const addVariantToProduct = (index: number) => {
    const product = findProduct(products, form.products[index]?.productId);
    if (!product?.variants.length) {
      toast.info("สินค้านี้ไม่มี Variant ให้เลือก");
      return;
    }
    setForm((prev) => {
      const productsState = [...prev.products];
      const variants = [
        ...productsState[index].variants,
        { variantId: product.variants[0].id, quantity: 1 },
      ];
      productsState[index] = { ...productsState[index], variants };
      return { ...prev, products: productsState };
    });
  };

  const removeVariantFromProduct = (productIndex: number, variantIndex: number) => {
    setForm((prev) => {
      const productsState = [...prev.products];
      const variants = productsState[productIndex].variants.filter((_, idx) => idx !== variantIndex);
      productsState[productIndex] = { ...productsState[productIndex], variants };
      return { ...prev, products: productsState };
    });
  };

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const payload: BundlePayload = {
      ...form,
      price: Number(form.price) || 0,
      products: form.products.map<BundleItemPayload>((item) => ({
        productId: item.productId,
        quantity: item.quantity ?? null,
        variants: item.variants.length
          ? item.variants.map((variant) => ({
              variantId: variant.variantId,
              quantity: variant.quantity ?? null,
            }))
          : null,
      })),
    };

    if (!payload.products.length) {
      toast.info("กรุณาเพิ่มสินค้าใน Bundle");
      return;
    }

    if (isEditing && form.bundleId) {
      updateMutation.mutate({ bundleId: form.bundleId, payload });
    } else {
      createMutation.mutate(payload);
    }
  };

  const handleCreate = () => {
    if (!products.length) {
      toast.info("กรุณาเพิ่มสินค้าอย่างน้อย 1 รายการก่อนสร้าง Bundle");
      return;
    }
    setForm(emptyForm);
    setIsEditing(false);
    setIsModalOpen(true);
  };

  const handleEdit = (bundle: Bundle) => {
    if (!products.length) {
      toast.info("ไม่มีสินค้าพอสำหรับแก้ไข Bundle");
      return;
    }
    setForm(mapBundleToForm(bundle));
    setIsEditing(true);
    setIsModalOpen(true);
  };

  const handleDelete = (bundle: Bundle) => {
    if (window.confirm(`ต้องการลบ Bundle "${bundle.name}" ใช่หรือไม่?`)) {
      deleteMutation.mutate(bundle.id);
    }
  };

  const sortedBundles = useMemo(() => {
    return (bundlesQuery.data ?? []).slice().sort((a, b) => a.name.localeCompare(b.name));
  }, [bundlesQuery.data]);

  return (
    <div className="space-y-6">
      <section className="space-y-4">
        <header className="flex items-center justify-between text-white">
          <div>
            <h2 className="text-2xl font-semibold">รายการ Bundles</h2>
            <p className="text-sm text-white/60">
              จัดการชุดสินค้าพร้อมดูจำนวนที่เหลือและมูลค่าการประหยัด
            </p>
          </div>
          <button
            type="button"
            onClick={handleCreate}
            className="rounded-xl border border-white/20 px-4 py-2 text-xs font-semibold text-white transition hover:border-white/40"
          >
            สร้าง Bundle ใหม่
          </button>
        </header>

        {bundlesQuery.isLoading ? (
          <div className="grid gap-4 lg:grid-cols-2">
            {Array.from({ length: 4 }).map((_, index) => (
              <div
                key={`bundle-skeleton-${index}`}
                className="animate-pulse rounded-3xl border border-white/10 bg-white/10 p-6 text-white"
              >
                <div className="h-6 w-2/3 rounded bg-white/20" />
                <div className="mt-4 h-4 w-full rounded bg-white/10" />
                <div className="mt-2 h-4 w-3/4 rounded bg-white/10" />
              </div>
            ))}
          </div>
        ) : sortedBundles.length === 0 ? (
          <div className="rounded-3xl border border-dashed border-white/20 bg-white/5 p-10 text-center text-sm text-white/60">
            ยังไม่มี Bundle ในระบบ
          </div>
        ) : (
          <div className="grid gap-4 lg:grid-cols-2">
            {sortedBundles.map((bundle) => (
              <article
                key={bundle.id}
                className="flex h-full flex-col justify-between rounded-3xl border border-white/10 bg-white/10 p-6 text-white shadow-lg transition hover:border-emerald-400/40 hover:shadow-emerald-400/20"
              >
                <div className="space-y-3">
                  <header>
                    <h3 className="text-lg font-semibold">{bundle.name}</h3>
                    {bundle.description ? (
                      <p className="mt-1 text-sm text-white/70">{bundle.description}</p>
                    ) : null}
                  </header>

                  <div className="flex flex-wrap gap-2 text-xs text-white/60">
                    <span className="rounded-full border border-white/20 px-3 py-1">
                      ฿{bundle.price.toLocaleString("th-TH", { minimumFractionDigits: 2 })}
                    </span>
                    {typeof bundle.availableBundles === "number" ? (
                      <span className="rounded-full border border-white/20 px-3 py-1">
                        เหลือ {bundle.availableBundles} ชุด
                      </span>
                    ) : null}
                    {typeof bundle.savings === "number" && bundle.savings > 0 ? (
                      <span className="rounded-full border border-emerald-300/60 bg-emerald-500/10 px-3 py-1 text-emerald-200">
                        ประหยัด ฿
                        {bundle.savings.toLocaleString("th-TH", { minimumFractionDigits: 2 })}
                      </span>
                    ) : null}
                  </div>

                  <div className="space-y-2 rounded-2xl border border-white/10 bg-white/5 p-4 text-xs text-white/70">
                    <p className="text-xs font-semibold text-white/80">สินค้าในชุด</p>
                    <ul className="space-y-2">
                      {bundle.products.map((product) => (
                        <li key={product.productId} className="rounded-xl border border-white/10 bg-white/5 p-3">
                          <p className="text-white/80">{product.name}</p>
                          {typeof product.quantity === "number" ? (
                            <p className="text-white/60">จำนวน {product.quantity} ชิ้น</p>
                          ) : null}
                          {product.variants?.length ? (
                            <ul className="mt-2 space-y-1 text-white/60">
                              {product.variants.map((variant) => (
                                <li
                                  key={variant.variantId}
                                  className="rounded border border-white/10 bg-white/5 px-3 py-2"
                                >
                                  SKU: {variant.sku} • จำนวน {variant.quantity}
                                </li>
                              ))}
                            </ul>
                          ) : null}
                        </li>
                      ))}
                    </ul>
                  </div>
                </div>

                <footer className="mt-4 flex items-center justify-end gap-3">
                  <button
                    type="button"
                    onClick={() => handleEdit(bundle)}
                    className="rounded-xl border border-white/20 px-4 py-2 text-xs font-semibold text-white transition hover:border-white/40"
                  >
                    แก้ไข
                  </button>
                  <button
                    type="button"
                    onClick={() => handleDelete(bundle)}
                    className="rounded-xl border border-rose-300/40 px-4 py-2 text-xs font-semibold text-rose-200 transition hover:border-rose-300 hover:text-rose-100"
                  >
                    ลบ
                  </button>
                </footer>
              </article>
            ))}
          </div>
        )}
      </section>

      <Modal
        open={isModalOpen}
        onClose={closeModal}
        title={isEditing ? "แก้ไข Bundle" : "สร้าง Bundle ใหม่"}
        description="ผสานสินค้าเข้าด้วยกันเป็นชุดพร้อมกำหนดราคาพิเศษ"
      >
        <form className="space-y-6" onSubmit={handleSubmit}>
          <div className="grid gap-4 sm:grid-cols-2">
            <label className="space-y-2 text-sm">
              <span className="text-xs uppercase tracking-wider text-white/60">ชื่อ Bundle</span>
              <input
                value={form.name}
                onChange={handleFieldChange("name")}
                required
                className="w-full rounded-xl border border-white/20 bg-white/10 px-4 py-2 text-sm text-white outline-none transition focus:border-emerald-300 focus:ring-2 focus:ring-emerald-300/40"
              />
            </label>
            <label className="space-y-2 text-sm">
              <span className="text-xs uppercase tracking-wider text-white/60">ราคา Bundle (รวม)</span>
              <input
                type="number"
                min={0}
                step="0.01"
                value={form.price}
                onChange={handleFieldChange("price")}
                className="w-full rounded-xl border border-white/20 bg-white/10 px-4 py-2 text-sm text-white outline-none transition focus:border-emerald-300 focus:ring-2 focus:ring-emerald-300/40"
              />
            </label>
          </div>

          <label className="space-y-2 text-sm">
            <span className="text-xs uppercase tracking-wider text-white/60">รายละเอียด</span>
            <textarea
              value={form.description}
              onChange={handleFieldChange("description")}
              rows={3}
              className="w-full rounded-xl border border-white/20 bg-white/10 px-4 py-2 text-sm text-white outline-none transition focus:border-emerald-300 focus:ring-2 focus:ring-emerald-300/40"
            />
          </label>

          <section className="space-y-4 rounded-2xl border border-white/10 bg-white/5 p-4">
            <header className="flex flex-wrap items-center justify-between gap-3">
              <div>
                <h3 className="text-base font-semibold text-white">สินค้าภายใน Bundle</h3>
                <p className="text-xs text-white/60">
                  เลือกสินค้าและกำหนดจำนวนรวมถึง Variant ที่ต้องการ
                </p>
              </div>
              <button
                type="button"
                onClick={addProductToBundle}
                disabled={!products.length}
                className="rounded-xl border border-white/20 px-3 py-1 text-xs font-semibold text-white transition hover:border-white/40 disabled:cursor-not-allowed disabled:opacity-60"
              >
                เพิ่มสินค้า
              </button>
            </header>

            {!products.length ? (
              <p className="text-xs text-emerald-200">กรุณาเพิ่มสินค้าก่อนเพื่อสร้าง Bundle</p>
            ) : null}

            <div className="space-y-4">
              {form.products.map((bundleProduct, productIndex) => {
                const productData = findProduct(products, bundleProduct.productId) ?? products[0];
                const variants = productData?.variants ?? [];
                return (
                  <div
                    key={`bundle-product-${productIndex}`}
                    className="space-y-3 rounded-xl border border-white/10 bg-white/10 p-4"
                  >
                    <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
                      <label className="flex-1 space-y-1 text-xs uppercase tracking-wider text-white/60">
                        เลือกสินค้า
                        <select
                          value={bundleProduct.productId}
                          onChange={(event) =>
                            handleProductChange(productIndex, { productId: event.target.value })
                          }
                          className="mt-1 w-full rounded-lg border border-white/20 bg-white/10 px-3 py-2 text-sm text-white outline-none transition focus:border-emerald-300 focus:ring-2 focus:ring-emerald-300/40"
                        >
                          {products.map((product) => (
                            <option value={product.id} key={product.id}>
                              {product.name}
                            </option>
                          ))}
                        </select>
                      </label>
                      <label className="w-full space-y-1 text-xs uppercase tracking-wider text-white/60 sm:w-40">
                        จำนวนสินค้า (รวม)
                        <input
                          type="number"
                          min={0}
                          value={bundleProduct.quantity ?? ""}
                          onChange={(event) =>
                            handleProductChange(productIndex, {
                              quantity: event.target.value === "" ? null : Number(event.target.value),
                            })
                          }
                          placeholder="ตาม variant"
                          className="mt-1 w-full rounded-lg border border-white/20 bg-white/10 px-3 py-2 text-sm text-white outline-none transition focus:border-emerald-300 focus:ring-2 focus:ring-emerald-300/40"
                        />
                      </label>
                      <button
                        type="button"
                        onClick={() => removeProductFromBundle(productIndex)}
                        className="text-xs font-semibold text-rose-300 hover:text-rose-200"
                      >
                        ลบสินค้า
                      </button>
                    </div>

                    {variants.length ? (
                      <div className="space-y-3 rounded-lg border border-white/10 bg-white/5 p-3">
                        <div className="flex items-center justify-between">
                          <p className="text-xs font-semibold uppercase tracking-wider text-white/70">
                            เลือก Variant เฉพาะ
                          </p>
                          <button
                            type="button"
                            onClick={() => addVariantToProduct(productIndex)}
                            className="rounded-lg border border-dashed border-white/20 px-3 py-1 text-xs text-white hover:border-white/40"
                          >
                            เพิ่ม Variant
                          </button>
                        </div>
                        {bundleProduct.variants.length === 0 ? (
                          <p className="text-xs text-white/60">
                            ยังไม่ได้เลือก Variant เฉพาะ (ใช้จำนวนรวม)
                          </p>
                        ) : null}
                        {bundleProduct.variants.map((variantItem, variantIndex) => (
                          <div
                            key={`bundle-product-${productIndex}-variant-${variantIndex}`}
                            className="flex flex-col gap-2 rounded-lg border border-white/10 bg-white/5 p-3 sm:flex-row sm:items-center"
                          >
                            <label className="flex-1 space-y-1 text-xs uppercase tracking-wider text-white/60">
                              Variant
                              <select
                                value={variantItem.variantId}
                                onChange={(event) =>
                                  handleVariantChange(productIndex, variantIndex, {
                                    variantId: event.target.value,
                                  })
                                }
                                className="mt-1 w-full rounded-lg border border-white/20 bg-white/10 px-3 py-2 text-sm text-white outline-none transition focus:border-emerald-300 focus:ring-2 focus:ring-emerald-300/40"
                              >
                                {variants.map((variant) => (
                                  <option value={variant.id} key={variant.id}>
                                    {variant.sku} • ฿
                                    {variant.price.toLocaleString("th-TH", {
                                      minimumFractionDigits: 2,
                                    })}
                                  </option>
                                ))}
                              </select>
                            </label>
                            <label className="w-full space-y-1 text-xs uppercase tracking-wider text-white/60 sm:w-32">
                              จำนวน
                              <input
                                type="number"
                                min={0}
                                value={variantItem.quantity ?? ""}
                                onChange={(event) =>
                                  handleVariantChange(productIndex, variantIndex, {
                                    quantity:
                                      event.target.value === "" ? null : Number(event.target.value),
                                  })
                                }
                                className="mt-1 w-full rounded-lg border border-white/20 bg-white/10 px-3 py-2 text-sm text-white outline-none transition focus:border-emerald-300 focus:ring-2 focus:ring-emerald-300/40"
                              />
                            </label>
                            <button
                              type="button"
                              onClick={() => removeVariantFromProduct(productIndex, variantIndex)}
                              className="text-xs font-semibold text-rose-300 hover:text-rose-200"
                            >
                              ลบ Variant
                            </button>
                          </div>
                        ))}
                      </div>
                    ) : null}
                  </div>
                );
              })}
              {form.products.length === 0 ? (
                <p className="text-xs text-white/60">ยังไม่ได้เพิ่มสินค้าใน Bundle</p>
              ) : null}
            </div>
          </section>

          <div className="flex justify-end gap-3">
            <button
              type="button"
              onClick={closeModal}
              className="rounded-xl border border-white/20 px-4 py-2 text-sm font-semibold text-white transition hover:border-white/40"
            >
              ยกเลิก
            </button>
            <button
              type="submit"
              disabled={isSaving || !products.length}
              className="flex items-center justify-center gap-2 rounded-2xl bg-gradient-to-r from-emerald-500 to-teal-500 px-6 py-3 text-sm font-semibold text-white shadow-lg shadow-emerald-500/30 transition hover:from-emerald-600 hover:to-teal-500 focus:outline-none focus:ring-2 focus:ring-emerald-300 disabled:cursor-not-allowed disabled:opacity-60"
            >
              {isSaving ? "กำลังบันทึก..." : isEditing ? "บันทึกการแก้ไข" : "สร้าง Bundle"}
            </button>
          </div>
        </form>
      </Modal>
    </div>
  );
}
