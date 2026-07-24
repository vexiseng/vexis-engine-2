using System.Text.Json.Nodes;

namespace Vexis.Commands;

public sealed record CommandRequest(string CommandName, JsonObject Arguments);

public sealed record TransactionPreview(
    Guid Id,
    string Label,
    IReadOnlyList<CommandRequest> Requests,
    IReadOnlyList<string> Warnings,
    bool RequiresExplicitApproval);

public sealed record TransactionReceipt(
    Guid Id,
    string Label,
    DateTimeOffset CommittedAt,
    IReadOnlyList<CommandExecution> Executions);
