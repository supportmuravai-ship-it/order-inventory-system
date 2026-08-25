import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';

import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-workspace',
  standalone: true,
  template: `
    <div class="min-h-screen bg-gray-100">

      <header class="bg-white border-b">

        <div class="max-w-6xl mx-auto px-6 py-4 flex justify-between items-center">

          <div>
            <div class="text-xl font-bold">
              SQUAD<span class="text-blue-600">21</span>
            </div>
          </div>

          <div class="flex items-center gap-4">

            <span class="text-sm text-gray-600">
              {{ authService.currentUser()?.name }}
            </span>

            <button
              type="button"
              (click)="changeStore()"
              class="text-sm text-blue-600 hover:underline">

              Change Store

            </button>

            <button
              type="button"
              (click)="logout()"
              class="text-sm text-red-600 hover:underline">

              Logout

            </button>

          </div>

        </div>

      </header>


      <main class="max-w-6xl mx-auto p-6">

        <div class="bg-white rounded-xl shadow p-8">

          <h1 class="text-2xl font-bold text-gray-900">
            {{ authService.selectedStore()?.name }}
          </h1>

          <p class="text-gray-600 mt-2">
            Store workspace is ready.
          </p>

          <div class="mt-6 bg-blue-50 p-4 rounded-lg">

            <p class="text-sm text-blue-800">
              Orders and dashboard will be added in the next phases.
            </p>

          </div>

        </div>

      </main>

    </div>
  `
})
export class WorkspaceComponent {

  readonly authService = inject(AuthService);

  private readonly router = inject(Router);

  changeStore(): void {

    this.authService.selectedStore.set(null);

    sessionStorage.removeItem('selectedStoreId');

    this.router.navigate(['/stores']);
  }

  logout(): void {

    this.authService.logout()
      .subscribe({
        next: () => {
          this.router.navigate(['/login']);
        }
      });

  }
}