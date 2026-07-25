// ============================================================================
// [ZUSTAND AUTH & USER IDENTITY STORE - ZERO-TRUST TELEMETRY]
// IMPLEMENTS: getUsername, customer loyalty CRM tracking (RewardPoints)
// ============================================================================
import { create } from "zustand";
import { UserIdentityResponse } from "@/types";
import { storefrontApi } from "@/services/api/client";

interface AuthState extends UserIdentityResponse {
  isLoading: boolean;
  token: string | null;
  
  // Actions
  getUsername: () => Promise<string>;
  syncIdentity: () => Promise<void>;
  setToken: (token: string) => void;
  logout: () => void;
}

export const useAuthStore = create<AuthState>((set, get) => ({
  username: "Guest Shopper",
  customerName: undefined,
  phoneNumber: undefined,
  customerType: "Standard Visitor",
  rewardPoints: 0,
  isAuthenticated: false,
  isLoading: false,
  token: typeof window !== "undefined" ? localStorage.getItem("digipose_jwt_token") : null,

  getUsername: async (): Promise<string> => {
    if (!get().isAuthenticated) {
      await get().syncIdentity();
    }
    return get().username;
  },

  syncIdentity: async () => {
    set({ isLoading: true });
    try {
      const data = await storefrontApi.getUserIdentity();
      set({
        username: data.username,
        customerName: data.customerName,
        phoneNumber: data.phoneNumber,
        customerType: data.customerType,
        rewardPoints: data.rewardPoints,
        isAuthenticated: data.isAuthenticated,
        isLoading: false,
      });
    } catch (e) {
      set({
        username: "Guest Shopper",
        customerType: "Standard Visitor",
        rewardPoints: 0,
        isAuthenticated: false,
        isLoading: false,
      });
    }
  },

  setToken: (token: string) => {
    if (typeof window !== "undefined") {
      localStorage.setItem("digipose_jwt_token", token);
    }
    set({ token, isAuthenticated: true });
    get().syncIdentity();
  },

  logout: () => {
    if (typeof window !== "undefined") {
      localStorage.removeItem("digipose_jwt_token");
    }
    set({
      token: null,
      username: "Guest Shopper",
      customerName: undefined,
      customerType: "Standard Visitor",
      rewardPoints: 0,
      isAuthenticated: false,
    });
  },
}));
