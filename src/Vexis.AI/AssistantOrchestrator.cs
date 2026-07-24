using Vexis.Commands;

namespace Vexis.AI;

public interface IAssistantOrchestrator
{
    ValueTask<(AssistantPlan Plan, TransactionPreview Preview)> PrepareAsync(
        AssistantContext context,
        CancellationToken cancellationToken = default);
}

public sealed class AssistantOrchestrator(
    IVexisAssistantProvider provider,
    ICommandRegistry registry,
    ICommandBus commandBus) : IAssistantOrchestrator
{
    public async ValueTask<(AssistantPlan Plan, TransactionPreview Preview)> PrepareAsync(
        AssistantContext context,
        CancellationToken cancellationToken = default)
    {
        var plan = await provider.PlanAsync(context, registry.DescribeAll(), cancellationToken);
        var requests = plan.ToolCalls
            .Select(call => new CommandRequest(call.Name, call.Arguments))
            .ToArray();

        var preview = commandBus.Preview($"AI: {context.UserRequest}", requests);
        return (plan, preview);
    }
}
