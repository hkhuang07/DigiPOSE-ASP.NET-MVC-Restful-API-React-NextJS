// ============================================================================
// [DIGIPOSE AXIOS API CLIENT - LOW-LATENCY PRODUCTION BRIDGE]
// TARGETING ASP.NET CORE BACKEND HTTP://LOCALHOST:5128/API/V1
// ============================================================================
import axios, { AxiosInstance, InternalAxiosRequestConfig, AxiosResponse } from "axios";
import {
  CartSummaryResponse,
  CatalogSearchFilter,
  CatalogSearchResponse,
  CheckoutRequest,
  CheckoutResponse,
  UserIdentityResponse,
} from "@/types";

// Base Endpoint Target
const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5128/api/v1";

const apiClient: AxiosInstance = axios.create({
  baseURL: API_BASE_URL,
  timeout: 10000,
  headers: {
    "Content-Type": "application/json",
    "Accept": "application/json",
  },
});

// Request Interceptor: Attach JWT Bearer Token if present in storage
apiClient.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    if (typeof window !== "undefined") {
      const token = localStorage.getItem("digipose_jwt_token");
      if (token && config.headers) {
        config.headers.Authorization = `Bearer ${token}`;
      }
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Response Interceptor: Self-healing error logging
apiClient.interceptors.response.use(
  (response: AxiosResponse) => response,
  (error) => {
    console.error(">>> [API_ERROR_INTERCEPT]: HTTP request failed:", error?.response?.data || error.message);
    return Promise.reject(error);
  }
);

// ==========================================
// STOREFRONT & CART DOMAIN SERVICES (PHASE 6.2)
// ==========================================
export const storefrontApi = {
  /**
   * Retrieves active user identity and customer VIP/Reward points profile (getUsername).
   */
  getUserIdentity: async (): Promise<UserIdentityResponse> => {
    try {
      const res = await apiClient.get<UserIdentityResponse>("/storefront/user-identity");
      return res.data;
    } catch (e) {
      // Fallback for guest state when backend auth endpoint is unreachable or token missing
      return {
        username: "Guest Shopper",
        customerType: "Standard Visitor",
        rewardPoints: 0,
        isAuthenticated: false,
      };
    }
  },

  /**
   * Dynamic O(1) catalog search with multi-attribute filtering & automated SEO responses.
   */
  searchCatalog: async (filter: CatalogSearchFilter): Promise<CatalogSearchResponse> => {
    const res = await apiClient.post<CatalogSearchResponse>("/storefront/catalog/search", filter);
    return res.data;
  },

  /**
   * Fetches full Shopping Cart structure and auto-determines state (Card vs CardEmpty).
   */
  getShoppingCart: async (cartId: number): Promise<CartSummaryResponse> => {
    if (cartId <= 0) {
      return {
        cartId: 0,
        customerIdentity: "Guest Shopper",
        cartState: "CardEmpty",
        totalQuantity: 0,
        grossPrice: 0,
        totalTaxAmount: 0,
        totalDiscountAmount: 0,
        totalPrice: 0,
        items: [],
      };
    }
    const res = await apiClient.get<CartSummaryResponse>(`/storefront/cart/${cartId}`);
    return res.data;
  },

  /**
   * Adds product line item to cart (addItem / addToCart). Creates container if cartId == 0.
   */
  addToCart: async (cartId: number, productId: number, quantity: number = 1): Promise<{ cartId: number; cartState: string }> => {
    const res = await apiClient.post("/storefront/cart/add", { cartId, productId, quantity });
    return res.data;
  },

  /**
   * Adjusts quantity of existing line item (updateQuantity / increaseProduct / decreaseProduct).
   */
  updateQuantity: async (cartId: number, productId: number, newQuantity: number): Promise<void> => {
    await apiClient.put("/storefront/cart/update-quantity", { cartId, productId, newQuantity });
  },

  /**
   * Deletes a specific product line item from the active cart (removeItem / deleteProduct).
   */
  removeItem: async (cartId: number, productId: number): Promise<void> => {
    await apiClient.delete("/storefront/cart/remove", { data: { cartId, productId } });
  },

  /**
   * Clears all items in the shopping cart (removeAllItems / clearCart -> transitions to CardEmpty).
   */
  clearCart: async (cartId: number): Promise<void> => {
    await apiClient.post(`/storefront/cart/clear/${cartId}`);
  },

  /**
   * Finalizes checkout transaction (checkout).
   */
  checkout: async (request: CheckoutRequest): Promise<CheckoutResponse> => {
    const res = await apiClient.post<CheckoutResponse>("/storefront/checkout", request);
    return res.data;
  },
};

// ==========================================
// IN-STORE HIGH-SPEED POS TERMINAL SERVICES (PHASE 6.1)
// ==========================================
export const posApi = {
  createDraftOrder: async (branchId: number = 1, shiftId: number = 1, userId: number = 1) => {
    const res = await apiClient.post("/POS/retail-draft/create", { branchId, shiftId, userId });
    return res.data;
  },

  addItemToDraft: async (orderId: number, productId: number, quantity: number = 1) => {
    const res = await apiClient.post("/POS/retail-draft/add-item", { orderId, productId, quantity });
    return res.data;
  },

  removeItemFromDraft: async (orderId: number, productId: number) => {
    const res = await apiClient.post("/POS/retail-draft/remove-item", { orderId, productId });
    return res.data;
  },

  checkoutDraft: async (orderId: number, paymentMethodId: number = 1, customerId?: number) => {
    const res = await apiClient.post("/POS/retail-draft/checkout", { orderId, paymentMethodId, customerId });
    return res.data;
  },
};

export default apiClient;
