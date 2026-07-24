# Local AI Plan

## Default development runtime

Ollama is the first provider because it offers a simple local endpoint, model switching, structured JSON output, and tool calling.

## Initial model

Start with `qwen3:8b`. It is small enough to evaluate broadly and capable enough for constrained planning and tool selection. The provider is not coupled to this model name.

For deeper code-oriented work, evaluate a larger Qwen coder model or Devstral after measuring the developer machine's RAM and VRAM.

## Provider strategy

The editor depends only on `IVexisAssistantProvider`.

Planned adapters:

- Ollama
- Embedded llama.cpp
- Foundry Local
- OpenAI API (optional)
- Anthropic API (optional)

## Retrieval

Do not send the whole project to the model. Build a local index containing:

- Asset metadata
- Schemas
- Vexis command documentation
- VexisScript API documentation
- Selected scene/object summaries
- Validation diagnostics
- Relevant content definitions

Retrieve a small context bundle for each request.

## Evaluation suite

Before allowing an AI feature to ship, test it against fixed prompts and score:

- Correct tool selection
- Valid schema arguments
- Number of invented tools
- Safety-policy compliance
- Minimality of the plan
- Successful undo
- Determinism across repeated runs
