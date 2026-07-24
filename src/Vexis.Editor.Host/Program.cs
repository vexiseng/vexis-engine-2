using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Vexis.AI;
using Vexis.AI.Ollama;
using Vexis.Commands;
using Vexis.Editor.Host;

var services = new ServiceCollection();
services.AddSingleton<InMemoryWorld>();
services.AddTransient<IVexisCommand, PlaceAssetCommand>();
services.AddSingleton<ICommandRegistry, CommandRegistry>();
services.AddSingleton<ICommandBus, CommandBus>();

var model = Environment.GetEnvironmentVariable("VEXIS_AI_MODEL") ?? "qwen3:8b";
services.AddSingleton(new OllamaOptions { Model = model });
services.AddHttpClient<OllamaAssistantProvider>((provider, client) =>
{
    var options = provider.GetRequiredService<OllamaOptions>();
    client.BaseAddress = options.Endpoint;
});
services.AddSingleton<IVexisAssistantProvider>(provider => provider.GetRequiredService<OllamaAssistantProvider>());
services.AddSingleton<IAssistantOrchestrator, AssistantOrchestrator>();

await using var provider = services.BuildServiceProvider();
var bus = provider.GetRequiredService<ICommandBus>();
var world = provider.GetRequiredService<InMemoryWorld>();

if (args.Length >= 2 && string.Equals(args[0], "ai", StringComparison.OrdinalIgnoreCase))
{
    var request = string.Join(' ', args.Skip(1));
    var orchestrator = provider.GetRequiredService<IAssistantOrchestrator>();
    var (plan, preview) = await orchestrator.PrepareAsync(new AssistantContext(
        request,
        Workspace: "World",
        SelectionSummary: "Cursor position (128, 0, 256)"));

    Console.WriteLine(plan.Explanation);
    foreach (var assumption in plan.Assumptions)
    {
        Console.WriteLine($"Assumption: {assumption}");
    }

    Console.WriteLine($"Preview {preview.Id}: {preview.Requests.Count} command(s)");
    foreach (var warning in preview.Warnings)
    {
        Console.WriteLine($"WARNING: {warning}");
    }

    if (preview.RequiresExplicitApproval)
    {
        Console.WriteLine("Transaction requires explicit approval and was not committed by this console demo.");
        return;
    }

    var receipt = await bus.CommitAsync(preview, "local-user");
    foreach (var execution in receipt.Executions)
    {
        Console.WriteLine(execution.Summary);
    }
}
else
{
    var preview = bus.Preview("Place demo oak", [new CommandRequest(
        "world.place_asset",
        new JsonObject
        {
            ["asset"] = "vaelor:flora/oak_tree",
            ["x"] = 10,
            ["y"] = 0,
            ["z"] = 15
        })]);

    var receipt = await bus.CommitAsync(preview, "local-user");
    Console.WriteLine(receipt.Executions[0].Summary);
    Console.WriteLine($"World objects: {world.Objects.Count}");

    await bus.UndoAsync("local-user");
    Console.WriteLine($"After undo: {world.Objects.Count}");

    await bus.RedoAsync("local-user");
    Console.WriteLine($"After redo: {world.Objects.Count}");
}
