"use client";

import { apiClient, isApiError } from "./ApiClient";
import type { BaseApiResponse } from "../types/api/response/BaseApiResponse.Type";

export type ProductOptionValue = {
  id: string;
  value: string;
};

export type ProductOption = {
  id: string;
  name: string;
  values: ProductOptionValue[];
};

export type ProductVariantCombination = {
  id: string;
  optionValueId: string;
};

export type ProductVariant = {
  id: string;
  sku: string;
  price: number;
  stock: number | null;
  combinations: ProductVariantCombination[];
};

export type Product = {
  id: string;
  name: string;
  description: string | null;
  basePrice: number;
  sku: string;
  baseStock: number | null;
  options: ProductOption[];
  variants: ProductVariant[];
};

export type BundleProductVariant = {
  variantId: string;
  sku: string;
  quantity: number | null;
};

export type BundleProduct = {
  productId: string;
  name: string;
  quantity: number | null;
  variants: BundleProductVariant[] | null;
};

export type Bundle = {
  id: string;
  name: string;
  description: string | null;
  price: number;
  availableBundles?: number | null;
  savings?: number | null;
  products: BundleProduct[];
};

export type OrderItemType = "product" | "variant" | "bundle";

export type OrderItemPayload = {
  id: string;
  type: OrderItemType;
  quantity: number;
};

type ProductListResponse = BaseApiResponse<Product[]>;
type BundleListResponse = BaseApiResponse<Bundle[]>;
type CreateOrderResponse = BaseApiResponse<{
  orderId: string;
  itemCount: number;
}>;

export async function fetchProducts(): Promise<Product[]> {
  try {
    const { data } = await apiClient.get<ProductListResponse>("/api/products");
    return data.data ?? [];
  } catch (error) {
    if (isApiError<Product[]>(error)) {
      throw new Error(error.response?.data.message ?? "Unable to load products");
    }
    throw error;
  }
}

export async function fetchBundles(): Promise<Bundle[]> {
  try {
    const { data } = await apiClient.get<BundleListResponse>("/api/bundles");
    return data.data ?? [];
  } catch (error) {
    if (isApiError<Bundle[]>(error)) {
      throw new Error(error.response?.data.message ?? "Unable to load bundles");
    }
    throw error;
  }
}

export async function createOrder(items: OrderItemPayload[]) {
  try {
    const { data } = await apiClient.post<CreateOrderResponse>("/api/orders", {
      items,
    });
    return data.data;
  } catch (error) {
    if (isApiError<CreateOrderResponse["data"]>(error)) {
      throw new Error(error.response?.data.message ?? "Unable to create order");
    }
    throw error;
  }
}
