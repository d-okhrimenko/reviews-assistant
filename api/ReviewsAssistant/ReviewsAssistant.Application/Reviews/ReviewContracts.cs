using ReviewsAssistant.Core.Reviews;

namespace ReviewsAssistant.Application.Reviews;

public sealed record CreateReviewRequest(string AuthorName, string Email, string Text);
public sealed record ReviewDto(Guid Id, string AuthorName, string Email, string Text, DateTime CreatedAtUtc,
    AnalysisStatus AnalysisStatus, Sentiment? Sentiment, Priority? Priority, ReviewCategory? Category,
    bool? NeedsUrgentResponse, string? Summary, string? AiDraftResponse, DateTime? AnalyzedAtUtc);
public sealed record SubmissionStatusDto(Guid Id, AnalysisStatus AnalysisStatus, DateTime CreatedAtUtc);
public sealed record ReviewQuery(AnalysisStatus? Status, Sentiment? Sentiment, Priority? Priority, string? SortBy, bool Descending = true);

public interface IReviewService
{
    Task<ReviewDto> CreateAsync(CreateReviewRequest request, CancellationToken cancellationToken);
    Task<SubmissionStatusDto?> GetSubmissionStatusAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<ReviewDto>> GetAllAsync(ReviewQuery query, CancellationToken cancellationToken);
    Task<ReviewDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
