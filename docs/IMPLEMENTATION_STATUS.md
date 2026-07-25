# Vexis Engine 2 implementation status

## Working editor foundation

- Avalonia desktop editor shell and workspaces
- Editable terrain heightfield with Raise, Lower, Smooth, Flatten, Set Height, Noise, Ramp, and Erode brushes
- Adaptive tile/chunk/region grid and software 3D terrain preview
- Free/orbit camera controls in the software preview
- Scene objects and content records
- Versioned JSON project persistence
- Project-relative asset source paths
- Terrain edit undo/redo grouped by brush stroke
- Undoable procedural, smoothing, reset, and flatten terrain operations
- Semantic water preview separated from persistent project state
- Explicit confirm/cancel water workflow
- Undoable committed water bodies
- Re-solving committed water when terrain is restored by undo/redo

## Foundations that are useful but not production-complete

- Asset registration and reimport metadata. This does not yet parse or cook GLB/glTF, textures, animation, or audio.
- Validation and build planning. These are initial checks, not complete production validation.
- Runtime bundle manifest generation. This is not yet a runnable packaged game.
- CPU-rendered 3D preview. Direct3D 11 rendering has not yet been implemented.

## Next implementation order

1. Renderer abstraction and Direct3D 11 device/swap-chain foundation
2. Native Avalonia GPU viewport with software fallback
3. GPU terrain mesh, normals, lighting, fog, picking, and dirty-region updates
4. Terrain material layers and painting
5. Smart Fill leak diagnostics, Sea Level, and Brush Fill water modes
6. GPU water mesh, shoreline mask, foam, wetness, mud, and sand
7. Spline roads and rivers
8. Real GLB/glTF and texture import/cooking pipeline
9. Runtime executable and deterministic package build
