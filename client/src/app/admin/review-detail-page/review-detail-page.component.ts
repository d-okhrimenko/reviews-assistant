import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import {
  analysisStatusLabels,
  categoryLabels,
  priorityLabels,
  sentimentLabels,
} from '../../core/localization/review-labels';
import { AnalysisStatus } from '../../core/models/review/analysis-status.model';
import { Priority } from '../../core/models/review/priority.model';
import { Review } from '../../core/models/review/review.model';
import { ReviewCategory } from '../../core/models/review/review-category.model';
import { Sentiment } from '../../core/models/review/sentiment.model';
import { ReviewsApi } from '../../core/reviews';

@Component({
  selector: 'app-review-detail-page',
  imports: [RouterLink],
  templateUrl: './review-detail-page.component.html',
  styleUrl: './review-detail-page.component.css',
})
export class ReviewDetailPage {
  private readonly api = inject(ReviewsApi);
  private readonly route = inject(ActivatedRoute);

  readonly review = signal<Review | null>(null);
  readonly error = signal('');
  readonly analysisError = signal('');
  readonly isAnalyzing = signal(false);
  readonly responseError = signal('');
  readonly isGeneratingResponse = signal(false);

  constructor() {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) return;

    this.api.get(id).subscribe({
      next: (review) => this.review.set(review),
      error: () => this.error.set('Відгук не знайдено.'),
    });
  }

  analyze(): void {
    const review = this.review();
    if (!review || this.isAnalyzing()) return;

    this.analysisError.set('');
    this.isAnalyzing.set(true);
    this.api.analyze(review.id).subscribe({
      next: (updatedReview) => {
        this.review.set(updatedReview);
        this.isAnalyzing.set(false);
      },
      error: () => {
        this.analysisError.set('Не вдалося виконати аналіз. Спробуйте ще раз.');
        this.isAnalyzing.set(false);
      },
    });
  }

  generateDraftResponse(): void {
    const review = this.review();
    if (!review || this.isGeneratingResponse()) return;

    this.responseError.set('');
    this.isGeneratingResponse.set(true);
    this.api.generateDraftResponse(review.id).subscribe({
      next: (updatedReview) => {
        this.review.set(updatedReview);
        this.isGeneratingResponse.set(false);
      },
      error: () => {
        this.responseError.set('Не вдалося згенерувати відповідь. Спробуйте ще раз.');
        this.isGeneratingResponse.set(false);
      },
    });
  }

  analysisStatusLabel(status: AnalysisStatus): string {
    return analysisStatusLabels[status];
  }

  sentimentLabel(sentiment: Sentiment | null | undefined): string {
    return sentiment === null || sentiment === undefined ? '—' : sentimentLabels[sentiment];
  }

  priorityLabel(priority: Priority | null | undefined): string {
    return priority === null || priority === undefined ? '—' : priorityLabels[priority];
  }

  categoryLabel(category: ReviewCategory | null | undefined): string {
    return category === null || category === undefined ? '—' : categoryLabels[category];
  }

  sentimentBadgeClass(sentiment: Sentiment | null | undefined): string {
    return sentiment === 2 ? 'tag tag--negative' : sentiment === 0 ? 'tag tag--positive' : 'tag';
  }

  priorityBadgeClass(priority: Priority | null | undefined): string {
    return priority === 3 ? 'tag tag--critical' : priority === null || priority === undefined ? 'tag' : 'tag tag--priority';
  }
}
