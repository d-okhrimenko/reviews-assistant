namespace ReviewsAssistant.Infrastructure.Ai;

public sealed class OpenAiOptions
{
    public const string SectionName = "Ai:OpenAI";

    public string ApiKey { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public string InstructionsFilePath { get; init; } = "Prompts/openai-response-generation.md";
}
