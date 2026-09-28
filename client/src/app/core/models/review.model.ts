export interface Review {
  id: string;
  authorName: string;
  email: string;
  text: string;
  createdAtUtc: string;
  analysisStatus: string;
  sentiment?: string;
  priority?: string;
  category?: string;
  needsUrgentResponse?: boolean;
  summary?: string;
  aiDraftResponse?: string;
}
