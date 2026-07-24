using Vexis.Foundation;

namespace Vexis.Commands;

public interface ICommandRegistry
{
    IReadOnlyCollection<CommandDescriptor> DescribeAll();
    Result<IVexisCommand> Resolve(string name);
}

public sealed class CommandRegistry(IEnumerable<IVexisCommand> commands) : ICommandRegistry
{
    private readonly Dictionary<string, IVexisCommand> _commands = commands.ToDictionary(
        command => command.Descriptor.Name,
        StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<CommandDescriptor> DescribeAll() =>
        _commands.Values.Select(command => command.Descriptor).OrderBy(x => x.Name).ToArray();

    public Result<IVexisCommand> Resolve(string name) =>
        _commands.TryGetValue(name, out var command)
            ? Result<IVexisCommand>.Success(command)
            : Result<IVexisCommand>.Failure($"Unknown Vexis command '{name}'.");
}
