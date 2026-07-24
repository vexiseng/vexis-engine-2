using System.Text.Json.Nodes;

namespace Vexis.Commands;

public enum CommandRisk
{
    ReadOnly,
    Reversible,
    Destructive
}

public sealed record CommandDescriptor(
    string Name,
    string Description,
    CommandRisk Risk,
    JsonObject InputSchema);

public sealed record CommandContext(
    IServiceProvider Services,
    string Actor,
    DateTimeOffset Timestamp,
    CancellationToken CancellationToken);

public sealed record CommandExecution(
    string Summary,
    JsonObject? Output = null,
    JsonObject? UndoToken = null);

public interface IVexisCommand
{
    CommandDescriptor Descriptor { get; }
    ValueTask<CommandExecution> ExecuteAsync(CommandContext context, JsonObject arguments);
    ValueTask UndoAsync(CommandContext context, JsonObject? undoToken);
}
