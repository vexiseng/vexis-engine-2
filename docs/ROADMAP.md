# Initial Roadmap

## Milestone 0 — Foundation (started in this repository)

- Solution/module boundaries
- Command registry
- Transaction preview
- Undo/redo
- Local assistant provider abstraction
- Ollama structured-output provider
- Console proof of concept
- Tests

## Milestone 1 — Project and asset core

- `.vexisproject` manifest
- Stable asset IDs and registry
- Import database
- Dependency graph
- Validation diagnostics
- Source/runtime asset separation
- File watching and incremental reimport

## Milestone 2 — Editor shell

- Avalonia-based studio shell initially
- Docking/workspace framework
- Content browser
- Inspector generated from schemas
- Selection service
- Command palette
- Activity/history panel
- AI assistant panel

## Milestone 3 — Custom runtime UI

- Vexis UI retained tree
- Layout engine
- Input/focus system
- Text shaping integration
- Rendering batches, clipping and nine-slice
- Theme and component assets
- Visual UI designer

## Milestone 4 — World editing

- Scene graph
- Regions and streaming
- Terrain and biome rules
- Placement and snapping
- Collision and navigation
- Procedural forest/road/building generators

## Milestone 5 — Model Studio and Blender bridge

- Preview and material assignment
- Collision/LOD/attachment editing
- Low-poly mesh operations
- Blender add-on
- One-click export and automatic reimport

## Milestone 6 — Content authoring

- Items, NPCs, objects, skills and shops
- Dialogue graph
- Quest graph
- Spawn and encounter tools
- VexisScript compiler/VM redesign

## Milestone 7 — External agents

- MCP server
- Vexis CLI
- Permission scopes
- Codex, Copilot and Claude-client interoperability

## World foundation milestone

1. Global terrain field and region chunk serialization
2. Cross-region brush transactions and edit-margin streaming
3. Terrain conflict visualizer and intentional cliff/terrace resolution
4. Semantic lakes, oceans, and river splines
5. Seamless water-body meshing with derived depth and shore masks
6. World-map scene extraction and tile-pyramid baker
7. Runtime world-map workspace in the new Vexis UI system
