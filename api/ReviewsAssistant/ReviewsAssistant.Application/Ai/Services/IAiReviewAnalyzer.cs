using ReviewsAssistant.Application.Ai.Contracts;

namespace ReviewsAssistant.Application.Ai.Services;

public interface IAiReviewAnalyzer
{
    Task<AiReviewAnalysisResult> AnalyzeAsync(AiReviewAnalysisRequest request, CancellationToken cancellationToken);
}
