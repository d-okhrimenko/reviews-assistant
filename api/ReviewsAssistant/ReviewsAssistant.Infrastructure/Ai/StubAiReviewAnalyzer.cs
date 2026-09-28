using ReviewsAssistant.Application.Ai.Contracts;
using ReviewsAssistant.Application.Ai.Services;
using ReviewsAssistant.Core.Reviews;

namespace ReviewsAssistant.Infrastructure.Ai;

public sealed class StubAiReviewAnalyzer : IAiReviewAnalyzer
{
    private static readonly AiProviderMetadata Provider = new("Stub", "stub-v1");

    public Task<AiReviewAnalysisResult> AnalyzeAsync(
        AiReviewAnalysisRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(new AiReviewAnalysisResult(
            Sentiment.Neutral,
            Priority.Medium,
            ReviewCategory.Other,
            false,
            "Тестовий аналіз відгуку.",
            Provider));
    }
}
