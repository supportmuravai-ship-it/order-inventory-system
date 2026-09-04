import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { AdminService } from '../admin.service';
import { AdminOrderKpis, AdminShopifyHealth, AdminUserListItem } from '../admin.models';

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './admin.html',
})
export class AdminComponent implements OnInit {
  private readonly adminService = inject(AdminService);
    private readonly router = inject(Router);


  readonly loading = signal(true);
  readonly errorMessage = signal('');

  readonly createUserName = signal('');
  readonly createUserEmail = signal('');
  readonly createUserPassword = signal('');
  readonly createUserRole = signal('CustomerSupport');
  readonly createUserStoreIds = signal<number[]>([]);

  readonly creatingUser = signal(false);
  readonly createUserMessage = signal('');
  readonly createUserError = signal('');

  readonly users = signal<AdminUserListItem[]>([]);
  readonly usersLoading = signal(false);
  readonly updatingUserId = signal<string | null>(null);

  readonly editingRoleUserId = signal<string | null>(null);
  readonly editingRoleValue = signal('');

  readonly editingStoresUserId = signal<string | null>(null);
  readonly editingStoreIds = signal<number[]>([]);

  readonly savingUserEditId = signal<string | null>(null);

  readonly createStoreName = signal('');
  readonly createStoreCode = signal('');
  readonly createStoreDomain = signal('');

  readonly creatingStore = signal(false);
  readonly createStoreMessage = signal('');
  readonly createStoreError = signal('');

  readonly kpis = signal<AdminOrderKpis>({
    totalOrders: 0,
    new: 0,
    confirmed: 0,
    shipped: 0,
    delivered: 0,
    cancelled: 0,
    noResponse: 0,
    return: 0,
    returnInProcess: 0,
    repeatedOrder: 0,
    returns: 0,
    needsAttention: 0,
    needToShip: 0,
  });

  readonly shopifyHealth = signal<AdminShopifyHealth[]>([]);

  ngOnInit(): void {
    this.loadDashboard();
    this.loadUsers();
  }

  backToOrders(): void {
    this.router.navigate(['/workspace']);
  }

  private loadDashboard(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.adminService.getOrderKpis().subscribe({
      next: (result) => {
        this.kpis.set(result);
        this.loadShopifyHealth();
      },
      error: () => {
        this.loading.set(false);
        this.errorMessage.set('Could not load Admin Dashboard.');
      },
    });
  }

  private loadUsers(): void {
    this.usersLoading.set(true);

    this.adminService.getUsers().subscribe({
      next: (users) => {
        this.users.set(users);
        this.usersLoading.set(false);
      },

      error: () => {
        this.usersLoading.set(false);
      },
    });
  }

  toggleUserActive(user: AdminUserListItem): void {
    this.updatingUserId.set(user.id);

    this.adminService.updateUserActiveStatus(user.id, !user.isActive).subscribe({
      next: () => {
        this.updatingUserId.set(null);
        this.loadUsers();
      },

      error: () => {
        this.updatingUserId.set(null);
      },
    });
  }

  private loadShopifyHealth(): void {
    this.adminService.getShopifyHealth().subscribe({
      next: (result) => {
        this.shopifyHealth.set(result);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.errorMessage.set('Could not load Shopify health.');
      },
    });
  }

  getConnectionClasses(status: string): string {
    return status === 'Connected' ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-700';
  }

  toggleUserStore(storeId: number): void {
    const current = this.createUserStoreIds();

    if (current.includes(storeId)) {
      this.createUserStoreIds.set(current.filter((id) => id !== storeId));

      return;
    }

    this.createUserStoreIds.set([...current, storeId]);
  }

  isUserStoreSelected(storeId: number): boolean {
    return this.createUserStoreIds().includes(storeId);
  }

  createUser(): void {
    const name = this.createUserName().trim();
    const email = this.createUserEmail().trim();
    const password = this.createUserPassword();
    const role = this.createUserRole();
    const storeIds = this.createUserStoreIds();

    this.createUserMessage.set('');
    this.createUserError.set('');

    if (!name || !email || !password) {
      this.createUserError.set('Name, email and password are required.');

      return;
    }

    if (storeIds.length === 0) {
      this.createUserError.set('Select at least one store.');

      return;
    }

    this.creatingUser.set(true);

    this.adminService
      .createUser({
        name,
        email,
        password,
        role,
        storeIds,
      })
      .subscribe({
        next: () => {
          this.creatingUser.set(false);

          this.createUserMessage.set('User created successfully.');

          this.createUserName.set('');
          this.createUserEmail.set('');
          this.createUserPassword.set('');
          this.createUserRole.set('CustomerSupport');
          this.createUserStoreIds.set([]);

          this.loadUsers();
        },

        error: (error) => {
          this.creatingUser.set(false);

          if (typeof error.error === 'string') {
            this.createUserError.set(error.error);
            return;
          }

          if (error.error?.message) {
            const errors = error.error?.errors;

            if (Array.isArray(errors) && errors.length > 0) {
              this.createUserError.set(`${error.error.message} ${errors.join(' ')}`);

              return;
            }

            this.createUserError.set(error.error.message);
            return;
          }

          this.createUserError.set('Could not create user.');
        },
      });
  }

  startRoleEdit(user: AdminUserListItem): void {
    this.editingRoleUserId.set(user.id);
    this.editingRoleValue.set(user.roles[0] ?? 'CustomerSupport');
  }

  cancelRoleEdit(): void {
    this.editingRoleUserId.set(null);
    this.editingRoleValue.set('');
  }

  saveUserRole(user: AdminUserListItem): void {
    const role = this.editingRoleValue();

    if (!role || user.roles[0] === role) {
      this.cancelRoleEdit();
      return;
    }

    this.savingUserEditId.set(user.id);

    this.adminService.updateUserRole(user.id, role).subscribe({
      next: () => {
        this.savingUserEditId.set(null);
        this.cancelRoleEdit();
        this.loadUsers();
      },

      error: () => {
        this.savingUserEditId.set(null);
      },
    });
  }

  startStoreEdit(user: AdminUserListItem): void {
    this.editingStoresUserId.set(user.id);
    this.editingStoreIds.set([...user.storeIds]);
  }

  cancelStoreEdit(): void {
    this.editingStoresUserId.set(null);
    this.editingStoreIds.set([]);
  }

  toggleEditingStore(storeId: number): void {
    const current = this.editingStoreIds();

    if (current.includes(storeId)) {
      this.editingStoreIds.set(current.filter((id) => id !== storeId));

      return;
    }

    this.editingStoreIds.set([...current, storeId]);
  }

  isEditingStoreSelected(storeId: number): boolean {
    return this.editingStoreIds().includes(storeId);
  }

  saveUserStores(user: AdminUserListItem): void {
    const storeIds = this.editingStoreIds();

    if (storeIds.length === 0) {
      return;
    }

    this.savingUserEditId.set(user.id);

    this.adminService.updateUserStores(user.id, storeIds).subscribe({
      next: () => {
        this.savingUserEditId.set(null);
        this.cancelStoreEdit();
        this.loadUsers();
      },

      error: () => {
        this.savingUserEditId.set(null);
      },
    });
  }

  createStore(): void {
    const name = this.createStoreName().trim();
    const code = this.createStoreCode().trim();
    const shopDomain = this.createStoreDomain().trim();

    this.createStoreMessage.set('');
    this.createStoreError.set('');

    if (!name || !code || !shopDomain) {
      this.createStoreError.set('Store Name, Store Code and Shop Domain are required.');

      return;
    }

    this.creatingStore.set(true);

    this.adminService
      .createStore({
        name,
        code,
        shopDomain,
      })
      .subscribe({
        next: () => {
          this.creatingStore.set(false);

          this.createStoreMessage.set('Store created successfully.');

          this.createStoreName.set('');
          this.createStoreCode.set('');
          this.createStoreDomain.set('');

          this.loadDashboard();
        },

        error: (error) => {
          this.creatingStore.set(false);

          if (typeof error.error === 'string') {
            this.createStoreError.set(error.error);
            return;
          }

          this.createStoreError.set('Could not create store.');
        },
      });
  }

  readonly syncingStoreId = signal<number | null>(null);

syncStore(storeId: number): void {

  this.syncingStoreId.set(storeId);

  this.adminService.syncStore(storeId)
    .subscribe({

      next: () => {
        this.syncingStoreId.set(null);
        this.loadDashboard();
      },

      error: () => {
        this.syncingStoreId.set(null);
        this.loadDashboard();
      }

    });
}
}
