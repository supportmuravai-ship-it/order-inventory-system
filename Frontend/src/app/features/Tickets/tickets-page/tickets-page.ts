import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import { AuthService } from '../../../core/auth/auth.service';
import {
  TicketListItem,
  TicketStatus,
  TicketDetails,
  AssignableTicketUser,
} from '../ticket.models';
import { TicketService } from '../ticket.service';

@Component({
  selector: 'app-tickets-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './tickets-page.html',
})
export class TicketsPageComponent implements OnInit {
  private readonly ticketService = inject(TicketService);
  readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly mobileMenuOpen = signal(false);
readonly openTicketCount = signal(0);

  readonly TicketStatus = TicketStatus;

  readonly tickets = signal<TicketListItem[]>([]);
  readonly loading = signal(false);
  readonly totalCount = signal(0);
  readonly totalPages = signal(0);

  readonly selectedTicket = signal<TicketDetails | null>(null);
  readonly loadingDetails = signal(false);
  readonly closingTicket = signal(false);

  readonly assignableUsers = signal<AssignableTicketUser[]>([]);
  readonly createTicketOpen = signal(false);
  readonly loadingAssignableUsers = signal(false);
  readonly creatingTicket = signal(false);

  readonly successMessage = signal('');
readonly errorMessage = signal('');

  readonly reopeningTicket = signal(false);

  searchValue = '';
  assignedUserFilter = '';

  ticketAssignedToUserIds: string[] = [];
  ticketDisplayOrderId = '';
  ticketTitle = '';
  ticketMessage = '';

  statusFilter: '' | TicketStatus = '';
  page = 1;
  pageSize = 25;

  ngOnInit(): void {
  this.loadTickets();

  if (this.isAdmin) {
    this.loadUsersForFilter();
  }
  this.loadOpenTicketCount();
}

  loadTickets(): void {
    const store = this.authService.selectedStore();

    if (!store) {
      this.router.navigate(['/stores']);
      return;
    }

    this.loading.set(true);

    this.ticketService
      .getTickets(store.id, {
        status: this.statusFilter === '' ? undefined : this.statusFilter,
        assignedToUserId:
          this.isAdmin && this.assignedUserFilter ? this.assignedUserFilter : undefined,
        search: this.searchValue.trim() || undefined,
        page: this.page,
        pageSize: this.pageSize,
      })
      .subscribe({
        next: (result) => {
          this.tickets.set(result.items);
          this.totalCount.set(result.totalCount);
          this.totalPages.set(result.totalPages);
          this.loading.set(false);
        },

        error: () => {
          this.loading.set(false);
        },
      });
  }

  reopenSelectedTicket(): void {
  const ticket = this.selectedTicket();

  if (!ticket || ticket.status === TicketStatus.Open) {
    return;
  }

  this.reopeningTicket.set(true);

  this.ticketService.reopenTicket(ticket.id).subscribe({
    next: () => {
      this.reopeningTicket.set(false);
      this.selectedTicket.set(null);

      this.loadTickets();
      this.loadOpenTicketCount();

      this.errorMessage.set('');
      this.successMessage.set('Ticket reopened successfully.');
    },

    error: (error) => {
      this.reopeningTicket.set(false);

      this.successMessage.set('');
      this.errorMessage.set(
        typeof error.error === 'string'
          ? error.error
          : 'Failed to reopen ticket.',
      );
    },
  });
}

  searchTickets(): void {
    this.page = 1;
    this.loadTickets();
  }

  clearSearch(): void {
    this.searchValue = '';
    this.page = 1;
    this.loadTickets();
  }

  changeAssignedUserFilter(): void {
    this.page = 1;
    this.loadTickets();
  }

  openOrder(orderId: number): void {
    this.router.navigate(['/workspace/orders', orderId]);
  }

  changeStatusFilter(): void {
    this.page = 1;
    this.loadTickets();
  }

  get isAdmin(): boolean {
    return this.authService.currentUser()?.roles?.includes('Admin') ?? false;
  }

  previousPage(): void {
    if (this.page <= 1) {
      return;
    }

    this.page--;
    this.loadTickets();
  }

  nextPage(): void {
    if (this.page >= this.totalPages()) {
      return;
    }

    this.page++;
    this.loadTickets();
  }

  getStatusLabel(status: TicketStatus): string {
    return status === TicketStatus.Open ? 'Open' : 'Closed';
  }

  openTicket(ticketId: number): void {
    this.loadingDetails.set(true);
    this.selectedTicket.set(null);

    this.ticketService.getTicket(ticketId).subscribe({
      next: (ticket) => {
        this.selectedTicket.set(ticket);
        this.loadingDetails.set(false);
      },
      error: () => {
        this.loadingDetails.set(false);
      },
    });
  }

  openAdminDashboard(): void {
  this.mobileMenuOpen.set(false);
  this.router.navigate(['/admin']);
}

  closeTicketDetails(): void {
    this.selectedTicket.set(null);
  }

  closeSelectedTicket(): void {
    const ticket = this.selectedTicket();

    if (!ticket || ticket.status === TicketStatus.Closed) {
      return;
    }

    this.closingTicket.set(true);

    this.ticketService.closeTicket(ticket.id).subscribe({
      next: () => {
        this.closingTicket.set(false);
        this.selectedTicket.set(null);
        this.loadTickets();
        this.loadOpenTicketCount();
      },
      error: () => {
        this.closingTicket.set(false);
      },
    });
  }

  openCreateTicket(): void {
    const store = this.authService.selectedStore();

    if (!store) {
      return;
    }

    this.ticketAssignedToUserIds = [];
    this.ticketDisplayOrderId = '';
    this.ticketTitle = '';
    this.ticketMessage = '';

    this.createTicketOpen.set(true);
    this.loadingAssignableUsers.set(true);

    this.ticketService.getAssignableUsers(store.id).subscribe({
      next: (users) => {
        this.assignableUsers.set(users);
        this.loadingAssignableUsers.set(false);
        this.loadOpenTicketCount();
      },
      error: () => {
        this.assignableUsers.set([]);
        this.loadingAssignableUsers.set(false);
      },
    });
  }

  closeCreateTicket(): void {
    if (!this.creatingTicket()) {
      this.createTicketOpen.set(false);
    }
  }

  createTicket(): void {
  const store = this.authService.selectedStore();

  if (
    !store ||
    this.ticketAssignedToUserIds.length === 0 ||
    !this.ticketTitle.trim() ||
    !this.ticketMessage.trim()
  ) {
    return;
  }

  this.creatingTicket.set(true);

  this.ticketService
    .createTicketFromPage(store.id, {
      assignedToUserIds: this.ticketAssignedToUserIds,
      displayOrderId:
        this.ticketDisplayOrderId.trim() || null,
      title: this.ticketTitle.trim(),
      message: this.ticketMessage.trim(),
    })
    .subscribe({
      next: () => {
        this.creatingTicket.set(false);
        this.createTicketOpen.set(false);

        this.errorMessage.set('');
        this.successMessage.set(
          'Ticket created successfully.',
        );

        this.loadTickets();
        this.loadOpenTicketCount();
      },

      error: (error) => {
        this.creatingTicket.set(false);

        this.successMessage.set('');

        this.errorMessage.set(
          typeof error.error === 'string'
            ? error.error
            : 'Failed to create ticket.',
        );
      },
    });
}

  private loadUsersForFilter(): void {
  const store = this.authService.selectedStore();

  if (!store) {
    return;
  }

  this.ticketService.getAssignableUsers(store.id).subscribe({
    next: (users) => this.assignableUsers.set(users),
  });
}

openMobileMenu(): void {
  this.mobileMenuOpen.set(true);
}

closeMobileMenu(): void {
  this.mobileMenuOpen.set(false);
}

backToOrders(): void {
  this.mobileMenuOpen.set(false);
  this.router.navigate(['/workspace']);
}

openCreateOrder(): void {
  this.mobileMenuOpen.set(false);
  this.router.navigate(['/workspace/create-order']);
}

openImportOrders(): void {
  this.mobileMenuOpen.set(false);
  this.router.navigate(['/workspace/import-orders']);
}

openTickets(): void {
  this.mobileMenuOpen.set(false);
  this.router.navigate(['/workspace/tickets']);
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

toggleTicketAssignee(
  userId: string,
  checked: boolean,
): void {
  if (checked) {
    if (!this.ticketAssignedToUserIds.includes(userId)) {
      this.ticketAssignedToUserIds = [
        ...this.ticketAssignedToUserIds,
        userId,
      ];
    }

    return;
  }

  this.ticketAssignedToUserIds =
    this.ticketAssignedToUserIds.filter(
      id => id !== userId,
    );
}
}
