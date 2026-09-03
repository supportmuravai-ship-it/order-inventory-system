export enum TicketStatus {
  Open = 0,
  Closed = 1
}

export interface TicketListItem {
  id: number;

  orderId: number | null;
  displayOrderId: string | null;

  assignedToUserId: string;
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

  assignedToUserId: string;
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
  assignedToUserId: string;
  title: string;
  message: string;
}

export interface UpdateTicketAssignmentRequest {
  assignedToUserId: string;
}

export interface CreateTicketFromPageRequest {
  assignedToUserId: string;
  displayOrderId: string | null;
  title: string;
  message: string;
}