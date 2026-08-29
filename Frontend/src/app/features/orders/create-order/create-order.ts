import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import { AuthService } from '../../../core/auth/auth.service';
import { CreateManualOrderItem, CreateManualOrderRequest } from '../models/order.model';
import { OrdersService } from '../services/orders.service';

@Component({
  selector: 'app-create-order',
  standalone: true,
  imports: [CommonModule, FormsModule],
  styleUrl: './create-order.css',
  templateUrl: './create-order.html',
})

export class CreateOrderComponent {
  readonly authService = inject(AuthService);

  private readonly ordersService = inject(OrdersService);
  private readonly router = inject(Router);

  readonly saving = signal(false);
  readonly errorMessage = signal('');

  displayOrderId = '';
  fullName = '';
  phone = '';
  addressLine1 = '';
  city = '';

  // 2 = WhatsApp
  orderSource = 2;

  totalAmount = 0;

  items: CreateManualOrderItem[] = [this.createEmptyItem()];

  addItem(): void {
  this.items.push(
    this.createEmptyItem(),
  );

  this.updateOrderTotal();
}

removeItem(index: number): void {
  if (this.items.length <= 1) {
    return;
  }

  this.items.splice(index, 1);

  this.updateOrderTotal();
}

  calculateItemsTotal(): number {
    return this.items.reduce(
      (total, item) => total + (Number(item.quantity) || 0) * (Number(item.unitPrice) || 0),
      0,
    );
  }

  updateOrderTotal(): void {
  this.totalAmount = this.calculateItemsTotal();
}

  saveOrder(): void {
    const store = this.authService.selectedStore();

    if (!store) {
      this.router.navigate(['/stores']);
      return;
    }

    this.errorMessage.set('');

    if (!this.displayOrderId.trim()) {
      this.errorMessage.set('Order ID is required.');

      return;
    }

    if (!this.fullName.trim()) {
      this.errorMessage.set('Customer Full Name is required.');

      return;
    }

    if (!this.phone.trim()) {
      this.errorMessage.set('Phone is required.');

      return;
    }

    if (!this.addressLine1.trim()) {
      this.errorMessage.set('Address 1 is required.');

      return;
    }

    if (!this.city.trim()) {
      this.errorMessage.set('City is required.');

      return;
    }


    if (this.items.length === 0) {
      this.errorMessage.set('At least one item is required.');

      return;
    }

    for (const item of this.items) {
      if (!item.productName.trim()) {
        this.errorMessage.set('Product Name is required for every item.');

        return;
      }

      if (!Number.isFinite(Number(item.quantity)) || Number(item.quantity) <= 0) {
        this.errorMessage.set('Quantity must be greater than 0.');

        return;
      }

      if (!Number.isFinite(Number(item.unitPrice)) || Number(item.unitPrice) < 0) {
        this.errorMessage.set('Unit Price cannot be negative.');

        return;
      }
    }

    if (!Number.isFinite(Number(this.totalAmount)) || Number(this.totalAmount) < 0) {
      this.errorMessage.set('Total Amount cannot be negative.');

      return;
    }

    const request: CreateManualOrderRequest = {
      displayOrderId: this.displayOrderId.trim(),

      fullName: this.fullName.trim(),

      phone: this.phone.trim(),

      addressLine1: this.addressLine1.trim(),

      city: this.city.trim(),

      orderDateUtc: new Date().toISOString(),

      orderSource: Number(this.orderSource),

      totalAmount: Number(this.totalAmount),

      items: this.items.map((item) => ({
        productName: item.productName.trim(),

        variantName: item.variantName?.trim() ? item.variantName.trim() : null,

        sku: item.sku?.trim() ? item.sku.trim() : null,

        quantity: Number(item.quantity),

        unitPrice: Number(item.unitPrice),
      })),
    };

    this.saving.set(true);

    this.ordersService.createManualOrder(store.id, request).subscribe({
      next: (result) => {
        this.saving.set(false);

        this.router.navigate(['/workspace/orders', result.id]);
      },

      error: (error: HttpErrorResponse) => {
        this.saving.set(false);

        if (error.status === 403) {
          this.errorMessage.set('You are not allowed to create orders.');

          return;
        }

        if (error.status === 409) {
          if (typeof error.error === 'string' && error.error.trim()) {
            this.errorMessage.set(error.error);
          } else {
            this.errorMessage.set('This Order ID already exists.');
          }

          return;
        }

        if (typeof error.error === 'string' && error.error.trim()) {
          this.errorMessage.set(error.error);

          return;
        }

        this.errorMessage.set('Could not create order.');
      },
    });
  }

  goBack(): void {
    this.router.navigate(['/workspace']);
  }

  private createEmptyItem(): CreateManualOrderItem {
    return {
      productName: '',
      variantName: null,
      sku: null,
      quantity: 1,
      unitPrice: 0,
    };
  }

  // scrolling the moue scroller sometimes increments / decrements the entered price. It stops that
  onNumberWheel(event: WheelEvent): void {
  const input = event.target as HTMLInputElement;
  input.blur();
}
}
