export interface OrderItem {
  id: number;
  productName: string;
  variantName: string | null;
  sku: string | null;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface OrderListItem {
  id: number;
  displayOrderId: string;
  orderDateUtc: string;

  fullName: string;
  phone: string;
  addressLine1: string;
  city: string;

  totalAmount: number;
  currency: string;

  trackingNumber: string | null;
  orderStatus: number;

  locationLink: string | null;
  finalDecision: string | null;

  orderSource: number;
  invoiceStatus: number;

  needsAttention: boolean;
  hoursInCurrentStatus: number;

  items: OrderItem[];
}

export interface OrderStatusHistory {
  oldStatus: number;
  newStatus: number;
  changedByUserId: string;
  changedBy: string;
  changedAtUtc: string;
}

export interface TrackingHistory {
  oldTrackingNumber: string | null;
  newTrackingNumber: string | null;
  changedByUserId: string;
  changedBy: string;
  changedAtUtc: string;
}

export interface OrderDetails {
  id: number;
  displayOrderId: string;
  externalOrderId: string | null;
  orderDateUtc: string;

  fullName: string;
  phone: string;
  addressLine1: string;
  city: string;
  country: string;

  totalAmount: number;
  currency: string;

  trackingNumber: string | null;
  orderStatus: number;

  locationLink: string | null;
  finalDecision: string | null;

  orderSource: number;
  invoiceStatus: number;

  warehouseName: string | null;
  lastStatusChangedAtUtc: string;

  needsAttention: boolean;
  hoursInCurrentStatus: number;

  statusHistory: OrderStatusHistory[];
  trackingHistory: TrackingHistory[];

  items: OrderItem[];
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface OrderQuery {
  page: number;
  pageSize: number;
  search?: string;

  dateFrom?: string;
  dateTo?: string;

  orderStatus?: number;
  product?: string;
  sku?: string;

  orderSource?: number;
  invoiceStatus?: number;

  sort?: string;
  needsAttention?: boolean;
}

export interface OrderSummary {
  totalOrders: number;
  confirmed: number;
  shipped: number;
  delivered: number;
  noResponse: number;
  return: number;
  returnInProcess: number;
  cancelled: number;
  repeatedOrder: number;
  needsAttention: number;
}