// ============================================================================
// [DIGIPOSE FRONTEND TYPE DEFINITIONS - MASTER ARCHITECTURE PHASE 6.1 & 6.2]
// STRICT 1-1 MAPPING TO ASP.NET CORE BACKEND DTOs
// ============================================================================

export interface SeoProductResponse {
  productId: number;
  sku: string;
  productName: string;
  basePrice: number;
  imageUrl: string;
  slug?: string;
  categoryName: string;
  manufacturerName?: string;
  productTypeName: string;
  isDigitalSaaS: boolean;
  metaTitle: string;
  metaDescription: string;
  metaKeywords: string;
  openGraphImage: string;
}

export interface CartDetailItem {
  productId: number;
  sku: string;
  productName: string;
  unitName: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
  lineTax: number;
  imageUrl: string;
}

export interface CartSummaryResponse {
  cartId: number;
  customerIdentity: string;
  cartState: "CardEmpty" | "Card";
  totalQuantity: number;
  grossPrice: number;
  totalTaxAmount: number;
  totalDiscountAmount: number;
  totalPrice: number;
  items: CartDetailItem[];
}

export interface CatalogSearchFilter {
  query?: string;
  categoryId?: number;
  productTypeId?: number;
  manufacturerId?: number;
  itemNatureId?: number; // 1 = Physical Retail, 2 = Digital SaaS Subscription
  minPrice?: number;
  maxPrice?: number;
  inStockOnly?: boolean;
  pageNumber: number;
  pageSize: number;
  sortBy: string; // name_asc, price_asc, price_desc, newest
}

export interface CatalogSearchResponse {
  totalRecords: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  products: SeoProductResponse[];
  seoGlobalMeta: {
    description: string;
    keywords: string;
    author: string;
  };
}

export interface UserIdentityResponse {
  username: string;
  customerName?: string;
  phoneNumber?: string;
  customerType: string;
  rewardPoints: number;
  isAuthenticated: boolean;
}

export interface CheckoutRequest {
  cartId: number;
  paymentMethodId: number;
  customerId?: number;
  shippingAddress?: string;
  contactPhone?: string;
  customerNotes?: string;
}

export interface CheckoutResponse {
  status: string;
  orderId: number;
  totalCharged: number;
  message: string;
}
