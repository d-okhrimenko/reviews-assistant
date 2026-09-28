using ReviewsAssistant.Application.Ai.Contracts;
using ReviewsAssistant.Application.Ai.Services;

namespace ReviewsAssistant.Infrastructure.Ai;

public sealed class StubAiResponseGenerator : IAiResponseGenerator
{
    private static readonly AiProviderMetadata Provider = new("Stub", "stub-v1");

    public Task<AiResponseGenerationResult> GenerateAsync(
        AiResponseGenerationRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var draftResponse = $"Дякуємо за ваш відгук, {request.AuthorName}.";
        return Task.FromResult(new AiResponseGenerationResult(draftResponse, Provider));
    }
}
