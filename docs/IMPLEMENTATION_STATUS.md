# Vexis Engine 2 — implementation status

## Current usable vertical slice

The current package is no longer centered on Blender-style modeling. Blender is the external asset-authoring tool; Vexis owns game construction.

Implemented in the desktop editor:

- Native Avalonia desktop shell
- World, World Map, NPC, Item, Quest, and Dialogue workspaces
- Interactive terrain height editor with Raise, Lower, Smooth, and Flatten brushes
- Adjustable brush radius and strength
- Panning and zooming terrain view
- World-object placement and selection
- Scene outliner and inspector
- Content database with editable NPC, item, quest, and dialogue records
- Project save/load to `Vaelor.vexis.json`
- Isolated play-in-editor runtime snapshot
- Console, AI request panel, validation panel, and build-task panel
- Existing command, AI-provider, semantic-water, world-coordinate, and world-map foundations

## Deliberately external

Blender remains responsible for mesh modeling, UVs, rigging, animation authoring, and sculpting. Vexis will import GLB/glTF, track dependencies, reimport changed files, assign gameplay metadata, preview animations, generate or assign collision, and cook assets for the runtime.

## Next production milestones

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

The World toolbar now includes **3D Preview**. It opens a separate perspective viewport that reads the same TerrainDocument and scene-object collection as the 2D terrain editor. Terrain sculpting, procedural generation, flattening, object placement, movement, duplication, loading, and selection invalidate the preview immediately.

Current preview controls:

- Left drag: orbit
- Middle or right drag: pan
- Mouse wheel: zoom
- R: reset camera
- Wireframe and object overlays can be toggled from the preview toolbar

This milestone uses a dependency-free CPU drawing backend. It establishes the real-time editor/world synchronization and camera workflow. A later rendering milestone can replace the drawing backend with the engine GPU renderer without changing the terrain document or editing workflow.
