"use client";

import { apiClient, isApiError } from "./ApiClient";
import type {
  BaseApiResponse,
  ValidationErrorDetail,
} from "../types/api/response/BaseApiResponse.Type";
import type { Bundle, Product } from "./orderApi";

export type ProductOptionPayload = {
  name: string;
  values: string[];
};

export type ProductVariantSelectionPayload = {
  optionName: string;
  value: string;
};

export type ProductVariantPayload = {
  sku: string;
  price: number;
  stock: number;
  selections: ProductVariantSelectionPayload[];
};

export type ProductPayload = {
  productId?: string;
  name: string;
  description: string;
  basePrice: number;
  sku: string;
  baseStock: number;
  options: ProductOptionPayload[];
  variants: ProductVariantPayload[];
};

export type BundleItemVariantPayload = {
  variantId: string;
  quantity: number | null;
};

export type BundleItemPayload = {
  productId: string;
  quantity: number | null;
  variants: BundleItemVariantPayload[] | null;
};

export type BundlePayload = {
  bundleId?: string;
  name: string;
  description: string;
  price: number;
  products: BundleItemPayload[];
};

type ProductListResponse = BaseApiResponse<Product[]>;

type ProductMutationResponse = BaseApiResponse<{
  id: string;
  basePrice: number;
}>;

type BundleListResponse = BaseApiResponse<Bundle[]>;

type BundleMutationResponse = BaseApiResponse<{
  id: string;
  name: string;
}>;

export async function fetchAdminProducts(): Promise<Product[]> {
  try {
    const { data } = await apiClient.get<ProductListResponse>("/api/products");
    return data.data ?? [];
  } catch (error) {
    if (isApiError<Product[]>(error)) {
      const response = error.response?.data;
      const validationError = (response?.errors as ValidationErrorDetail[])?.[0]?.error;
      throw new Error(validationError ?? response?.message ?? "Unable to load products");
    }
    throw error;
  }
}

export async function createProduct(payload: ProductPayload) {
  try {
    const { data } = await apiClient.post<ProductMutationResponse>(
      "/api/products",
      payload
    );
    return data.data;
  } catch (error) {
    if (isApiError<ProductMutationResponse["data"]>(error)) {
      const response = error.response?.data;
      const validationError = (response?.errors as ValidationErrorDetail[])?.[0]?.error;
      throw new Error(validationError ?? response?.message ?? "Unable to create product");
    }
    throw error;
  }
}

export async function updateProduct(
  productId: string,
  payload: ProductPayload
) {
  try {
    const body = { ...payload, productId };
    const { data } = await apiClient.put<ProductMutationResponse>(
      `/api/products/${productId}`,
      body
    );
    return data.data;
  } catch (error) {
    if (isApiError<ProductMutationResponse["data"]>(error)) {
      const response = error.response?.data;
      const validationError = (response?.errors as ValidationErrorDetail[])?.[0]?.error;
      throw new Error(validationError ?? response?.message ?? "Unable to update product");
    }
    throw error;
  }
}

export async function deleteProduct(productId: string) {
  try {
    const { data } = await apiClient.delete<ProductMutationResponse>(
      `/api/products/${productId}`
    );
    return data.data;
  } catch (error) {
    if (isApiError<ProductMutationResponse["data"]>(error)) {
      const response = error.response?.data;
      const validationError = (response?.errors as ValidationErrorDetail[])?.[0]?.error;
      throw new Error(validationError ?? response?.message ?? "Unable to delete product");
    }
    throw error;
  }
}

export async function fetchAdminBundles(): Promise<Bundle[]> {
  try {
    const { data } = await apiClient.get<BundleListResponse>("/api/bundles");
    return data.data ?? [];
  } catch (error) {
    if (isApiError<Bundle[]>(error)) {
      const response = error.response?.data;
      const validationError = (response?.errors as ValidationErrorDetail[])?.[0]?.error;
      throw new Error(validationError ?? response?.message ?? "Unable to load bundles");
    }
    throw error;
  }
}

export async function createBundle(payload: BundlePayload) {
  try {
    const { data } = await apiClient.post<BundleMutationResponse>(
      "/api/bundles",
      payload
    );
    return data.data;
  } catch (error) {
    if (isApiError<BundleMutationResponse["data"]>(error)) {
      const response = error.response?.data;
      const validationError = (response?.errors as ValidationErrorDetail[])?.[0]?.error;
      throw new Error(validationError ?? response?.message ?? "Unable to create bundle");
    }
    throw error;
  }
}

export async function updateBundle(bundleId: string, payload: BundlePayload) {
  try {
    const body = { ...payload, bundleId };
    const { data } = await apiClient.put<BundleMutationResponse>(
      `/api/bundles/${bundleId}`,
      body
    );
    return data.data;
  } catch (error) {
    if (isApiError<BundleMutationResponse["data"]>(error)) {
      const response = error.response?.data;
      const validationError = (response?.errors as ValidationErrorDetail[])?.[0]?.error;
      throw new Error(validationError ?? response?.message ?? "Unable to update bundle");
    }
    throw error;
  }
}

export async function deleteBundle(bundleId: string) {
  try {
    const { data } = await apiClient.delete<BundleMutationResponse>(
      `/api/bundles/${bundleId}`
    );
    return data.data;
  } catch (error) {
    if (isApiError<BundleMutationResponse["data"]>(error)) {
      const response = error.response?.data;
      const validationError = (response?.errors as ValidationErrorDetail[])?.[0]?.error;
      throw new Error(validationError ?? response?.message ?? "Unable to delete bundle");
    }
    throw error;
  }
}
