using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ReviewsAssistant.Application.Ai.Contracts;
using ReviewsAssistant.Application.Ai.Services;
using Microsoft.Extensions.Options;

namespace ReviewsAssistant.Infrastructure.Ai;

public sealed class OpenAiResponseGenerator(
    HttpClient httpClient,
    IOptions<OpenAiOptions> options) : IAiResponseGenerator
{
    public async Task<AiResponseGenerationResult> GenerateAsync(
        AiResponseGenerationRequest request,
        CancellationToken cancellationToken)
    {
        var instructions = await File.ReadAllTextAsync(GetInstructionsPath(), cancellationToken);
        var requestBody = new
        {
            model = options.Value.Model,
            instructions,
            input = $"Автор відгуку: {request.AuthorName}\nВідгук:\n{request.ReviewText}",
            store = false,
        };

        using var response = await httpClient.PostAsJsonAsync("responses", requestBody, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);
        var draftResponse = ExtractOutputText(document.RootElement);

        if (string.IsNullOrWhiteSpace(draftResponse))
        {
            throw new InvalidOperationException("OpenAI did not return a draft response.");
        }

        return new AiResponseGenerationResult(
            draftResponse,
            new AiProviderMetadata("OpenAI", options.Value.Model));
    }

    private string GetInstructionsPath()
    {
        var path = options.Value.InstructionsFilePath;
        return Path.IsPathRooted(path) ? path : Path.Combine(AppContext.BaseDirectory, path);
    }

    private static string ExtractOutputText(JsonElement root)
    {
        if (!root.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("OpenAI returned an invalid response.");
        }

        var text = new StringBuilder();
        foreach (var item in output.EnumerateArray())
        {
            if (!item.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var part in content.EnumerateArray())
            {
                if (part.TryGetProperty("type", out var type) && type.GetString() == "output_text" &&
                    part.TryGetProperty("text", out var value))
                {
                    text.Append(value.GetString());
                }
            }
        }

        return text.ToString().Trim();
    }
}
