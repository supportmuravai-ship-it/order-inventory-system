export enum TicketStatus {
  Open = 0,
  Closed = 1
}

export interface TicketListItem {
  id: number;

  orderId: number | null;
  displayOrderId: string | null;

  // Kept for backward compatibility.
  // Backend returns first assigned user's ID.
  assignedToUserId: string;

  // Backend returns all assigned emails joined by commas.
  assignedToEmail: string;

  createdByUserId: string;
  createdByEmail: string;

  title: string;
  status: TicketStatus;

  createdAtUtc: string;
  closedAtUtc: string | null;
}

export interface TicketDetails {
  id: number;

  orderId: number | null;
  displayOrderId: string | null;

  // Kept for backward compatibility.
  assignedToUserId: string;

  // Backend returns all assigned emails joined by commas.
  assignedToEmail: string;

  createdByUserId: string;
  createdByEmail: string;

  closedByUserId: string | null;
  closedByEmail: string | null;

  title: string;
  message: string;

  status: TicketStatus;

  createdAtUtc: string;
  closedAtUtc: string | null;
}

export interface AssignableTicketUser {
  userId: string;
  email: string;
  roles: string[];
}

export interface TicketQuery {
  status?: TicketStatus;

  // Keep this singular.
  // Admin filter still filters tickets containing this user.
  assignedToUserId?: string;

  search?: string;

  page?: number;
  pageSize?: number;
}

export interface PagedTickets {
  items: TicketListItem[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface CreateTicketRequest {
  assignedToUserIds: string[];
  title: string;
  message: string;
}

export interface UpdateTicketAssignmentRequest {
  assignedToUserIds: string[];
}

export interface CreateTicketFromPageRequest {
  assignedToUserIds: string[];
  displayOrderId: string | null;
  title: string;
  message: string;
}