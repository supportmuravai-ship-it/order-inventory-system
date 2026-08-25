import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { catchError, map, of } from 'rxjs';

import { AuthService } from '../auth/auth.service';

export const storeGuard: CanActivateFn = () => {

  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.selectedStore()) {
    return true;
  }

  return authService.restoreSelectedStore().pipe(

    map(store => {

      if (store) {
        return true;
      }

      return router.createUrlTree(['/stores']);
    }),

    catchError(() => {
      return of(
        router.createUrlTree(['/login'])
      );
    })

  );
};