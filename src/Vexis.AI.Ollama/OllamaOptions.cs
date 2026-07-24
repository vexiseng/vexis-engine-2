namespace Vexis.AI.Ollama;

public sealed class OllamaOptions
{
    public Uri Endpoint { get; init; } = new("http://localhost:11434");
    public string Model { get; init; } = "qwen3:8b";
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(5);
}
