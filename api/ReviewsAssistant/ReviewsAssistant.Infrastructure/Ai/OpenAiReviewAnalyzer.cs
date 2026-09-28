using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ReviewsAssistant.Application.Ai.Contracts;
using ReviewsAssistant.Application.Ai.Services;
using ReviewsAssistant.Core.Reviews;

namespace ReviewsAssistant.Infrastructure.Ai;

public sealed class OpenAiReviewAnalyzer(
    HttpClient httpClient,
    IOptions<OpenAiOptions> options) : IAiReviewAnalyzer
{
    public async Task<AiReviewAnalysisResult> AnalyzeAsync(
        AiReviewAnalysisRequest request,
        CancellationToken cancellationToken)
    {
        var instructions = await File.ReadAllTextAsync(GetInstructionsPath(), cancellationToken);
        var requestBody = new
        {
            model = options.Value.Model,
            instructions,
            input = request.ReviewText,
            store = false,
            text = new
            {
                format = new
                {
                    type = "json_schema",
                    name = "review_analysis",
                    strict = true,
                    schema = new
                    {
                        type = "object",
                        additionalProperties = false,
                        properties = new
                        {
                            sentiment = new { type = "string", @enum = Enum.GetNames<Sentiment>() },
                            priority = new { type = "string", @enum = Enum.GetNames<Priority>() },
                            category = new { type = "string", @enum = Enum.GetNames<ReviewCategory>() },
                            needsUrgentResponse = new { type = "boolean" },
                            summary = new { type = "string" },
                        },
                        required = new[] { "sentiment", "priority", "category", "needsUrgentResponse", "summary" },
                    },
                },
            },
        };

        using var response = await httpClient.PostAsJsonAsync("responses", requestBody, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);
        return ParseResult(ExtractOutputText(document.RootElement));
    }

    private string GetInstructionsPath()
    {
        var path = options.Value.ReviewAnalysisInstructionsFilePath;
        return Path.IsPathRooted(path) ? path : Path.Combine(AppContext.BaseDirectory, path);
    }

    private AiReviewAnalysisResult ParseResult(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            throw new InvalidOperationException("OpenAI did not return an analysis result.");
        }

        using var document = JsonDocument.Parse(output);
        var root = document.RootElement;
        var summary = GetRequiredString(root, "summary");
        if (summary.Length > 1000)
        {
            throw new InvalidOperationException("OpenAI returned an analysis summary that is too long.");
        }

        return new AiReviewAnalysisResult(
            GetRequiredEnum<Sentiment>(root, "sentiment"),
            GetRequiredEnum<Priority>(root, "priority"),
            GetRequiredEnum<ReviewCategory>(root, "category"),
            GetRequiredBoolean(root, "needsUrgentResponse"),
            summary,
            new AiProviderMetadata("OpenAI", options.Value.Model));
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

    private static string GetRequiredString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(property.GetString()))
        {
            throw new InvalidOperationException($"OpenAI returned an invalid '{propertyName}' value.");
        }

        return property.GetString()!.Trim();
    }

    private static bool GetRequiredBoolean(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property) || property.ValueKind is not JsonValueKind.True and not JsonValueKind.False)
        {
            throw new InvalidOperationException($"OpenAI returned an invalid '{propertyName}' value.");
        }

        return property.GetBoolean();
    }

    private static TEnum GetRequiredEnum<TEnum>(JsonElement root, string propertyName)
        where TEnum : struct, Enum
    {
        var value = GetRequiredString(root, propertyName);
        if (!Enum.TryParse<TEnum>(value, ignoreCase: false, out var result))
        {
            throw new InvalidOperationException($"OpenAI returned an invalid '{propertyName}' value.");
        }

        return result;
    }
}
