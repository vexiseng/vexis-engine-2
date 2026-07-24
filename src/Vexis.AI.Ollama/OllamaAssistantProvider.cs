using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Vexis.Commands;

namespace Vexis.AI.Ollama;

public sealed class OllamaAssistantProvider(HttpClient httpClient, OllamaOptions options) : IVexisAssistantProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public string Name => $"Ollama/{options.Model}";

    public async ValueTask<AssistantPlan> PlanAsync(
        AssistantContext context,
        IReadOnlyCollection<CommandDescriptor> availableTools,
        CancellationToken cancellationToken = default)
    {
        var schema = BuildOutputSchema();
        var prompt = BuildPrompt(context, availableTools);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(options.Timeout);

        var body = new JsonObject
        {
            ["model"] = options.Model,
            ["stream"] = false,
            ["format"] = schema,
            ["messages"] = new JsonArray
            {
                new JsonObject
                {
                    ["role"] = "system",
                    ["content"] = SystemPrompt
                },
                new JsonObject
                {
                    ["role"] = "user",
                    ["content"] = prompt
                }
            },
            ["options"] = new JsonObject
            {
                ["temperature"] = 0.1
            }
        };

        using var response = await httpClient.PostAsJsonAsync("/api/chat", body, JsonOptions, timeout.Token);
        response.EnsureSuccessStatusCode();
        var document = await response.Content.ReadFromJsonAsync<JsonObject>(JsonOptions, timeout.Token)
            ?? throw new InvalidOperationException("Ollama returned an empty response.");

        var content = document["message"]?["content"]?.GetValue<string>()
            ?? throw new InvalidOperationException("Ollama response did not contain message.content.");

        var dto = JsonSerializer.Deserialize<PlanDto>(content, JsonOptions)
            ?? throw new InvalidOperationException("The local model returned an invalid assistant plan.");

        return new AssistantPlan(
            dto.Explanation,
            dto.ToolCalls.Select(call => new AssistantToolCall(call.Name, call.Arguments)).ToArray(),
            dto.Assumptions);
    }

    private static string BuildPrompt(AssistantContext context, IReadOnlyCollection<CommandDescriptor> tools)
    {
        var toolJson = JsonSerializer.Serialize(tools, JsonOptions);
        var knowledgeJson = JsonSerializer.Serialize(context.RetrievedKnowledge, JsonOptions);
        return $$"""
        USER REQUEST:
        {{context.UserRequest}}

        ACTIVE WORKSPACE:
        {{context.Workspace}}

        CURRENT SELECTION:
        {{context.SelectionSummary ?? "Nothing selected"}}

        RETRIEVED PROJECT KNOWLEDGE:
        {{knowledgeJson}}

        AVAILABLE VEXIS TOOLS:
        {{toolJson}}

        Produce the smallest safe sequence of tool calls that fulfills the request.
        Do not invent tools. Do not emit shell commands, source-code edits, or filesystem operations.
        """;
    }

    private static JsonObject BuildOutputSchema() => new()
    {
        ["type"] = "object",
        ["additionalProperties"] = false,
        ["required"] = new JsonArray("explanation", "toolCalls", "assumptions"),
        ["properties"] = new JsonObject
        {
            ["explanation"] = new JsonObject { ["type"] = "string" },
            ["toolCalls"] = new JsonObject
            {
                ["type"] = "array",
                ["items"] = new JsonObject
                {
                    ["type"] = "object",
                    ["additionalProperties"] = false,
                    ["required"] = new JsonArray("name", "arguments"),
                    ["properties"] = new JsonObject
                    {
                        ["name"] = new JsonObject { ["type"] = "string" },
                        ["arguments"] = new JsonObject { ["type"] = "object" }
                    }
                }
            },
            ["assumptions"] = new JsonObject
            {
                ["type"] = "array",
                ["items"] = new JsonObject { ["type"] = "string" }
            }
        }
    };

    private const string SystemPrompt = """
    You are the local Vexis Studio assistant for the Vaelor MMORPG project.
    You are a planner. The Vexis command system performs all real work.
    Use only tools listed in the request. Prefer reversible operations.
    Keep plans small, deterministic, and suitable for preview and undo.
    """;

    private sealed record PlanDto(string Explanation, List<ToolCallDto> ToolCalls, List<string> Assumptions);
    private sealed record ToolCallDto(string Name, JsonObject Arguments);
}
