namespace ReviewsAssistant.Application.Ai.Contracts;

public sealed record AiResponseGenerationResult(string DraftResponse, AiProviderMetadata Provider);
