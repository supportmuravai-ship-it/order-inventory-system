import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';

import { AuthService } from '../../../core/auth/auth.service';
import { OrdersService } from '../../orders/services/orders.service';
import { OrderListItem, OrderSummary } from '../../orders/models/order.model';
import { HttpErrorResponse } from '@angular/common/http';
import { TicketService } from '../../Tickets/ticket.service';

@Component({
  selector: 'app-workspace',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './workspace.html',
})
export class WorkspaceComponent implements OnInit {
  readonly authService = inject(AuthService);

  private readonly ordersService = inject(OrdersService);
  private readonly router = inject(Router);
  private readonly ticketService = inject(TicketService);

  readonly orders = signal<OrderListItem[]>([]);

  readonly page = signal(1);
  readonly pageSize = signal(25);
  readonly totalCount = signal(0);
  readonly totalPages = signal(0);

  readonly search = signal('');

  readonly dateFrom = signal('');
  readonly dateTo = signal('');

  readonly selectedStatus = signal<number | undefined>(undefined);

  readonly productFilter = signal('');
  readonly skuFilter = signal('');

  readonly selectedSource = signal<number | undefined>(undefined);

  readonly selectedInvoiceStatus = signal<number | undefined>(undefined);

  readonly sort = signal('newest');

  // for inline status chnage option
  readonly editingStatusOrderId = signal<number | null>(null);
  readonly savingStatusOrderId = signal<number | null>(null);
  readonly statusEditValue = signal<number | null>(null);
  readonly statusUpdateMessage = signal('');

  readonly editingTrackingOrderId = signal<number | null>(null);
  readonly savingTrackingOrderId = signal<number | null>(null);
  readonly trackingEditValue = signal('');

  readonly editingInvoiceOrderId = signal<number | null>(null);
  readonly savingInvoiceOrderId = signal<number | null>(null);
  readonly invoiceEditValue = signal<number | null>(null);

  readonly editingLocationOrderId = signal<number | null>(null);
  readonly savingLocationOrderId = signal<number | null>(null);
  readonly locationEditValue = signal('');

  readonly editingFinalDecisionOrderId = signal<number | null>(null);
  readonly savingFinalDecisionOrderId = signal<number | null>(null);
  readonly finalDecisionEditValue = signal('');

  readonly needsAttentionOnly = signal(false);

  readonly editingShoaibNoteOrderId = signal<number | null>(null);
  readonly savingShoaibNoteOrderId = signal<number | null>(null);
  readonly shoaibNoteEditValue = signal('');

  readonly editingTrenvoNoteOrderId = signal<number | null>(null);
  readonly savingTrenvoNoteOrderId = signal<number | null>(null);
  readonly trenvoNoteEditValue = signal('');

  readonly mobileMenuOpen = signal(false);

  readonly openTicketCount = signal(0);

  readonly needToShipOnly = signal(false);
readonly savingNeedToShipOrderId = signal<number | null>(null);

  readonly summary = signal<OrderSummary>({
    totalOrders: 0,
    confirmed: 0,
    shipped: 0,
    delivered: 0,
    noResponse: 0,
    return: 0,
    returnInProcess: 0,
    cancelled: 0,
    repeatedOrder: 0,
    needsAttention: 0,
    new: 0,
    needToShip: 0
  });

  readonly summaryLoading = signal(true);

  readonly loading = signal(true);
  readonly errorMessage = signal('');

  ngOnInit(): void {
    this.loadOrders();
    this.loadSummary();
    this.loadOpenTicketCount();
  }

  private loadOrders(): void {
    const store = this.authService.selectedStore();

    if (!store) {
      this.router.navigate(['/stores']);
      return;
    }

    this.loading.set(true);
    this.errorMessage.set('');

    this.ordersService
      .getOrders(store.id, {
        page: this.page(),
        pageSize: this.pageSize(),
        search: this.search(),

        dateFrom: this.dateFrom() || undefined,
        dateTo: this.dateTo() || undefined,

        orderStatus: this.selectedStatus(),

        product: this.productFilter(),
        sku: this.skuFilter(),

        orderSource: this.selectedSource(),

        invoiceStatus: this.selectedInvoiceStatus(),

        needsAttention: this.needsAttentionOnly() ? true : undefined,
needToShip: this.needToShipOnly() ? true : undefined,

sort: this.sort(),
      })
      .subscribe({
        next: (result) => {
          if (result.totalPages > 0 && result.page > result.totalPages) {
            this.page.set(result.totalPages);
            this.loadOrders();
            return;
          }
          this.orders.set(result.items);
          this.page.set(result.page);
          this.pageSize.set(result.pageSize);
          this.totalCount.set(result.totalCount);
          this.totalPages.set(result.totalPages);

          this.loading.set(false);
        },

        error: (error: HttpErrorResponse) => {
          this.loading.set(false);

          if (typeof error.error === 'string' && error.error.trim()) {
            this.errorMessage.set(error.error);
            return;
          }

          this.errorMessage.set('Could not load orders.');
        },
      });
  }

  startTrackingEdit(order: OrderListItem): void {
    this.editingTrackingOrderId.set(order.id);
    this.trackingEditValue.set(order.trackingNumber ?? '');
  }

  cancelTrackingEdit(): void {
    this.editingTrackingOrderId.set(null);
    this.trackingEditValue.set('');
  }

  saveTracking(order: OrderListItem): void {
    const store = this.authService.selectedStore();

    if (!store) {
      return;
    }

    const value = this.trackingEditValue().trim();

    const trackingNumber = value === '' ? null : value;

    if (trackingNumber === order.trackingNumber) {
      this.cancelTrackingEdit();
      return;
    }

    this.savingTrackingOrderId.set(order.id);
    this.errorMessage.set('');
    this.statusUpdateMessage.set('');

    this.ordersService.updateTrackingNumber(store.id, order.id, trackingNumber).subscribe({
      next: () => {
        this.savingTrackingOrderId.set(null);
        this.editingTrackingOrderId.set(null);
        this.trackingEditValue.set('');

        this.statusUpdateMessage.set(
          `${order.displayOrderId} tracking number updated successfully.`,
        );

        // Reload actual SQL/backend data.
        this.loadOrders();
      },

      error: (error: HttpErrorResponse) => {
        this.savingTrackingOrderId.set(null);

        if (error.status === 403) {
          this.errorMessage.set('You are not allowed to update the tracking number.');
          return;
        }

        if (typeof error.error === 'string' && error.error.trim()) {
          this.errorMessage.set(error.error);
          return;
        }

        this.errorMessage.set('Could not update tracking number.');
      },
    });
  }

  applyFilters(): void {
    this.page.set(1);

    this.loadOrders();
    this.loadSummary();
  }

  clearFilters(): void {
    this.dateFrom.set('');
    this.dateTo.set('');

    this.selectedStatus.set(undefined);

    this.productFilter.set('');
    this.skuFilter.set('');

    this.selectedSource.set(undefined);

    this.selectedInvoiceStatus.set(undefined);

    this.page.set(1);
this.needsAttentionOnly.set(false);
this.needToShipOnly.set(false);

this.loadOrders();
    this.loadSummary();
  }

  showNeedsAttention(): void {
  this.needsAttentionOnly.set(true);
  this.needToShipOnly.set(false);
  this.page.set(1);

  this.loadOrders();
}

showNeedToShip(): void {
  this.needToShipOnly.set(true);
  this.needsAttentionOnly.set(false);
  this.page.set(1);

  this.loadOrders();
}

  showAllOrders(): void {
  this.needsAttentionOnly.set(false);
  this.needToShipOnly.set(false);
  this.page.set(1);

  this.loadOrders();
}

  changeSort(event: Event): void {
    const select = event.target as HTMLSelectElement;

    this.sort.set(select.value);
    this.page.set(1);

    this.loadOrders();
  }

  goToPreviousPage(): void {
    if (this.page() <= 1) {
      return;
    }

    this.page.update((value) => value - 1);

    this.loadOrders();
  }

  goToNextPage(): void {
    if (this.page() >= this.totalPages()) {
      return;
    }

    this.page.update((value) => value + 1);

    this.loadOrders();
  }

  changePageSize(event: Event): void {
    const select = event.target as HTMLSelectElement;

    const newPageSize = Number(select.value);

    this.pageSize.set(newPageSize);

    // Important:
    // Reset to first page when page size changes.
    this.page.set(1);

    this.loadOrders();
  }

  changeStore(): void {
    this.authService.selectedStore.set(null);

    sessionStorage.removeItem('selectedStoreId');

    this.router.navigate(['/stores']);
  }

  private loadSummary(): void {
    const store = this.authService.selectedStore();

    if (!store) {
      return;
    }

    this.summaryLoading.set(true);

    this.ordersService
      .getSummary(store.id, this.dateFrom() || undefined, this.dateTo() || undefined)
      .subscribe({
        next: (result) => {
          this.summary.set(result);

          this.summaryLoading.set(false);
        },

        error: (error: HttpErrorResponse) => {
          this.summaryLoading.set(false);

          if (typeof error.error === 'string' && error.error.trim()) {
            this.errorMessage.set(error.error);
          }
        },
      });
  }

  startStatusEdit(order: OrderListItem): void {
    this.editingStatusOrderId.set(order.id);
    this.statusEditValue.set(order.orderStatus);
    this.statusUpdateMessage.set('');
  }

  cancelStatusEdit(): void {
    this.editingStatusOrderId.set(null);
    this.statusEditValue.set(null);
  }

  saveStatus(order: OrderListItem): void {
    const store = this.authService.selectedStore();
    const newStatus = this.statusEditValue();

    if (!store || newStatus === null) {
      return;
    }

    if (newStatus === order.orderStatus) {
      this.cancelStatusEdit();
      return;
    }

    this.savingStatusOrderId.set(order.id);
    this.errorMessage.set('');
    this.statusUpdateMessage.set('');

    this.ordersService.updateOrderStatus(store.id, order.id, newStatus).subscribe({
      next: () => {
        this.savingStatusOrderId.set(null);
        this.editingStatusOrderId.set(null);
        this.statusEditValue.set(null);

        this.statusUpdateMessage.set(`${order.displayOrderId} status updated successfully.`);

        this.loadOrders();
        this.loadSummary();
      },

      error: (error: HttpErrorResponse) => {
        this.savingStatusOrderId.set(null);

        if (error.status === 403) {
          this.errorMessage.set('You are not allowed to update order status.');
          return;
        }

        if (typeof error.error === 'string' && error.error.trim()) {
          this.errorMessage.set(error.error);
          return;
        }

        this.errorMessage.set('Could not update order status.');
      },
    });
  }

  startLocationEdit(order: OrderListItem): void {
    this.editingLocationOrderId.set(order.id);
    this.locationEditValue.set(order.locationLink ?? '');
  }

  cancelLocationEdit(): void {
    this.editingLocationOrderId.set(null);
    this.locationEditValue.set('');
  }

  saveLocationLink(order: OrderListItem): void {
    const store = this.authService.selectedStore();

    if (!store) {
      return;
    }

    const value = this.locationEditValue().trim();

    const locationLink = value === '' ? null : value;

    if (locationLink === order.locationLink) {
      this.cancelLocationEdit();
      return;
    }

    this.savingLocationOrderId.set(order.id);
    this.errorMessage.set('');
    this.statusUpdateMessage.set('');

    this.ordersService.updateLocationLink(store.id, order.id, locationLink).subscribe({
      next: () => {
        this.savingLocationOrderId.set(null);
        this.editingLocationOrderId.set(null);
        this.locationEditValue.set('');

        this.statusUpdateMessage.set(`${order.displayOrderId} location link updated successfully.`);

        this.loadOrders();
      },

      error: (error: HttpErrorResponse) => {
        this.savingLocationOrderId.set(null);

        if (error.status === 403) {
          this.errorMessage.set('You are not allowed to update the location link.');
          return;
        }

        if (typeof error.error === 'string' && error.error.trim()) {
          this.errorMessage.set(error.error);
          return;
        }

        this.errorMessage.set('Could not update location link.');
      },
    });
  }

  startFinalDecisionEdit(order: OrderListItem): void {
    this.editingFinalDecisionOrderId.set(order.id);
    this.finalDecisionEditValue.set(order.finalDecision ?? '');
  }

  cancelFinalDecisionEdit(): void {
    this.editingFinalDecisionOrderId.set(null);
    this.finalDecisionEditValue.set('');
  }

  saveFinalDecision(order: OrderListItem): void {
    const store = this.authService.selectedStore();

    if (!store) {
      return;
    }

    const value = this.finalDecisionEditValue().trim();

    const finalDecision = value === '' ? null : value;

    if (finalDecision === order.finalDecision) {
      this.cancelFinalDecisionEdit();
      return;
    }

    this.savingFinalDecisionOrderId.set(order.id);
    this.errorMessage.set('');
    this.statusUpdateMessage.set('');

    this.ordersService.updateFinalDecision(store.id, order.id, finalDecision).subscribe({
      next: () => {
        this.savingFinalDecisionOrderId.set(null);
        this.editingFinalDecisionOrderId.set(null);
        this.finalDecisionEditValue.set('');

        this.statusUpdateMessage.set(
          `${order.displayOrderId} final decision updated successfully.`,
        );

        this.loadOrders();
      },

      error: (error: HttpErrorResponse) => {
        this.savingFinalDecisionOrderId.set(null);

        if (error.status === 403) {
          this.errorMessage.set('You are not allowed to update the final decision.');
          return;
        }

        if (typeof error.error === 'string' && error.error.trim()) {
          this.errorMessage.set(error.error);
          return;
        }

        this.errorMessage.set('Could not update final decision.');
      },
    });
  }

  searchOrders(): void {
    this.page.set(1);

    this.loadOrders();
  }

  clearSearch(): void {
    if (!this.search()) {
      return;
    }

    this.search.set('');
    this.page.set(1);

    this.loadOrders();
  }

  openOrder(orderId: number): void {
    this.router.navigate(['/workspace/orders', orderId]);
  }

  logout(): void {
    this.authService.logout().subscribe({
      next: () => {
        this.router.navigate(['/login']);
      },
    });
  }

  startInvoiceEdit(order: OrderListItem): void {
    this.editingInvoiceOrderId.set(order.id);
    this.invoiceEditValue.set(order.invoiceStatus);
  }

  cancelInvoiceEdit(): void {
    this.editingInvoiceOrderId.set(null);
    this.invoiceEditValue.set(null);
  }

  saveInvoiceStatus(order: OrderListItem): void {
    const store = this.authService.selectedStore();
    const newStatus = this.invoiceEditValue();

    if (!store || newStatus === null) {
      return;
    }

    if (newStatus === order.invoiceStatus) {
      this.cancelInvoiceEdit();
      return;
    }

    this.savingInvoiceOrderId.set(order.id);
    this.errorMessage.set('');
    this.statusUpdateMessage.set('');

    this.ordersService.updateInvoiceStatus(store.id, order.id, newStatus).subscribe({
      next: () => {
        this.savingInvoiceOrderId.set(null);
        this.editingInvoiceOrderId.set(null);
        this.invoiceEditValue.set(null);

        this.statusUpdateMessage.set(
          `${order.displayOrderId} invoice status updated successfully.`,
        );

        this.loadOrders();
      },

      error: (error: HttpErrorResponse) => {
        this.savingInvoiceOrderId.set(null);

        if (error.status === 403) {
          this.errorMessage.set('You are not allowed to update invoice status.');
          return;
        }

        if (typeof error.error === 'string' && error.error.trim()) {
          this.errorMessage.set(error.error);
          return;
        }

        this.errorMessage.set('Could not update invoice status.');
      },
    });
  }

  getStatusName(status: number): string {
    const statuses: Record<number, string> = {
      0: 'Confirmed',
      1: 'Shipped',
      2: 'Delivered',
      3: 'No Response',
      4: 'Return',
      5: 'Return In Process',
      6: 'Cancelled',
      7: 'Repeated Order',
      8: 'New',
    };

    return statuses[status] ?? 'Unknown';
  }

  getStatusClasses(status: number): string {
    const classes: Record<number, string> = {
      0: 'bg-purple-100 text-purple-700',
      1: 'bg-blue-100 text-blue-700',
      2: 'bg-green-100 text-green-700',
      3: 'bg-orange-100 text-orange-700',
      4: 'bg-teal-100 text-teal-700',
      5: 'bg-yellow-100 text-yellow-700',
      6: 'bg-gray-200 text-gray-700',
      7: 'bg-pink-100 text-pink-700',
      8: 'bg-cyan-100 text-cyan-700',
    };

    return classes[status] ?? 'bg-gray-100 text-gray-700';
  }

  getOrderRowClasses(order: OrderListItem): string {
  if (order.needsAttention) {
    return 'bg-red-100 hover:bg-red-200';
  }

  if (order.needToShip) {
    return 'bg-indigo-50 hover:bg-indigo-100';
  }

  return this.getStatusRowClasses(order.orderStatus);
}

  getSourceName(source: number): string {
    const sources: Record<number, string> = {
      0: 'Shopify',
      1: 'CSV Import',
      2: 'WhatsApp',
      3: 'Other',
    };

    return sources[source] ?? 'Unknown';
  }

  getInvoiceName(status: number): string {
    return status === 0 ? 'Paid' : 'Unpaid';
  }

  startShoaibNoteEdit(order: OrderListItem): void {
    this.editingShoaibNoteOrderId.set(order.id);
    this.shoaibNoteEditValue.set(order.shoaibNote ?? '');
    this.statusUpdateMessage.set('');
  }

  cancelShoaibNoteEdit(): void {
    this.editingShoaibNoteOrderId.set(null);
    this.shoaibNoteEditValue.set('');
  }

  saveShoaibNote(order: OrderListItem): void {
    const store = this.authService.selectedStore();

    if (!store) {
      return;
    }

    const value = this.shoaibNoteEditValue().trim();
    const text = value === '' ? null : value;

    if (text === order.shoaibNote) {
      this.cancelShoaibNoteEdit();
      return;
    }

    this.savingShoaibNoteOrderId.set(order.id);
    this.errorMessage.set('');
    this.statusUpdateMessage.set('');

    this.ordersService.updateShoaibNote(store.id, order.id, text).subscribe({
      next: () => {
        this.savingShoaibNoteOrderId.set(null);
        this.editingShoaibNoteOrderId.set(null);
        this.shoaibNoteEditValue.set('');

        this.statusUpdateMessage.set(
          `${order.displayOrderId} Customer Support note updated successfully.`,
        );

        this.loadOrders();
      },

      error: (error: HttpErrorResponse) => {
        this.savingShoaibNoteOrderId.set(null);

        if (error.status === 403) {
          this.errorMessage.set('You are not allowed to update Customer Support note.');
          return;
        }

        if (typeof error.error === 'string' && error.error.trim()) {
          this.errorMessage.set(error.error);
          return;
        }

        this.errorMessage.set('Could not update the Customer Support note.');
      },
    });
  }

  startTrenvoNoteEdit(order: OrderListItem): void {
    this.editingTrenvoNoteOrderId.set(order.id);
    this.trenvoNoteEditValue.set(order.trenvoNote ?? '');
    this.statusUpdateMessage.set('');
  }

  cancelTrenvoNoteEdit(): void {
    this.editingTrenvoNoteOrderId.set(null);
    this.trenvoNoteEditValue.set('');
  }

  saveTrenvoNote(order: OrderListItem): void {
    const store = this.authService.selectedStore();

    if (!store) {
      return;
    }

    const value = this.trenvoNoteEditValue().trim();
    const text = value === '' ? null : value;

    if (text === order.trenvoNote) {
      this.cancelTrenvoNoteEdit();
      return;
    }

    this.savingTrenvoNoteOrderId.set(order.id);
    this.errorMessage.set('');
    this.statusUpdateMessage.set('');

    this.ordersService.updateTrenvoNote(store.id, order.id, text).subscribe({
      next: () => {
        this.savingTrenvoNoteOrderId.set(null);
        this.editingTrenvoNoteOrderId.set(null);
        this.trenvoNoteEditValue.set('');

        this.statusUpdateMessage.set(
          `${order.displayOrderId} Warehouse staff Note updated successfully.`,
        );

        this.loadOrders();
      },

      error: (error: HttpErrorResponse) => {
        this.savingTrenvoNoteOrderId.set(null);

        if (error.status === 403) {
          this.errorMessage.set('You are not allowed to update Warehouse staff Note.');
          return;
        }

        if (typeof error.error === 'string' && error.error.trim()) {
          this.errorMessage.set(error.error);
          return;
        }

        this.errorMessage.set('Could not update Warehouse staff Note.');
      },
    });
  }

  openMobileMenu(): void {
    this.mobileMenuOpen.set(true);
  }

  closeMobileMenu(): void {
    this.mobileMenuOpen.set(false);
  }

  openCreateOrder(): void {
    this.mobileMenuOpen.set(false);
    this.router.navigate(['/workspace/create-order']);
  }

  openImportOrders(): void {
    this.mobileMenuOpen.set(false);
    this.router.navigate(['/workspace/import-orders']);
  }

  getStatusRowClasses(status: number): string {
    const classes: Record<number, string> = {
      0: 'bg-purple-50 hover:bg-purple-100', // Confirmed
      1: 'bg-blue-50 hover:bg-blue-100', // Shipped
      2: 'bg-green-50 hover:bg-green-100', // Delivered
      3: 'bg-orange-50 hover:bg-orange-100', // No Response
      4: 'bg-teal-50 hover:bg-teal-100', // Return
      5: 'bg-yellow-50 hover:bg-yellow-100', // Return In Process
      6: 'bg-gray-100 hover:bg-gray-200', // Cancelled
      7: 'bg-pink-50 hover:bg-pink-100', // Repeated Order
      8: 'bg-cyan-50 hover:bg-cyan-100', // New
    };

    return classes[status] ?? 'hover:bg-gray-50';
  }

  private loadOpenTicketCount(): void {
    const store = this.authService.selectedStore();

    if (!store) {
      return;
    }

    this.ticketService.getMyOpenCount(store.id).subscribe({
      next: (count) => {
        this.openTicketCount.set(count);
      },
      error: () => {
        this.openTicketCount.set(0);
      },
    });
  }

  openTickets(): void {
    this.router.navigate(['/workspace/tickets']);
  }

  updateNeedToShip(order: OrderListItem, needToShip: boolean): void {
  const store = this.authService.selectedStore();

  if (!store) {
    return;
  }

  this.savingNeedToShipOrderId.set(order.id);
  this.errorMessage.set('');
  this.statusUpdateMessage.set('');

  this.ordersService.updateNeedToShip(store.id, order.id, needToShip).subscribe({
    next: () => {
      this.savingNeedToShipOrderId.set(null);

      this.statusUpdateMessage.set(
        needToShip
          ? `${order.displayOrderId} marked as Need to Ship.`
          : `${order.displayOrderId} removed from Need to Ship.`,
      );

      this.loadOrders();
      this.loadSummary();
    },

    error: (error: HttpErrorResponse) => {
      this.savingNeedToShipOrderId.set(null);

      if (error.status === 403) {
        this.errorMessage.set('You are not allowed to update Need to Ship.');
        return;
      }

      if (typeof error.error === 'string' && error.error.trim()) {
        this.errorMessage.set(error.error);
        return;
      }

      this.errorMessage.set('Could not update Need to Ship.');
    },
  });
}

canMarkNeedToShip(order: OrderListItem): boolean {
  return order.orderStatus === 0 || order.orderStatus === 8;
}
}
