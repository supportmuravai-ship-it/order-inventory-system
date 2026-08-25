import { CommonModule } from '@angular/common';
import {
  Component,
  inject,
  OnInit,
  signal
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import { AuthService } from '../../../core/auth/auth.service';
import { Store } from '../../../core/auth/auth.models';

@Component({
  selector: 'app-store-selection',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule
  ],
  styleUrl: './store-selection.css',
  templateUrl: './store-selection.html',
})
export class StoreSelectionComponent implements OnInit {

  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly stores = signal<Store[]>([]);
  readonly loading = signal(true);
  readonly errorMessage = signal('');

  selectedStoreId: number | null = null;

  ngOnInit(): void {
    this.loadStores();
  }

  private loadStores(): void {

    this.loading.set(true);

    this.authService.getStores()
      .subscribe({
        next: stores => {
          this.stores.set(stores);
          this.loading.set(false);
        },

        error: () => {
          this.loading.set(false);
          this.errorMessage.set(
            'Could not load your stores.'
          );
        }
      });
  }

  continue(): void {

    if (this.selectedStoreId === null) {
      return;
    }

    const store = this.stores()
      .find(x => x.id === Number(this.selectedStoreId));

    if (!store) {
      return;
    }

    this.authService.setSelectedStore(store);

    // Temporary workspace route.
    // We will create the authenticated layout next.
    this.router.navigate(['/workspace']);
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