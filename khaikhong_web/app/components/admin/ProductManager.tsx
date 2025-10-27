"use client";

import { ChangeEvent, FormEvent, useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "react-toastify";
import type { Product } from "../../lib/orderApi";
import {
  createProduct,
  deleteProduct,
  fetchAdminProducts,
  updateProduct,
  type ProductPayload,
  type ProductVariantPayload,
} from "../../lib/adminApi";
import { Modal } from "../common/Modal";

type ProductFormState = ProductPayload;

const emptyForm: ProductFormState = {
  name: "",
  description: "",
  basePrice: 0,
  sku: "",
  baseStock: 0,
  options: [],
  variants: [],
};

function mapProductToForm(product: Product): ProductFormState {
  const optionValueLookup = new Map<string, { optionName: string; value: string }>();

  product.options.forEach((option) => {
    option.values.forEach((value) => {
      optionValueLookup.set(value.id, { optionName: option.name, value: value.value });
    });
  });

  return {
    productId: product.id,
    name: product.name,
    description: product.description ?? "",
    basePrice: product.basePrice,
    sku: product.sku,
    baseStock: product.baseStock ?? 0,
    options: product.options.map((option) => ({
      name: option.name,
      values: option.values.map((value) => value.value),
    })),
    variants: product.variants.map((variant) => ({
      sku: variant.sku,
      price: variant.price,
      stock: variant.stock ?? 0,
      selections: variant.combinations.map((combo) => {
        const details = optionValueLookup.get(combo.optionValueId);
        return (
          details ?? {
            optionName: "",
            value: "",
          }
        );
      }),
    })),
  };
}

export function ProductManager() {
  const queryClient = useQueryClient();
  const productsQuery = useQuery({
    queryKey: ["admin-products"],
    queryFn: fetchAdminProducts,
  });

  const [form, setForm] = useState<ProductFormState>(emptyForm);
  const [isEditing, setIsEditing] = useState(false);
  const [isModalOpen, setIsModalOpen] = useState(false);

  const closeModal = () => {
    setIsModalOpen(false);
    setIsEditing(false);
    setForm(emptyForm);
  };

  const createMutation = useMutation({
    mutationFn: createProduct,
    onSuccess: () => {
      toast.success("เพิ่มสินค้าเรียบร้อย");
      queryClient.invalidateQueries({ queryKey: ["admin-products"] });
      closeModal();
    },
    onError: (error) => {
      toast.error(error instanceof Error ? error.message : "ไม่สามารถเพิ่มสินค้าได้");
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ productId, payload }: { productId: string; payload: ProductPayload }) =>
      updateProduct(productId, payload),
    onSuccess: () => {
      toast.success("อัปเดตสินค้าสำเร็จ");
      queryClient.invalidateQueries({ queryKey: ["admin-products"] });
      closeModal();
    },
    onError: (error) => {
      toast.error(error instanceof Error ? error.message : "ไม่สามารถอัปเดตสินค้าได้");
    },
  });

  const deleteMutation = useMutation({
    mutationFn: deleteProduct,
    onSuccess: () => {
      toast.success("ลบสินค้าสำเร็จ");
      queryClient.invalidateQueries({ queryKey: ["admin-products"] });
    },
    onError: (error) => {
      toast.error(error instanceof Error ? error.message : "ไม่สามารถลบสินค้าได้");
    },
  });

  const isSaving = createMutation.isPending || updateMutation.isPending;

  const handleBaseFieldChange =
    (field: keyof ProductPayload) =>
    (event: ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
      const value =
        field === "basePrice" || field === "baseStock"
          ? Number(event.target.value)
          : event.target.value;
      setForm((prev) => ({ ...prev, [field]: value }));
    };

  const handleOptionNameChange = (index: number, value: string) => {
    setForm((prev) => {
      const options = [...prev.options];
      options[index] = { ...options[index], name: value };
      return { ...prev, options };
    });
  };

  const handleOptionValueChange = (optionIndex: number, valueIndex: number, value: string) => {
    setForm((prev) => {
      const options = [...prev.options];
      const values = [...options[optionIndex].values];
      values[valueIndex] = value;
      options[optionIndex] = { ...options[optionIndex], values };
      return { ...prev, options };
    });
  };

  const addOption = () => {
    setForm((prev) => ({
      ...prev,
      options: [...prev.options, { name: "", values: [""] }],
    }));
  };

  const removeOption = (index: number) => {
    setForm((prev) => {
      const optionToRemove = prev.options[index];
      const optionName = optionToRemove?.name ?? "";
      const options = prev.options.filter((_, i) => i !== index);
      const variants = prev.variants
        .map((variant) => ({
          ...variant,
          selections: variant.selections.filter(
            (selection) => selection.optionName !== optionName
          ),
        }))
        .filter((variant) => variant.selections.length === options.length || options.length === 0);

      return { ...prev, options, variants };
    });
  };

  const addOptionValue = (optionIndex: number) => {
    setForm((prev) => {
      const options = [...prev.options];
      options[optionIndex] = {
        ...options[optionIndex],
        values: [...options[optionIndex].values, ""],
      };
      return { ...prev, options };
    });
  };

  const removeOptionValue = (optionIndex: number, valueIndex: number) => {
    setForm((prev) => {
      const options = [...prev.options];
      const values = options[optionIndex].values.filter((_, idx) => idx !== valueIndex);
      options[optionIndex] = { ...options[optionIndex], values };
      return { ...prev, options };
    });
  };

  const handleVariantChange = <Field extends keyof ProductVariantPayload>(
    variantIndex: number,
    field: Field,
    value: ProductVariantPayload[Field]
  ) => {
    setForm((prev) => {
      const variants = [...prev.variants];
      variants[variantIndex] = {
        ...variants[variantIndex],
        [field]: value,
      };
      return { ...prev, variants };
    });
  };

  const addVariant = () => {
    setForm((prev) => ({
      ...prev,
      variants: [
        ...prev.variants,
        {
          sku: "",
          price: 0,
          stock: 0,
          selections: [],
        },
      ],
    }));
  };

  const removeVariant = (index: number) => {
    setForm((prev) => {
      const variants = prev.variants.filter((_, i) => i !== index);
      return { ...prev, variants };
    });
  };

  const updateVariantSelection = (
    variantIndex: number,
    optionName: string,
    value: string
  ) => {
    setForm((prev) => {
      const variants = [...prev.variants];
      const selections = [...(variants[variantIndex]?.selections ?? [])];
      const optionIndex = selections.findIndex((selection) => selection.optionName === optionName);

      if (optionIndex === -1) {
        selections.push({ optionName, value });
      } else {
        selections[optionIndex] = { optionName, value };
      }

      variants[variantIndex] = { ...variants[variantIndex], selections };
      return { ...prev, variants };
    });
  };

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const payload: ProductPayload = {
      ...form,
      basePrice: Number(form.basePrice) || 0,
      baseStock: Number(form.baseStock) || 0,
      variants: form.variants.map((variant) => ({
        ...variant,
        price: Number(variant.price) || 0,
        stock: Number(variant.stock) || 0,
        selections: variant.selections.filter(
          (selection) => selection.optionName.trim() && selection.value.trim()
        ),
      })),
      options: form.options
        .filter((option) => option.name.trim())
        .map((option) => ({
          name: option.name.trim(),
          values: option.values.map((value) => value.trim()).filter(Boolean),
        })),
    };

    if (isEditing && form.productId) {
      updateMutation.mutate({ productId: form.productId, payload });
    } else {
      createMutation.mutate(payload);
    }
  };

  const handleCreate = () => {
    setForm(emptyForm);
    setIsEditing(false);
    setIsModalOpen(true);
  };

  const handleEdit = (product: Product) => {
    setForm(mapProductToForm(product));
    setIsEditing(true);
    setIsModalOpen(true);
  };

  const handleDelete = (product: Product) => {
    if (window.confirm(`ต้องการลบสินค้า "${product.name}" ใช่หรือไม่?`)) {
      deleteMutation.mutate(product.id);
    }
  };

  const sortedProducts = useMemo(() => {
    return (productsQuery.data ?? []).slice().sort((a, b) => a.name.localeCompare(b.name));
  }, [productsQuery.data]);

  return (
    <div className="space-y-6">
      <section className="space-y-4">
        <header className="flex items-center justify-between text-white">
          <div>
            <h2 className="text-2xl font-semibold">รายการสินค้า</h2>
            <p className="text-sm text-white/60">
              รายการสินค้าทั้งหมดในระบบ พร้อมจัดการแก้ไข ลบ หรือเพิ่มตัวเลือก
            </p>
          </div>
          <button
            type="button"
            onClick={handleCreate}
            className="rounded-xl border border-white/20 px-4 py-2 text-xs font-semibold text-white transition hover:border-white/40"
          >
            เพิ่มสินค้าใหม่
          </button>
        </header>

        {productsQuery.isLoading ? (
          <div className="grid gap-4 lg:grid-cols-2">
            {Array.from({ length: 4 }).map((_, index) => (
              <div
                key={`product-skeleton-${index}`}
                className="animate-pulse rounded-3xl border border-white/10 bg-white/10 p-6 text-white"
              >
                <div className="h-6 w-2/3 rounded bg-white/20" />
                <div className="mt-4 h-4 w-full rounded bg-white/10" />
                <div className="mt-2 h-4 w-3/4 rounded bg-white/10" />
              </div>
            ))}
          </div>
        ) : sortedProducts.length === 0 ? (
          <div className="rounded-3xl border border-dashed border-white/20 bg-white/5 p-10 text-center text-sm text-white/60">
            ยังไม่มีสินค้าในระบบ
          </div>
        ) : (
          <div className="grid gap-4 lg:grid-cols-2">
            {sortedProducts.map((product) => (
              <article
                key={product.id}
                className="flex h-full flex-col justify-between rounded-3xl border border-white/10 bg-white/10 p-6 text-white shadow-lg transition hover:border-indigo-400/40 hover:shadow-indigo-400/20"
              >
                <div className="space-y-3">
                  <header>
                    <h3 className="text-lg font-semibold">{product.name}</h3>
                    <p className="text-xs uppercase tracking-widest text-white/60">
                      SKU: {product.sku}
                    </p>
                    {product.description ? (
                      <p className="mt-2 text-sm text-white/70">{product.description}</p>
                    ) : null}
                  </header>
                  <div className="flex flex-wrap gap-2 text-xs text-white/60">
                    <span className="rounded-full border border-white/20 px-3 py-1">
                      ฿{product.basePrice.toLocaleString("th-TH", { minimumFractionDigits: 2 })}
                    </span>
                    {typeof product.baseStock === "number" ? (
                      <span className="rounded-full border border-white/20 px-3 py-1">
                        สต็อกพื้นฐาน {product.baseStock}
                      </span>
                    ) : null}
                    {product.options.length ? (
                      <span className="rounded-full border border-white/20 px-3 py-1">
                        ตัวเลือก {product.options.length} รายการ
                      </span>
                    ) : null}
                    {product.variants.length ? (
                      <span className="rounded-full border border-white/20 px-3 py-1">
                        Variants {product.variants.length} แบบ
                      </span>
                    ) : null}
                  </div>

                  {product.options.length ? (
                    <div className="space-y-2 rounded-2xl border border-white/10 bg-white/5 p-4 text-xs text-white/70">
                      <p className="text-xs font-semibold text-white/80">ตัวเลือกสินค้า</p>
                      <ul className="space-y-1">
                        {product.options.map((option) => (
                          <li key={option.id}>
                            <span className="font-semibold text-white/80">{option.name}:</span>{" "}
                            {option.values.map((value) => value.value).join(", ")}
                          </li>
                        ))}
                      </ul>
                    </div>
                  ) : null}

                  {product.variants.length ? (
                    <div className="space-y-2 rounded-2xl border border-white/10 bg-white/5 p-4 text-xs text-white/70">
                      <p className="text-xs font-semibold text-white/80">Variants</p>
                      <ul className="space-y-2">
                        {product.variants.map((variant) => (
                          <li
                            key={variant.id}
                            className="rounded-xl border border-white/10 bg-white/5 p-3"
                          >
                            <div className="flex flex-wrap items-center gap-2 text-white/80">
                              <span className="text-white">SKU: {variant.sku}</span>
                              <span>
                                ฿{variant.price.toLocaleString("th-TH", { minimumFractionDigits: 2 })}
                              </span>
                              {typeof variant.stock === "number" ? (
                                <span>สต็อก {variant.stock}</span>
                              ) : null}
                            </div>
                            {variant.combinations.length ? (
                              <ul className="mt-2 space-y-1 text-white/60">
                                {variant.combinations.map((combination) => (
                                  <li key={combination.id}>รหัสตัวเลือก: {combination.optionValueId}</li>
                                ))}
                              </ul>
                            ) : null}
                          </li>
                        ))}
                      </ul>
                    </div>
                  ) : null}
                </div>

                <footer className="mt-4 flex items-center justify-end gap-3">
                  <button
                    type="button"
                    onClick={() => handleEdit(product)}
                    className="rounded-xl border border-white/20 px-4 py-2 text-xs font-semibold text-white transition hover:border-white/40"
                  >
                    แก้ไข
                  </button>
                  <button
                    type="button"
                    onClick={() => handleDelete(product)}
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
        title={isEditing ? "แก้ไขสินค้า" : "เพิ่มสินค้าใหม่"}
        description="กำหนดรายละเอียดสินค้า ตัวเลือก และตัวแปรต่างๆ"
      >
        <form className="space-y-6" onSubmit={handleSubmit}>
          <div className="grid gap-4 sm:grid-cols-2">
            <label className="space-y-2 text-sm">
              <span className="text-xs uppercase tracking-wider text-white/60">ชื่อสินค้า</span>
              <input
                value={form.name}
                onChange={handleBaseFieldChange("name")}
                required
                className="w-full rounded-xl border border-white/20 bg-white/10 px-4 py-2 text-sm text-white outline-none transition focus:border-indigo-300 focus:ring-2 focus:ring-indigo-300/40"
              />
            </label>
            <label className="space-y-2 text-sm">
              <span className="text-xs uppercase tracking-wider text-white/60">SKU</span>
              <input
                value={form.sku}
                onChange={handleBaseFieldChange("sku")}
                required
                className="w-full rounded-xl border border-white/20 bg-white/10 px-4 py-2 text-sm text-white outline-none transition focus:border-indigo-300 focus:ring-2 focus:ring-indigo-300/40"
              />
            </label>
          </div>

          <label className="space-y-2 text-sm">
            <span className="text-xs uppercase tracking-wider text-white/60">รายละเอียด</span>
            <textarea
              value={form.description}
              onChange={handleBaseFieldChange("description")}
              rows={3}
              className="w-full rounded-xl border border-white/20 bg-white/10 px-4 py-2 text-sm text-white outline-none transition focus:border-indigo-300 focus:ring-2 focus:ring-indigo-300/40"
            />
          </label>

          <div className="grid gap-4 sm:grid-cols-2">
            <label className="space-y-2 text-sm">
              <span className="text-xs uppercase tracking-wider text-white/60">ราคาพื้นฐาน</span>
              <input
                type="number"
                value={form.basePrice}
                onChange={handleBaseFieldChange("basePrice")}
                min={0}
                step="0.01"
                required
                className="w-full rounded-xl border border-white/20 bg-white/10 px-4 py-2 text-sm text-white outline-none transition focus:border-indigo-300 focus:ring-2 focus:ring-indigo-300/40"
              />
            </label>
            <label className="space-y-2 text-sm">
              <span className="text-xs uppercase tracking-wider text-white/60">สต็อกพื้นฐาน</span>
              <input
                type="number"
                value={form.baseStock}
                onChange={handleBaseFieldChange("baseStock")}
                min={0}
                className="w-full rounded-xl border border-white/20 bg-white/10 px-4 py-2 text-sm text-white outline-none transition focus:border-indigo-300 focus:ring-2 focus:ring-indigo-300/40"
              />
            </label>
          </div>

          <section className="space-y-4 rounded-2xl border border-white/10 bg-white/5 p-4">
            <header className="flex items-center justify-between">
              <div>
                <h3 className="text-base font-semibold text-white">ตัวเลือกสินค้า (Options)</h3>
                <p className="text-xs text-white/60">
                  ระบุกลุ่มตัวเลือก เช่น สี หรือ ขนาด พร้อมค่าที่ใช้
                </p>
              </div>
              <button
                type="button"
                onClick={addOption}
                className="rounded-xl border border-white/20 px-3 py-1 text-xs font-semibold text-white transition hover:border-white/40"
              >
                เพิ่มตัวเลือก
              </button>
            </header>

            <div className="space-y-4">
              {form.options.map((option, optionIndex) => (
                <div
                  key={`option-${optionIndex}`}
                  className="space-y-3 rounded-xl border border-white/10 bg-white/10 p-4"
                >
                  <div className="flex items-center justify-between gap-3">
                    <label className="flex-1 space-y-1 text-xs uppercase tracking-wider text-white/60">
                      ชื่อตัวเลือก
                      <input
                        value={option.name}
                        onChange={(event) => handleOptionNameChange(optionIndex, event.target.value)}
                        className="mt-1 w-full rounded-lg border border-white/20 bg-white/10 px-3 py-2 text-sm text-white outline-none transition focus:border-indigo-300 focus:ring-2 focus:ring-indigo-300/40"
                      />
                    </label>
                    <button
                      type="button"
                      onClick={() => removeOption(optionIndex)}
                      className="text-xs font-semibold text-rose-300 hover:text-rose-200"
                    >
                      ลบ
                    </button>
                  </div>

                  <div className="space-y-2">
                    <p className="text-xs uppercase tracking-wider text-white/60">ค่า (Values)</p>
                    {option.values.map((value, valueIndex) => (
                      <div key={`option-${optionIndex}-value-${valueIndex}`} className="flex gap-2">
                        <input
                          value={value}
                          onChange={(event) =>
                            handleOptionValueChange(optionIndex, valueIndex, event.target.value)
                          }
                          className="flex-1 rounded-lg border border-white/20 bg-white/10 px-3 py-2 text-sm text-white outline-none transition focus:border-indigo-300 focus:ring-2 focus:ring-indigo-300/40"
                        />
                        <button
                          type="button"
                          onClick={() => removeOptionValue(optionIndex, valueIndex)}
                          className="rounded-lg border border-white/20 px-2 text-xs text-white hover:border-white/40"
                        >
                          ลบ
                        </button>
                      </div>
                    ))}
                    <button
                      type="button"
                      onClick={() => addOptionValue(optionIndex)}
                      className="rounded-lg border border-dashed border-white/20 px-3 py-1 text-xs text-white hover:border-white/40"
                    >
                      เพิ่มค่า
                    </button>
                  </div>
                </div>
              ))}
              {form.options.length === 0 ? (
                <p className="text-xs text-white/60">ยังไม่ได้เพิ่มตัวเลือกสินค้า</p>
              ) : null}
            </div>
          </section>

          <section className="space-y-4 rounded-2xl border border-white/10 bg-white/5 p-4">
            <header className="flex items-center justify-between">
              <div>
                <h3 className="text-base font-semibold text-white">ตัวแปรสินค้า (Variants)</h3>
                <p className="text-xs text-white/60">
                  กำหนด SKU เฉพาะสำหรับชุดตัวเลือกที่แตกต่างกัน
                </p>
              </div>
              <button
                type="button"
                onClick={addVariant}
                className="rounded-xl border border-white/20 px-3 py-1 text-xs font-semibold text-white transition hover:border-white/40"
              >
                เพิ่ม Variant
              </button>
            </header>

            <div className="space-y-4">
              {form.variants.map((variant, variantIndex) => (
                <div
                  key={`variant-${variantIndex}`}
                  className="space-y-3 rounded-xl border border-white/10 bg-white/10 p-4"
                >
                  <div className="flex items-center justify-between gap-3">
                    <div className="grid flex-1 gap-3 sm:grid-cols-3">
                      <label className="space-y-1 text-xs uppercase tracking-wider text-white/60">
                        SKU
                        <input
                          value={variant.sku}
                          onChange={(event) =>
                            handleVariantChange(variantIndex, "sku", event.target.value)
                          }
                          className="mt-1 w-full rounded-lg border border-white/20 bg-white/10 px-3 py-2 text-sm text-white outline-none transition focus:border-indigo-300 focus:ring-2 focus:ring-indigo-300/40"
                        />
                      </label>
                      <label className="space-y-1 text-xs uppercase tracking-wider text-white/60">
                        ราคา
                        <input
                          type="number"
                          min={0}
                          step="0.01"
                          value={variant.price}
                          onChange={(event) =>
                            handleVariantChange(variantIndex, "price", Number(event.target.value))
                          }
                          className="mt-1 w-full rounded-lg border border-white/20 bg-white/10 px-3 py-2 text-sm text-white outline-none transition focus:border-indigo-300 focus:ring-2 focus:ring-indigo-300/40"
                        />
                      </label>
                      <label className="space-y-1 text-xs uppercase tracking-wider text-white/60">
                        สต็อก
                        <input
                          type="number"
                          min={0}
                          value={variant.stock}
                          onChange={(event) =>
                            handleVariantChange(variantIndex, "stock", Number(event.target.value))
                          }
                          className="mt-1 w-full rounded-lg border border-white/20 bg-white/10 px-3 py-2 text-sm text-white outline-none transition focus:border-indigo-300 focus:ring-2 focus:ring-indigo-300/40"
                        />
                      </label>
                    </div>
                    <button
                      type="button"
                      onClick={() => removeVariant(variantIndex)}
                      className="text-xs font-semibold text-rose-300 hover:text-rose-200"
                    >
                      ลบ
                    </button>
                  </div>

                  <div className="space-y-2">
                    <p className="text-xs uppercase tracking-wider text-white/60">
                      ตัวเลือกที่เลือกใช้ (Selections)
                    </p>
                    <div className="space-y-2">
                      {form.options.map((option) => {
                        const currentValue =
                          variant.selections.find(
                            (selection) => selection.optionName === option.name
                          )?.value ?? "";
                        return (
                          <div
                            key={`variant-${variantIndex}-option-${option.name}`}
                            className="flex flex-col gap-2 rounded-lg border border-white/10 bg-white/5 p-3"
                          >
                            <label className="text-xs font-semibold uppercase tracking-wider text-white/60">
                              {option.name}
                            </label>
                            <select
                              value={currentValue}
                              onChange={(event) =>
                                updateVariantSelection(
                                  variantIndex,
                                  option.name,
                                  event.target.value
                                )
                              }
                              className="w-full rounded-lg border border-white/20 bg-white/10 px-3 py-2 text-sm text-white outline-none transition focus:border-indigo-300 focus:ring-2 focus:ring-indigo-300/40"
                            >
                              <option value="">เลือกค่า</option>
                              {option.values.map((value) => (
                                <option key={value} value={value}>
                                  {value}
                                </option>
                              ))}
                            </select>
                          </div>
                        );
                      })}
                      {form.options.length === 0 ? (
                        <p className="text-xs text-white/60">
                          ยังไม่มีตัวเลือกสินค้า ต้องเพิ่มตัวเลือกก่อนจึงจะเพิ่ม Variant ได้
                        </p>
                      ) : null}
                    </div>
                  </div>
                </div>
              ))}
              {form.variants.length === 0 ? (
                <p className="text-xs text-white/60">ยังไม่มีการเพิ่ม Variant</p>
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
              disabled={isSaving}
              className="flex items-center justify-center gap-2 rounded-2xl bg-gradient-to-r from-indigo-500 to-purple-500 px-6 py-3 text-sm font-semibold text-white shadow-lg shadow-indigo-500/30 transition hover:from-indigo-600 hover:to-purple-500 focus:outline-none focus:ring-2 focus:ring-indigo-300 disabled:cursor-not-allowed disabled:opacity-60"
            >
              {isSaving ? "กำลังบันทึก..." : isEditing ? "บันทึกการแก้ไข" : "เพิ่มสินค้า"}
            </button>
          </div>
        </form>
      </Modal>
    </div>
  );
}
