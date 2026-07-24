# Vexis Engine 2 — Accelerated Studio Build

This package advances the Milestone 1 prototype toward the breadth of Vexis Engine 1 while keeping the redesigned Engine 2 architecture.

## Immediate changes

- Correct dark-theme integration and readable tabs/text.
- Project dashboard.
- Expanded RPG workspaces: NPCs, items, quests, dialogue, spawns, shops, loot, abilities, and skills.
- Editable object name, category, position, rotation, and scale.
- Object duplication and deletion.
- Terrain generation and full flatten operations.
- Existing terrain sculpting, world map, save/load, runtime snapshot, console, AI placeholder, validation, and task panels remain.

Run `Build.ps1`, then `Run-Studio.ps1`.

# Vexis Engine 2 — Full Studio Development Build

Vexis Engine 2 is the new editor, engine, runtime, world-building, local-AI, and game-content platform for Vaelor. Engine 1 is reference material only; all Engine 2 systems are new implementations.

## Run

```powershell
Get-ChildItem -Recurse -File | Unblock-File
.\Build.ps1
.\Run-Studio.ps1
```

The desktop editor includes a working interactive viewport, scene outliner, inspector, asset browser, console, local-AI panel, runtime controls, world-map workspace, and the first Blender-like mesh editing operations.

Read `docs/IMPLEMENTATION_STATUS.md`, `docs/BLENDER_LIKE_MODELING.md`, and the world-foundation documents for exact status and architecture.

# Vexis Engine 2 Foundation

This repository is the clean architectural base for the Vaelor/Vexis rewrite.

## Included now

- Modular .NET 10 solution
- Deterministic editor command bus
- Transaction, preview, commit, undo and redo support
- AI-provider abstraction
- Ollama local-model provider
- Schema-driven AI tool registry
- Safety policy that prevents unapproved destructive execution
- Console editor host proving the end-to-end pipeline
- Unit-test projects
- Architecture and roadmap documents

## First run

Requirements:

- .NET 10 SDK
- Ollama for local AI (optional for the non-AI command demo)

```powershell
cd Vexis-Engine-2
dotnet restore .\Vexis.Engine2.slnx
dotnet build .\Vexis.Engine2.slnx -c Release
dotnet test .\Vexis.Engine2.slnx -c Release
dotnet run --project .\src\Vexis.Editor.Host
```

For AI use, install a tool-capable local model and set the model name:

```powershell
ollama pull qwen3:8b
$env:VEXIS_AI_MODEL = "qwen3:8b"
dotnet run --project .\src\Vexis.Editor.Host -- ai "Create a ruined watchtower at the selected location"
```

The model never receives direct filesystem or engine access. It can only request registered Vexis tools. Every request is validated and converted into a previewable command transaction.

## Current status

This foundation is intentionally UI- and renderer-independent. It establishes the command and AI seams before Avalonia, custom Vexis UI, rendering, world editing, or asset importers are attached.

## New world foundation

The solution now includes `Vexis.World`, establishing global terrain coordinates, region-safe elevation storage, deterministic semantic water bodies, and the projection layer for a proper zoomable world map. See `docs/WORLD_FOUNDATION.md`.


## Milestone 1 vertical slice

The desktop editor now includes a functional terrain sculpting workspace, project persistence, world-object placement, RPG content databases, world map workspace, and isolated play mode. Blender remains the external asset-authoring application. See `docs/IMPLEMENTATION_STATUS.md` and `docs/BLENDER_INTEGRATION.md`.
