import { AuthTokens, apiClient, isApiError } from "./ApiClient";
import type { BaseApiResponse } from "../types/api/response/BaseApiResponse.Type";
import { useAuthStore, type UserProfile } from "../stores/authStore";

export type LoginPayload = {
  email: string;
  password: string;
};

export type RegisterPayload = {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  role: "Admin" | "User";
};

type LoginResponse = BaseApiResponse<AuthTokens>;

type LogoutResponse = BaseApiResponse<{ message: string }>;

type ProfileResponse = BaseApiResponse<UserProfile>;

type RegisterResponse = BaseApiResponse<{
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
}>;

function ensureSuccess<T>(response: BaseApiResponse<T>): T {
  if (!response.isSuccess || !response.data) {
    throw new Error(response.message || "Request failed");
  }

  return response.data;
}

export async function login(payload: LoginPayload): Promise<AuthTokens> {
  try {
    const { data } = await apiClient.post<LoginResponse>(
      "/api/auth/login",
      payload
    );
    return ensureSuccess(data);
  } catch (error) {
    if (isApiError<AuthTokens>(error)) {
      throw new Error(error.response?.data.message ?? "Unable to login");
    }
    throw error;
  }
}

export async function logout(): Promise<string> {
  try {
    const { data } = await apiClient.post<LogoutResponse>("/api/auth/logout");
    return ensureSuccess(data).message;
  } catch (error) {
    if (isApiError<{ message: string }>(error)) {
      throw new Error(error.response?.data.message ?? "Unable to logout");
    }
    throw error;
  }
}

export async function getUserProfile(): Promise<UserProfile> {
  try {
    const { data } = await apiClient.get<ProfileResponse>("/api/user");
    const profile = ensureSuccess(data);
    useAuthStore.getState().setUser(profile);
    return profile;
  } catch (error) {
    useAuthStore.getState().setUser(null);
    if (isApiError<UserProfile>(error)) {
      throw new Error(
        error.response?.data.message ?? "Unable to fetch profile"
      );
    }
    throw error;
  }
}

export async function registerUser(payload: RegisterPayload) {
  try {
    const { data } = await apiClient.post<RegisterResponse>(
      "/api/auth/register",
      payload,
      { skipAuth: true }
    );
    return ensureSuccess(data);
  } catch (error) {
    if (isApiError<RegisterResponse["data"]>(error)) {
      throw new Error(
        error.response?.data.message ?? "Unable to register account"
      );
    }
    throw error;
  }
}
