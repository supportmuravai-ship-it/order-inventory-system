import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';

import { AuthService } from '../auth/auth.service';

export const adminGuard: CanActivateFn = () => {

  const authService = inject(AuthService);
  const router = inject(Router);

  const currentUser = authService.currentUser();

  // User already loaded
  if (currentUser) {

    if (currentUser.roles.includes('Admin')) {
      return true;
    }

    return router.createUrlTree(['/unauthorized']);
  }

  // User not loaded yet, so ask backend who is logged in
  return authService.loadCurrentUser().pipe(

    map(user => {

      if (user.roles.includes('Admin')) {
        return true;
      }

      return router.createUrlTree(['/unauthorized']);
    }),

    catchError(() => {
      return of(
        router.createUrlTree(['/login'])
      );
    })

  );
};