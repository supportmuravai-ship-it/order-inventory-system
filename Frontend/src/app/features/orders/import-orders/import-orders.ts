import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';

import { AuthService } from '../../../core/auth/auth.service';
import { CsvImportResult } from '../models/order.model';
import { OrdersService } from '../services/orders.service';

import { Component, inject, signal, OnInit } from '@angular/core';
import { TicketService } from '../../Tickets/ticket.service';

@Component({
  selector: 'app-import-orders',
  standalone: true,
  imports: [CommonModule],
  styleUrl: './import-orders.css',
  templateUrl: './import-orders.html',
})
export class ImportOrdersComponent implements OnInit{
  readonly authService = inject(AuthService);

  private readonly ordersService = inject(OrdersService);
  private readonly router = inject(Router);

  readonly selectedFile = signal<File | null>(null);

  readonly loading = signal(false);

  readonly errorMessage = signal('');

  readonly result = signal<CsvImportResult | null>(null);

private readonly ticketService = inject(TicketService);

readonly mobileMenuOpen = signal(false);
readonly openTicketCount = signal(0);

ngOnInit(): void {
  this.loadOpenTicketCount();
}

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;

    const file = input.files && input.files.length > 0 ? input.files[0] : null;

    this.selectedFile.set(file);

    this.errorMessage.set('');
    this.result.set(null);
  }

  importCsv(): void {
    const store = this.authService.selectedStore();

    const file = this.selectedFile();

    if (!store) {
      this.router.navigate(['/stores']);
      return;
    }

    if (!file) {
      this.errorMessage.set('Please select a CSV file.');

      return;
    }

    if (!file.name.toLowerCase().endsWith('.csv')) {
      this.errorMessage.set('Please select a valid CSV file.');

      return;
    }

    this.loading.set(true);

    this.errorMessage.set('');

    this.result.set(null);

    this.ordersService.importCsv(store.id, file).subscribe({
      next: (result) => {
        this.loading.set(false);

        this.result.set(result);
      },

      error: (error: HttpErrorResponse) => {
        this.loading.set(false);

        if (error.status === 403) {
          this.errorMessage.set('You are not allowed to import orders.');

          return;
        }

        if (typeof error.error === 'string' && error.error.trim()) {
          this.errorMessage.set(error.error);

          return;
        }

        this.errorMessage.set('Could not import CSV file.');
      },
    });
  }

  goBack(): void {
    this.router.navigate(['/workspace']);
  }

openMobileMenu(): void {
  this.mobileMenuOpen.set(true);
}

closeMobileMenu(): void {
  this.mobileMenuOpen.set(false);
}

openAdminDashboard(): void {
  this.mobileMenuOpen.set(false);
  this.router.navigate(['/admin']);
}

openTickets(): void {
  this.mobileMenuOpen.set(false);
  this.router.navigate(['/workspace/tickets']);
}

openCreateOrder(): void {
  this.mobileMenuOpen.set(false);
  this.router.navigate(['/workspace/create-order']);
}

openImportOrders(): void {
  this.mobileMenuOpen.set(false);
  this.router.navigate(['/workspace/import-orders']);
}

private loadOpenTicketCount(): void {
  const store = this.authService.selectedStore();

  if (!store) {
    this.openTicketCount.set(0);
    return;
  }

  this.ticketService.getMyOpenCount(store.id).subscribe({
    next: (count) => this.openTicketCount.set(count),
    error: () => this.openTicketCount.set(0),
  });
}

get isAdmin(): boolean {
  return this.authService.currentUser()?.roles?.includes('Admin') ?? false;
}
}
