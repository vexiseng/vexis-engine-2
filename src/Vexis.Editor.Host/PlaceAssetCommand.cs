using System.Globalization;
using System.Text.Json.Nodes;
using Vexis.Commands;

namespace Vexis.Editor.Host;

public sealed class PlaceAssetCommand(InMemoryWorld world) : IVexisCommand
{
    public CommandDescriptor Descriptor { get; } = new(
        "world.place_asset",
        "Place one registered asset in the active world at explicit coordinates.",
        CommandRisk.Reversible,
        new JsonObject
        {
            ["type"] = "object",
            ["additionalProperties"] = false,
            ["required"] = new JsonArray("asset", "x", "y", "z"),
            ["properties"] = new JsonObject
            {
                ["asset"] = new JsonObject { ["type"] = "string" },
                ["x"] = new JsonObject { ["type"] = "number" },
                ["y"] = new JsonObject { ["type"] = "number" },
                ["z"] = new JsonObject { ["type"] = "number" }
            }
        });

    public ValueTask<CommandExecution> ExecuteAsync(CommandContext context, JsonObject arguments)
    {
        var asset = arguments["asset"]?.GetValue<string>()
            ?? throw new ArgumentException("asset is required");
        var x = ReadSingle(arguments, "x");
        var y = ReadSingle(arguments, "y");
        var z = ReadSingle(arguments, "z");

        var created = world.Add(asset, x, y, z);
        var undoToken = new JsonObject { ["createdId"] = created.Id.ToString() };

        return ValueTask.FromResult(new CommandExecution(
            $"Placed '{asset}' at ({x}, {y}, {z}).",
            new JsonObject { ["id"] = created.Id.ToString() },
            undoToken));
    }

    private static float ReadSingle(JsonObject arguments, string propertyName)
    {
        if (arguments[propertyName] is not JsonValue value)
        {
            throw new ArgumentException($"{propertyName} is required and must be a number");
        }

        // System.Text.Json nodes retain the CLR numeric type used to create them.
        // Accept all ordinary JSON numeric representations rather than requiring
        // the node to have been created from a Single specifically.
        if (value.TryGetValue<float>(out var single) && float.IsFinite(single))
        {
            return single;
        }

        if (value.TryGetValue<double>(out var @double) &&
            double.IsFinite(@double) &&
            @double is >= -float.MaxValue and <= float.MaxValue)
        {
            return (float)@double;
        }

        // Every Decimal value fits inside the magnitude range of Single.
        if (value.TryGetValue<decimal>(out var @decimal))
        {
            return (float)@decimal;
        }

        if (value.TryGetValue<long>(out var @long))
        {
            return @long;
        }

        if (value.TryGetValue<ulong>(out var @ulong))
        {
            // Single has a much larger magnitude range than UInt64, so every
            // UInt64 value is representable in range (though large values lose precision).
            return @ulong;
        }

        // Handles values originating from parsed JSON where the backing CLR type
        // is not exposed by one of the common numeric TryGetValue overloads.
        if (float.TryParse(value.ToJsonString(), NumberStyles.Float, CultureInfo.InvariantCulture, out single) &&
            float.IsFinite(single))
        {
            return single;
        }

        throw new ArgumentException($"{propertyName} must be a finite number within the Single range");
    }

    public ValueTask UndoAsync(CommandContext context, JsonObject? undoToken)
    {
        var idText = undoToken?["createdId"]?.GetValue<string>();
        if (Guid.TryParse(idText, out var id))
        {
            world.Remove(id);
        }

        return ValueTask.CompletedTask;
    }
}
