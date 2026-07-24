# Vexis Engine 2 Architecture

## Non-negotiable principles

1. The editor, scripts, procedural generators, AI, macros, and plugins use one command system.
2. Every mutating editor action is previewable, transactional, auditable, and undoable.
3. AI providers never receive direct engine, filesystem, process, or network authority.
4. Content is data. Code supplies reusable behavior and validation.
5. Source assets and optimized runtime assets remain separate.
6. Vexis Studio remains fully usable without AI.
7. Local AI is the default; cloud providers and MCP clients are optional adapters.

## Core flow

```text
Human UI / VexisScript / Generator / AI / MCP
                    |
             Command Registry
                    |
           Validate and Preview
                    |
          Transaction and Undo
                    |
          Editor Domain Services
                    |
              Project Assets
```

## Planned modules

- Vexis.Foundation
- Vexis.Commands
- Vexis.Assets
- Vexis.Project
- Vexis.Graphics
- Vexis.Scene
- Vexis.World
- Vexis.Animation
- Vexis.UI
- Vexis.Script
- Vexis.AI
- Vexis.AI.Ollama
- Vexis.Mcp
- Vexis.Editor
- Vaelor.Shared
- Vaelor.Client
- Vaelor.Server

## AI security boundary

An assistant may:

- Query registered metadata
- Search indexed project knowledge
- Produce typed command requests
- Explain plans and validation failures

An assistant may not:

- Read arbitrary files
- Execute shell commands
- Modify source code directly
- Connect to arbitrary network endpoints
- Commit destructive operations without explicit approval

## Why command-first

The command system is more important than the model. A small local model can reliably select a constrained tool, while even a powerful model is unsafe and inconsistent when asked to manipulate raw engine state.
