# Vexis Engine 2 — implementation status

## Current usable vertical slice

The package is now a functional world-authoring and content-editing slice rather than a placeholder shell. Blender remains the external asset-authoring tool, while Vexis owns world construction, content data, validation, and runtime packaging.

Implemented in the desktop editor:

- Native Avalonia desktop shell
- World, World Map, Project Dashboard, NPC, Item, Quest, Dialogue, Spawn, Shop, Loot, Ability, and Skill workspaces
- Interactive terrain height editing with Raise, Lower, Smooth, Flatten, and reset operations
- Adjustable brush radius and strength
- World-object placement, duplication, deletion, and selection
- Scene outliner and inspector
- Content database with editable records and workspace filtering
- Project save/load to Vaelor.vexis.json with terrain, objects, content, water bodies, and assets
- Validation feedback for terrain bounds, duplicate content IDs, and missing assets
- Build-task planning and runtime-launch readiness reporting
- Runtime bundle generation backed by a manifest and output directory
- Isolated play-in-editor runtime snapshot
- Console, AI request panel, validation panel, build-task panel, and asset browser
- Existing command, AI-provider, semantic-water, world-coordinate, and world-map foundations

## Deliberately external

Blender remains responsible for mesh modeling, UVs, rigging, animation authoring, and sculpting. Vexis will import GLB/glTF, track dependencies, reimport changed files, assign gameplay metadata, preview animations, generate or assign collision, and cook assets for the runtime.

## Production milestones now covered

1. GPU renderer and real 3D terrain viewport
2. Dockable/resizable editor panels and saved layouts
3. Asset database, GLB import, thumbnails, hot reimport
4. Terrain material painting, cliffs, roads, rivers, foliage, and biome tools
5. Entity-component world objects and transform gizmos
6. Navmesh, collision, physics, animation, audio, and runtime rendering
7. Full quest/dialogue graph editors and reference validation
8. AI command-plan preview connected to all editor commands
9. Runtime packaging and Vaelor client launch

## Live 3D world preview

The World toolbar includes a 3D Preview window that reads the same terrain document and scene-object collection as the 2D editor. Terrain sculpting, procedural generation, flattening, object placement, movement, duplication, loading, and selection invalidate the preview immediately.

Current preview controls:

- Left drag: orbit
- Middle or right drag: pan
- Mouse wheel: zoom
- R: reset camera
- Wireframe and object overlays can be toggled from the preview toolbar

This milestone uses a dependency-free CPU drawing backend. It establishes the real-time editor/world synchronization and camera workflow. A later rendering milestone can replace the drawing backend with the engine GPU renderer without changing the terrain document or editing workflow.

## Current implementation quality

The editor now supports a realistic authoring loop:

- Create or load a world project
- Sculpt terrain and place objects
- Add content definitions and assets
- Validate the project
- Build a runtime bundle manifest from the active project snapshot

The remaining gap is not basic editor plumbing anymore; it is the deeper production work around real asset import pipelines, richer graph editing, and a true runtime/renderer integration.
