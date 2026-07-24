# Vexis Engine 1 Reference Policy

Vexis Engine 1 is reference material only.

We may inspect it to understand desired capabilities, prior pain points, data requirements, and useful workflows. Engine 2 implementations must be newly designed and written against Engine 2 architecture.

## Do not carry forward

- Engine 1 application architecture
- Runtime UI implementation or visual language
- Region-edge repair as the primary terrain continuity strategy
- Per-region water ownership or opaque water painting behavior
- Manual shoreline cleanup as a normal workflow
- Water stitching passes
- Tight coupling between editor, renderer, and gameplay runtime

## Preserve only as product requirements

- Terrain sculpting, painting, stamps, and procedural operations
- Region streaming
- Attractive water rendering
- Object placement and batch workflows
- Content definitions and validation
- Script diagnostics and interaction inspection
- A live playable preview

Every retained capability must be re-evaluated for usability, determinism, undoability, automation, validation, and AI/editor-command access.
