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

  items: OrderItem[];
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

  items: OrderItem[];
}