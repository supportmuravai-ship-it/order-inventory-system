import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { AuthService } from '../../../core/auth/auth.service';
import { OrderDetails } from '../models/order.model';
import { OrdersService } from '../services/orders.service';

@Component({
  selector: 'app-order-details',
  standalone: true,
  imports: [CommonModule],
  styleUrl: './order-details.css',
  templateUrl: './order-details.html',
})
export class OrderDetailsComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly ordersService = inject(OrdersService);

  readonly authService = inject(AuthService);

  readonly order = signal<OrderDetails | null>(null);

  readonly loading = signal(true);
  readonly savingStatus = signal(false);

  readonly errorMessage = signal('');
  readonly successMessage = signal('');

  readonly selectedStatus = signal<number | null>(null);

  readonly savingTracking = signal(false);
  readonly trackingValue = signal('');

  readonly savingInvoice = signal(false);
  readonly invoiceValue = signal<number | null>(null);

  readonly savingLocation = signal(false);
  readonly locationValue = signal('');

  readonly savingFinalDecision = signal(false);
  readonly finalDecisionValue = signal('');

  readonly savingShoaibNote = signal(false);
  readonly shoaibNoteValue = signal('');

  readonly savingTrenvoNote = signal(false);
  readonly trenvoNoteValue = signal('');

  ngOnInit(): void {
    this.loadOrder();
  }

  private loadOrder(): void {
    const store = this.authService.selectedStore();

    if (!store) {
      this.router.navigate(['/stores']);
      return;
    }

    const orderId = Number(this.route.snapshot.paramMap.get('orderId'));

    if (!Number.isInteger(orderId) || orderId <= 0) {
      this.errorMessage.set('Invalid order.');
      this.loading.set(false);
      return;
    }

    this.loading.set(true);
    this.errorMessage.set('');

    this.ordersService.getOrder(store.id, orderId).subscribe({
      next: (order) => {
        this.order.set(order);

        this.selectedStatus.set(order.orderStatus);
        this.trackingValue.set(order.trackingNumber ?? '');
        this.invoiceValue.set(order.invoiceStatus);
        this.locationValue.set(order.locationLink ?? '');
        this.finalDecisionValue.set(order.finalDecision ?? '');

        this.shoaibNoteValue.set(order.shoaibNote ?? '');
        this.trenvoNoteValue.set(order.trenvoNote ?? '');

        this.loading.set(false);
      },

      error: (error: HttpErrorResponse) => {
        this.loading.set(false);

        if (error.status === 404) {
          this.errorMessage.set('Order not found.');
          return;
        }

        if (error.status === 403) {
          this.errorMessage.set('You do not have access to this order.');
          return;
        }

        this.errorMessage.set('Could not load the order.');
      },
    });
  }

  saveStatus(): void {
    const store = this.authService.selectedStore();
    const order = this.order();
    const status = this.selectedStatus();

    if (!store || !order || status === null) {
      return;
    }

    this.savingStatus.set(true);
    this.errorMessage.set('');
    this.successMessage.set('');

    this.ordersService.updateOrderStatus(store.id, order.id, status).subscribe({
      next: () => {
        this.loadOrder();

        this.successMessage.set('Order status updated successfully.');

        this.savingStatus.set(false);
      },

      error: (error: HttpErrorResponse) => {
        this.savingStatus.set(false);

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

  backToOrders(): void {
    this.router.navigate(['/workspace']);
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

  saveTracking(): void {
    const store = this.authService.selectedStore();
    const order = this.order();

    if (!store || !order) {
      return;
    }

    const value = this.trackingValue().trim();

    const trackingNumber = value === '' ? null : value;

    if (trackingNumber === order.trackingNumber) {
      return;
    }

    this.savingTracking.set(true);
    this.errorMessage.set('');
    this.successMessage.set('');

    this.ordersService.updateTrackingNumber(store.id, order.id, trackingNumber).subscribe({
      next: () => {
        this.savingTracking.set(false);

        this.successMessage.set('Tracking number updated successfully.');

        this.loadOrder();
      },

      error: (error: HttpErrorResponse) => {
        this.savingTracking.set(false);

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

  saveInvoiceStatus(): void {
    const store = this.authService.selectedStore();
    const order = this.order();
    const invoiceStatus = this.invoiceValue();

    if (!store || !order || invoiceStatus === null) {
      return;
    }

    if (invoiceStatus === order.invoiceStatus) {
      return;
    }

    this.savingInvoice.set(true);
    this.errorMessage.set('');
    this.successMessage.set('');

    this.ordersService.updateInvoiceStatus(store.id, order.id, invoiceStatus).subscribe({
      next: () => {
        this.savingInvoice.set(false);

        this.successMessage.set('Invoice status updated successfully.');

        this.loadOrder();
      },

      error: (error: HttpErrorResponse) => {
        this.savingInvoice.set(false);

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

  saveLocation(): void {
    const store = this.authService.selectedStore();
    const order = this.order();

    if (!store || !order) {
      return;
    }

    const value = this.locationValue().trim();

    const locationLink = value === '' ? null : value;

    if (locationLink === order.locationLink) {
      return;
    }

    this.savingLocation.set(true);
    this.errorMessage.set('');
    this.successMessage.set('');

    this.ordersService.updateLocationLink(store.id, order.id, locationLink).subscribe({
      next: () => {
        this.savingLocation.set(false);

        this.successMessage.set('Location link updated successfully.');

        this.loadOrder();
      },

      error: (error: HttpErrorResponse) => {
        this.savingLocation.set(false);

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

  saveFinalDecision(): void {
    const store = this.authService.selectedStore();
    const order = this.order();

    if (!store || !order) {
      return;
    }

    const value = this.finalDecisionValue().trim();

    const finalDecision = value === '' ? null : value;

    if (finalDecision === order.finalDecision) {
      return;
    }

    this.savingFinalDecision.set(true);
    this.errorMessage.set('');
    this.successMessage.set('');

    this.ordersService.updateFinalDecision(store.id, order.id, finalDecision).subscribe({
      next: () => {
        this.savingFinalDecision.set(false);

        this.successMessage.set('Final decision updated successfully.');

        this.loadOrder();
      },

      error: (error: HttpErrorResponse) => {
        this.savingFinalDecision.set(false);

        if (error.status === 403) {
          this.errorMessage.set('Only an Admin can update the final decision.');
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

  saveShoaibNote(): void {
  const store = this.authService.selectedStore();
  const order = this.order();

  if (!store || !order) {
    return;
  }

  const value = this.shoaibNoteValue().trim();

  const text =
    value === '' ? null : value;

  if (text === order.shoaibNote) {
    return;
  }

  this.savingShoaibNote.set(true);
  this.errorMessage.set('');
  this.successMessage.set('');

  this.ordersService
    .updateShoaibNote(
      store.id,
      order.id,
      text,
    )
    .subscribe({
      next: () => {
        this.savingShoaibNote.set(false);

        this.successMessage.set(
          text === null
            ? "Customer support Note cleared successfully."
            : "Customer support Note updated successfully.",
        );

        this.loadOrder();
      },

      error: (error: HttpErrorResponse) => {
        this.savingShoaibNote.set(false);

        if (error.status === 403) {
          this.errorMessage.set(
            "You are not allowed to update Customer Support Note.",
          );
          return;
        }

        if (
          typeof error.error === 'string' &&
          error.error.trim()
        ) {
          this.errorMessage.set(error.error);
          return;
        }

        this.errorMessage.set(
          "Could not update Customer support Note.",
        );
      },
    });
}

saveTrenvoNote(): void {
  const store = this.authService.selectedStore();
  const order = this.order();

  if (!store || !order) {
    return;
  }

  const value = this.trenvoNoteValue().trim();

  const text =
    value === '' ? null : value;

  if (text === order.trenvoNote) {
    return;
  }

  this.savingTrenvoNote.set(true);
  this.errorMessage.set('');
  this.successMessage.set('');

  this.ordersService
    .updateTrenvoNote(
      store.id,
      order.id,
      text,
    )
    .subscribe({
      next: () => {
        this.savingTrenvoNote.set(false);

        this.successMessage.set(
          text === null
            ? 'Warehouse staff Note cleared successfully.'
            : 'Warehouse staff Note updated successfully.',
        );

        this.loadOrder();
      },

      error: (error: HttpErrorResponse) => {
        this.savingTrenvoNote.set(false);

        if (error.status === 403) {
          this.errorMessage.set(
            'You are not allowed to update Warehouse staff Note.',
          );
          return;
        }

        if (
          typeof error.error === 'string' &&
          error.error.trim()
        ) {
          this.errorMessage.set(error.error);
          return;
        }

        this.errorMessage.set(
          'Could not update Warehouse staff Note.',
        );
      },
    });
}
}
