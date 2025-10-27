import axios, { AxiosError, AxiosHeaders } from "axios";
import { BaseApiResponse } from "../types/api/response/BaseApiResponse.Type";
import { useAuthStore } from "../stores/authStore";

declare module "axios" {
  export interface AxiosRequestConfig {
    _retry?: boolean;
    skipAuth?: boolean;
  }
}

export type AuthTokens = {
  accessToken: string;
  refreshToken: string;
};

export const apiClient = axios.create({
  baseURL: process.env.NEXT_PUBLIC_API_URL ?? "",
  withCredentials: true,
});

const refreshClient = axios.create({
  baseURL: process.env.NEXT_PUBLIC_API_URL ?? "",
  withCredentials: true,
});

let refreshRequest: Promise<AuthTokens | null> | null = null;

export function isAxiosError(error: unknown): error is AxiosError {
  return (error as AxiosError)?.isAxiosError === true;
}

export function isApiError<T>(
  error: unknown
): error is AxiosError<BaseApiResponse<T>> {
  if (!isAxiosError(error)) return false;
  const data = error.response?.data as BaseApiResponse<T> | undefined;
  return (
    typeof data?.status === "number" &&
    typeof data?.message === "string" &&
    typeof data?.isSuccess === "boolean"
  );
}

function setAuthHeader(
  accessToken: string | null,
  headers?: AxiosHeaders | Record<string, unknown>
) {
  if (!headers) return;

  const value = accessToken ? `Bearer ${accessToken}` : undefined;

  if (headers instanceof AxiosHeaders) {
    if (value) {
      headers.set("Authorization", value);
    } else {
      headers.delete("Authorization");
    }
    return;
  }

  if (value) {
    (headers as Record<string, unknown>).Authorization = value;
  } else {
    delete (headers as Record<string, unknown>).Authorization;
  }
}

async function fetchRefreshToken(): Promise<AuthTokens | null> {
  const { accessToken } = useAuthStore.getState();
  if (!accessToken) return null;

  const response = await refreshClient.post<BaseApiResponse<AuthTokens>>(
    "/api/auth/refresh",
    undefined,
    {
      headers: {
        Authorization: `Bearer ${accessToken}`,
      },
    }
  );

  return response.data.data ?? null;
}

async function refreshTokens() {
  if (!refreshRequest) {
    refreshRequest = fetchRefreshToken()
      .then((tokens) => {
        if (!tokens) {
          useAuthStore.getState().clearAuth();
          return null;
        }

        useAuthStore.getState().setTokens(tokens);
        return tokens;
      })
      .catch((error) => {
        useAuthStore.getState().clearAuth();
        throw error;
      })
      .finally(() => {
        refreshRequest = null;
      });
  }

  return refreshRequest;
}

apiClient.interceptors.request.use((config) => {
  if (!config.headers) {
    config.headers = new AxiosHeaders(config.headers);
  }
  if (config.skipAuth) {
    setAuthHeader(null, config.headers as AxiosHeaders);
  } else {
    const { accessToken } = useAuthStore.getState();
    setAuthHeader(accessToken, config.headers as AxiosHeaders);
  }
  config.withCredentials = true;
  return config;
});

apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    if (
      error.response?.status === 401 &&
      !originalRequest._retry
    ) {
      originalRequest._retry = true;

      try {
        const tokens = await refreshTokens();
        if (!tokens) {
          return Promise.reject(error);
        }

        if (!originalRequest.headers) {
          originalRequest.headers = new AxiosHeaders(originalRequest.headers);
        }
        setAuthHeader(tokens.accessToken, originalRequest.headers as AxiosHeaders);
        return apiClient(originalRequest);
      } catch (refreshError) {
        return Promise.reject(refreshError);
      }
    }

    return Promise.reject(error);
  }
);
