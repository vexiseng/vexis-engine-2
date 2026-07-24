using System.Text.Json.Nodes;
using Vexis.Commands;
using Xunit;

namespace Vexis.Commands.Tests;

public sealed class CommandBusTests
{
    [Fact]
    public async Task Commit_then_undo_restores_state()
    {
        var state = new CounterState();
        var services = new TestServiceProvider();
        var command = new IncrementCommand(state);
        var registry = new CommandRegistry([command]);
        var bus = new CommandBus(registry, services);
        var preview = bus.Preview("increment", [new CommandRequest("test.increment", new JsonObject())]);

        await bus.CommitAsync(preview, "test");
        Assert.Equal(1, state.Value);

        Assert.True(await bus.UndoAsync("test"));
        Assert.Equal(0, state.Value);
    }

    private sealed class TestServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    private sealed class CounterState
    {
        public int Value { get; set; }
    }

    private sealed class IncrementCommand(CounterState state) : IVexisCommand
    {
        public CommandDescriptor Descriptor { get; } = new(
            "test.increment", "Increment state", CommandRisk.Reversible, new JsonObject());

        public ValueTask<CommandExecution> ExecuteAsync(CommandContext context, JsonObject arguments)
        {
            state.Value++;
            return ValueTask.FromResult(new CommandExecution("Incremented"));
        }

        public ValueTask UndoAsync(CommandContext context, JsonObject? undoToken)
        {
            state.Value--;
            return ValueTask.CompletedTask;
        }
    }
}
