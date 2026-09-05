import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  styleUrl: './login.css',
  templateUrl: './login.html',
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly errorMessage = signal('');
  readonly showPassword = signal(false);

  readonly loginForm = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],

    password: ['', [Validators.required]],
  });

  login(): void {
    if (this.loginForm.invalid || this.loading()) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.errorMessage.set('');

    this.authService.login(this.loginForm.getRawValue()).subscribe({
      next: () => {
        this.authService.loadCurrentUser().subscribe({
          next: () => {
            this.loading.set(false);
            this.router.navigate(['/stores']);
          },

          error: () => {
            this.loading.set(false);
            this.errorMessage.set('Could not load your account.');
          },
        });
      },

      error: (error) => {
        this.loading.set(false);

        if (error.status === 401) {
          this.errorMessage.set('Invalid email or password.');
          return;
        }

        if (error.status === 0) {
          this.errorMessage.set('Cannot connect to server. Please try again later.');
          return;
        }

        this.errorMessage.set('Something went wrong. Please try again.');
      },
    });
  }

  togglePassword(): void {
    this.showPassword.update((value) => !value);
  }
}
