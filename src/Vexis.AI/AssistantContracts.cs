using System.Text.Json.Nodes;
using Vexis.Commands;

namespace Vexis.AI;

public sealed record AssistantContext(
    string UserRequest,
    string Workspace,
    string? SelectionSummary,
    IReadOnlyDictionary<string, string>? RetrievedKnowledge = null);

public sealed record AssistantToolCall(string Name, JsonObject Arguments);

public sealed record AssistantPlan(
    string Explanation,
    IReadOnlyList<AssistantToolCall> ToolCalls,
    IReadOnlyList<string> Assumptions);

public interface IVexisAssistantProvider
{
    string Name { get; }
    ValueTask<AssistantPlan> PlanAsync(
        AssistantContext context,
        IReadOnlyCollection<CommandDescriptor> availableTools,
        CancellationToken cancellationToken = default);
}
