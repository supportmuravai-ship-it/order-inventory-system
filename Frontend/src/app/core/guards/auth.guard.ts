import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { catchError, map, of } from 'rxjs';

import { AuthService } from '../auth/auth.service';

export const authGuard: CanActivateFn = () => {

  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.currentUser()) {
    return true;
  }

  return authService.loadCurrentUser().pipe(

    map(() => true),

    catchError(() => {
      return of(
        router.createUrlTree(['/login'])
      );
    })

  );
};