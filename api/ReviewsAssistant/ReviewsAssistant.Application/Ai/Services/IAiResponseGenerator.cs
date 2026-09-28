using ReviewsAssistant.Application.Ai.Contracts;

namespace ReviewsAssistant.Application.Ai.Services;

public interface IAiResponseGenerator
{
    Task<AiResponseGenerationResult> GenerateAsync(AiResponseGenerationRequest request, CancellationToken cancellationToken);
}
