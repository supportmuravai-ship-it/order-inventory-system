import { Component } from '@angular/core';

@Component({
  selector: 'app-admin',
  standalone: true,
  styleUrl: './admin.css',
  template: `
    <div class="min-h-screen bg-gray-100 p-8">

      <div class="max-w-3xl mx-auto bg-white rounded-xl shadow p-8">

        <h1 class="text-2xl font-bold text-gray-900">
          Admin Area
        </h1>

        <p class="mt-2 text-gray-600">
          Only Admin users can access this page.
        </p>

      </div>

    </div>
  `
})
export class AdminComponent {
}