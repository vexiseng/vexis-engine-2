using System.Text.Json.Nodes;
using Vexis.Commands;
using Xunit;

namespace Vexis.AI.Tests;

public sealed class AssistantOrchestratorTests
{
    [Fact]
    public async Task Converts_model_tool_calls_into_preview()
    {
        var command = new NoOpCommand();
        var registry = new CommandRegistry([command]);
        var bus = new CommandBus(registry, new EmptyServices());
        var orchestrator = new AssistantOrchestrator(new FakeProvider(), registry, bus);

        var (_, preview) = await orchestrator.PrepareAsync(new AssistantContext("do it", "World", null));

        Assert.Single(preview.Requests);
        Assert.Equal("test.noop", preview.Requests[0].CommandName);
    }

    private sealed class FakeProvider : IVexisAssistantProvider
    {
        public string Name => "fake";
        public ValueTask<AssistantPlan> PlanAsync(AssistantContext context, IReadOnlyCollection<CommandDescriptor> availableTools, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new AssistantPlan("test", [new AssistantToolCall("test.noop", new JsonObject())], []));
    }

    private sealed class NoOpCommand : IVexisCommand
    {
        public CommandDescriptor Descriptor { get; } = new("test.noop", "No-op", CommandRisk.ReadOnly, new JsonObject());
        public ValueTask<CommandExecution> ExecuteAsync(CommandContext context, JsonObject arguments) => ValueTask.FromResult(new CommandExecution("ok"));
        public ValueTask UndoAsync(CommandContext context, JsonObject? undoToken) => ValueTask.CompletedTask;
    }

    private sealed class EmptyServices : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
