"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "react-toastify";
import {
  Bundle,
  Product,
  ProductVariant,
  createOrder,
  fetchBundles,
  fetchProducts,
  type OrderItemPayload,
} from "../../lib/orderApi";
import { BundleList } from "./BundleList";
import { CartItem, CartSummary } from "./CartSummary";
import { ProductList } from "./ProductList";

function describeVariant(product: Product, variant: ProductVariant) {
  if (!variant.combinations.length) return product.name;

  const valueLookup = new Map<string, string>();
  product.options.forEach((option) => {
    option.values.forEach((value) => {
      valueLookup.set(value.id, `${option.name}: ${value.value}`);
    });
  });

  const details = variant.combinations
    .map((combo) => valueLookup.get(combo.optionValueId))
    .filter(Boolean)
    .join(" • ");

  return details ? `${product.name} (${details})` : product.name;
}

function clampQuantity(next: number, stock: number | null | undefined) {
  if (typeof stock !== "number") return Math.max(1, next);
  return Math.min(Math.max(1, next), stock);
}

export function UserOrderPanel() {
  const queryClient = useQueryClient();
  const [cartItems, setCartItems] = useState<CartItem[]>([]);

  const productsQuery = useQuery({
    queryKey: ["products"],
    queryFn: fetchProducts,
    staleTime: 60_000,
  });

  const bundlesQuery = useQuery({
    queryKey: ["bundles"],
    queryFn: fetchBundles,
    staleTime: 60_000,
  });

  const createOrderMutation = useMutation({
    mutationFn: (items: OrderItemPayload[]) => createOrder(items),
    onSuccess: (order) => {
      toast.success(
        order?.orderId
          ? `สร้างคำสั่งซื้อสำเร็จ (#${order.orderId})`
          : "สร้างคำสั่งซื้อสำเร็จ"
      );
      setCartItems([]);
      queryClient.invalidateQueries({ queryKey: ["products"] });
      queryClient.invalidateQueries({ queryKey: ["bundles"] });
    },
    onError: (error) => {
      if (error instanceof Error) {
        toast.error(error.message);
      } else {
        toast.error("ไม่สามารถสร้างคำสั่งซื้อได้");
      }
    },
  });

  const upsertCartItem = (nextItem: CartItem) => {
    setCartItems((prev) => {
      const index = prev.findIndex(
        (item) => item.id === nextItem.id && item.type === nextItem.type
      );

      if (index === -1) {
        return [...prev, nextItem];
      }

      const existing = prev[index];
      const updatedQuantity = clampQuantity(
        existing.quantity + nextItem.quantity,
        existing.stock ?? nextItem.stock
      );

      if (
        typeof existing.stock === "number" &&
        updatedQuantity === existing.stock &&
        existing.quantity + nextItem.quantity > existing.stock
      ) {
        toast.info("จำนวนเกินกว่าสต็อกที่มีอยู่ ปรับให้สูงสุดแทน");
      }

      const updatedItems = [...prev];
      updatedItems[index] = { ...existing, quantity: updatedQuantity };
      return updatedItems;
    });
  };

  const handleAddVariant = (
    product: Product,
    variant: ProductVariant,
    quantity: number
  ) => {
    if (typeof variant.stock === "number" && variant.stock < 1) {
      toast.info("สินค้าหมดชั่วคราว");
      return;
    }

    const item: CartItem = {
      id: variant.id,
      type: "variant",
      name: describeVariant(product, variant),
      sku: variant.sku,
      price: variant.price,
      quantity: clampQuantity(quantity, variant.stock),
      stock: variant.stock,
    };

    upsertCartItem(item);
  };

  const handleAddBase = (product: Product, quantity: number) => {
    if (typeof product.baseStock === "number" && product.baseStock < 1) {
      toast.info("สินค้าหมดชั่วคราว");
      return;
    }

    const item: CartItem = {
      id: product.id,
      type: "product",
      name: product.name,
      sku: product.sku,
      price: product.basePrice,
      quantity: clampQuantity(quantity, product.baseStock),
      stock: product.baseStock ?? undefined,
    };

    upsertCartItem(item);
  };

  const handleAddBundle = (bundle: Bundle, quantity: number) => {
    const item: CartItem = {
      id: bundle.id,
      type: "bundle",
      name: bundle.name,
      price: bundle.price,
      quantity: Math.max(1, quantity),
    };

    upsertCartItem(item);
  };

  const handleUpdateQuantity = (item: CartItem, quantity: number) => {
    setCartItems((prev) =>
      prev.map((cartItem) =>
        cartItem.id === item.id && cartItem.type === item.type
          ? { ...cartItem, quantity: clampQuantity(quantity, cartItem.stock) }
          : cartItem
      )
    );
  };

  const handleRemoveItem = (item: CartItem) => {
    setCartItems((prev) =>
      prev.filter(
        (cartItem) =>
          !(cartItem.id === item.id && cartItem.type === item.type)
      )
    );
  };

  const handleSubmitOrder = () => {
    if (!cartItems.length) {
      toast.info("กรุณาเพิ่มสินค้าในตะกร้าก่อนสั่งซื้อ");
      return;
    }

    const payload: OrderItemPayload[] = cartItems.map((item) => ({
      id: item.id,
      type: item.type,
      quantity: item.quantity,
    }));

    createOrderMutation.mutate(payload);
  };

  return (
    <div className="grid gap-6 lg:grid-cols-[2fr_1fr]">
      <section className="space-y-6">
        <div className="space-y-2 text-white">
          <h2 className="text-2xl font-semibold">สินค้า</h2>
          <p className="text-sm text-white/70">
            เลือกสินค้าและตัวเลือกเพื่อเพิ่มลงตะกร้า
          </p>
        </div>
        <ProductList
          products={productsQuery.data}
          isLoading={productsQuery.isLoading}
          onAddVariant={handleAddVariant}
          onAddBase={handleAddBase}
        />

        <div className="space-y-2 text-white">
          <h2 className="text-2xl font-semibold">ชุดสินค้า (Bundles)</h2>
          <p className="text-sm text-white/70">
            เลือกชุดสำเร็จรูปในราคาพิเศษ
          </p>
        </div>
        <BundleList
          bundles={bundlesQuery.data}
          isLoading={bundlesQuery.isLoading}
          onAdd={handleAddBundle}
        />
      </section>

      <CartSummary
        items={cartItems}
        isSubmitting={createOrderMutation.isPending}
        onUpdateQuantity={handleUpdateQuantity}
        onRemove={handleRemoveItem}
        onSubmit={handleSubmitOrder}
      />
    </div>
  );
}
