import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Review } from '../../core/models/review.model';
import { ReviewsApi } from '../../core/reviews';

@Component({
  selector: 'app-reviews-page',
  imports: [RouterLink, DatePipe],
  templateUrl: './reviews-page.component.html',
  styleUrl: './reviews-page.component.css',
})
export class ReviewsPage {
  private readonly api = inject(ReviewsApi);
  private readonly filters: Record<string, string> = {};

  readonly reviews = signal<Review[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');

  constructor() {
    this.load();
  }

  filter(name: string, event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    if (value) this.filters[name] = value;
    else delete this.filters[name];
    this.load();
  }

  sortByPriority(): void {
    this.filters['sortBy'] = 'priority';
    this.load();
  }

  reviewPreview(text: string): string {
    const maxLength = 50;
    return text.length > maxLength ? `${text.slice(0, maxLength)}…` : text;
  }

  private load(): void {
    this.loading.set(true);
    this.api.list(this.filters).subscribe({
      next: (reviews) => {
        this.reviews.set(reviews);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Не вдалося завантажити відгуки.');
        this.loading.set(false);
      },
    });
  }
}
