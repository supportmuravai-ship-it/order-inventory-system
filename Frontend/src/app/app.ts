import { Component, inject, signal } from '@angular/core';
import { HealthService } from './core/services/health.service';

@Component({
  selector: 'app-root',
  standalone: true,
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  private healthService = inject(HealthService);

  apiStatus = signal('Checking...');

  constructor() {
    this.healthService.getHealth().subscribe({
      next: response => {
        this.apiStatus.set(response.status);
      },
      error: error => {
        console.error('Health API error:', error);
        this.apiStatus.set('API unavailable');
      }
    });
  }
}