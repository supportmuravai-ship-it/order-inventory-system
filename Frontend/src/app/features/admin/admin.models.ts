export interface AdminOrderKpis {
  totalOrders: number;
  new: number;
  confirmed: number;
  shipped: number;
  delivered: number;
  cancelled: number;
  noResponse: number;
  return: number;
  returnInProcess: number;
  repeatedOrder: number;
  returns: number;
  needsAttention: number;
  needToShip: number;
}

export interface AdminShopifyHealth {
  storeId: number;
  storeName: string;
  storeCode: string;
  isActive: boolean;
  shopDomain: string | null;
  connectionStatus: string;
  shopifyConnectedAtUtc: string | null;
  lastSuccessfulSyncAtUtc: string | null;
  lastReconciliationAtUtc: string | null;
  lastWebhookReceivedAtUtc: string | null;
  lastShopifyError: string | null;
}

export interface CreateAdminUserRequest {
  name: string;
  email: string;
  password: string;
  role: string;
  storeIds: number[];
}

export interface AdminUserListItem {
  id: string;
  name: string;
  email: string;
  isActive: boolean;
  roles: string[];
  storeIds: number[];
  stores: string[];
}

export interface CreateAdminStoreRequest {
  name: string;
  code: string;
  shopDomain: string;
}