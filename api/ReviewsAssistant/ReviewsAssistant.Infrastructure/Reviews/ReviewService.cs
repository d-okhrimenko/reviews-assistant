using Microsoft.EntityFrameworkCore;
using ReviewsAssistant.Application.Ai.Contracts;
using ReviewsAssistant.Application.Ai.Services;
using ReviewsAssistant.Application.Reviews;
using ReviewsAssistant.Application.Reviews.Contracts;
using ReviewsAssistant.Core.Reviews;
using ReviewsAssistant.Infrastructure.Data;

namespace ReviewsAssistant.Infrastructure.Reviews;

public sealed class ReviewService(
    ReviewsDbContext dbContext,
    IAiReviewAnalyzer aiReviewAnalyzer,
    IAiResponseGenerator aiResponseGenerator) : IReviewService
{
    public async Task<ReviewDto> CreateAsync(CreateReviewRequest request, CancellationToken cancellationToken)
    {
        var review = new Review
        {
            AuthorName = request.AuthorName.Trim(),
            Email = request.Email.Trim(),
            Text = request.Text.Trim(),
        };

        dbContext.Reviews.Add(review);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Map(review);
    }

    public async Task<SubmissionStatusDto?> GetSubmissionStatusAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.Reviews
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new SubmissionStatusDto(item.Id, item.AnalysisStatus, item.CreatedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ReviewDto>> GetAllAsync(ReviewQuery query, CancellationToken cancellationToken)
    {
        IQueryable<Review> reviews = dbContext.Reviews.AsNoTracking();
        if (query.Status is not null)
        {
            reviews = reviews.Where(item => item.AnalysisStatus == query.Status);
        }

        if (query.Sentiment is not null)
        {
            reviews = reviews.Where(item => item.Sentiment == query.Sentiment);
        }

        if (query.Priority is not null)
        {
            reviews = reviews.Where(item => item.Priority == query.Priority);
        }

        reviews = query.SortBy?.ToLowerInvariant() switch
        {
            "priority" => query.Descending
                ? reviews.OrderByDescending(item => item.Priority)
                : reviews.OrderBy(item => item.Priority),
            _ => query.Descending
                ? reviews.OrderByDescending(item => item.CreatedAtUtc)
                : reviews.OrderBy(item => item.CreatedAtUtc),
        };

        return await reviews
            .Select(item => Map(item))
            .ToListAsync(cancellationToken);
    }

    public async Task<ReviewDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.Reviews
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => Map(item))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<ReviewDto?> AnalyzeAsync(Guid id, CancellationToken cancellationToken)
    {
        var review = await dbContext.Reviews.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (review is null)
        {
            return null;
        }

        review.AnalysisStatus = AnalysisStatus.Processing;
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var analysis = await aiReviewAnalyzer.AnalyzeAsync(
                new AiReviewAnalysisRequest(review.Text),
                cancellationToken);

            review.Sentiment = analysis.Sentiment;
            review.Priority = analysis.Priority;
            review.Category = analysis.Category;
            review.NeedsUrgentResponse = analysis.NeedsUrgentResponse;
            review.Summary = analysis.Summary;
            review.AiAnalysisProvider = analysis.Provider.Provider;
            review.AiAnalysisModel = analysis.Provider.Model;
            review.AnalyzedAtUtc = DateTime.UtcNow;
            review.AnalysisStatus = AnalysisStatus.Completed;
            await dbContext.SaveChangesAsync(cancellationToken);

            return Map(review);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            review.AnalysisStatus = AnalysisStatus.Failed;
            await dbContext.SaveChangesAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<ReviewDto?> GenerateDraftResponseAsync(Guid id, CancellationToken cancellationToken)
    {
        var review = await dbContext.Reviews.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (review is null)
        {
            return null;
        }

        var response = await aiResponseGenerator.GenerateAsync(
            new AiResponseGenerationRequest(review.AuthorName, review.Text),
            cancellationToken);

        review.AiDraftResponse = response.DraftResponse;
        review.AiDraftResponseProvider = response.Provider.Provider;
        review.AiDraftResponseModel = response.Provider.Model;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Map(review);
    }

    private static ReviewDto Map(Review item) => new(
        item.Id,
        item.AuthorName,
        item.Email,
        item.Text,
        item.CreatedAtUtc,
        item.AnalysisStatus,
        item.Sentiment,
        item.Priority,
        item.Category,
        item.NeedsUrgentResponse,
        item.Summary,
        item.AiAnalysisProvider,
        item.AiAnalysisModel,
        item.AiDraftResponse,
        item.AiDraftResponseProvider,
        item.AiDraftResponseModel,
        item.AnalyzedAtUtc);
}
