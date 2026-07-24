namespace Vexis.Commands;

public interface ICommandBus
{
    TransactionPreview Preview(string label, IReadOnlyList<CommandRequest> requests);
    ValueTask<TransactionReceipt> CommitAsync(TransactionPreview preview, string actor, CancellationToken cancellationToken = default);
    ValueTask<bool> UndoAsync(string actor, CancellationToken cancellationToken = default);
    ValueTask<bool> RedoAsync(string actor, CancellationToken cancellationToken = default);
}

public sealed class CommandBus(ICommandRegistry registry, IServiceProvider services) : ICommandBus
{
    private readonly Stack<CommittedTransaction> _undo = new();
    private readonly Stack<CommittedTransaction> _redo = new();

    public TransactionPreview Preview(string label, IReadOnlyList<CommandRequest> requests)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentNullException.ThrowIfNull(requests);

        var warnings = new List<string>();
        var requiresApproval = false;

        foreach (var request in requests)
        {
            var resolution = registry.Resolve(request.CommandName);
            if (!resolution.IsSuccess)
            {
                warnings.Add(resolution.Error!);
                requiresApproval = true;
                continue;
            }

            if (resolution.Value!.Descriptor.Risk is CommandRisk.Destructive)
            {
                warnings.Add($"'{request.CommandName}' is destructive.");
                requiresApproval = true;
            }
        }

        return new TransactionPreview(Guid.NewGuid(), label, requests, warnings, requiresApproval);
    }

    public async ValueTask<TransactionReceipt> CommitAsync(
        TransactionPreview preview,
        string actor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(preview);
        ArgumentException.ThrowIfNullOrWhiteSpace(actor);

        if (preview.Warnings.Any(warning => warning.StartsWith("Unknown Vexis command", StringComparison.Ordinal)))
        {
            throw new InvalidOperationException("Cannot commit a transaction containing unknown commands.");
        }

        var context = new CommandContext(services, actor, DateTimeOffset.UtcNow, cancellationToken);
        var completed = new List<ExecutedCommand>();

        try
        {
            foreach (var request in preview.Requests)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var command = registry.Resolve(request.CommandName).Value!;
                var execution = await command.ExecuteAsync(context, request.Arguments);
                completed.Add(new ExecutedCommand(command, request, execution));
            }
        }
        catch
        {
            for (var index = completed.Count - 1; index >= 0; index--)
            {
                await completed[index].Command.UndoAsync(context, completed[index].Execution.UndoToken);
            }

            throw;
        }

        var transaction = new CommittedTransaction(preview, completed);
        _undo.Push(transaction);
        _redo.Clear();

        return new TransactionReceipt(
            preview.Id,
            preview.Label,
            DateTimeOffset.UtcNow,
            completed.Select(item => item.Execution).ToArray());
    }

    public async ValueTask<bool> UndoAsync(string actor, CancellationToken cancellationToken = default)
    {
        if (!_undo.TryPop(out var transaction))
        {
            return false;
        }

        var context = new CommandContext(services, actor, DateTimeOffset.UtcNow, cancellationToken);
        for (var index = transaction.Executed.Count - 1; index >= 0; index--)
        {
            var item = transaction.Executed[index];
            await item.Command.UndoAsync(context, item.Execution.UndoToken);
        }

        _redo.Push(transaction);
        return true;
    }

    public async ValueTask<bool> RedoAsync(string actor, CancellationToken cancellationToken = default)
    {
        if (!_redo.TryPop(out var transaction))
        {
            return false;
        }

        var context = new CommandContext(services, actor, DateTimeOffset.UtcNow, cancellationToken);
        var reexecuted = new List<ExecutedCommand>();

        foreach (var item in transaction.Executed)
        {
            var execution = await item.Command.ExecuteAsync(context, item.Request.Arguments);
            reexecuted.Add(new ExecutedCommand(item.Command, item.Request, execution));
        }

        _undo.Push(transaction with { Executed = reexecuted });
        return true;
    }

    private sealed record ExecutedCommand(
        IVexisCommand Command,
        CommandRequest Request,
        CommandExecution Execution);

    private sealed record CommittedTransaction(
        TransactionPreview Preview,
        IReadOnlyList<ExecutedCommand> Executed);
}
