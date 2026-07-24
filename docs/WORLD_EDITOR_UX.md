# World Editor UX Direction

## Terrain tool

The user paints across the world without selecting or stitching regions. Region loading is automatic.

Brush modes:

- Raise/lower
- Set elevation
- Smooth curvature
- Flatten to sampled plane
- Ramp between two picked surfaces
- Terrace
- Erode
- Noise with preview
- Cliff and retaining-wall assist

Every brush displays its footprint, falloff, affected elevation range, and region-loading margin before application.

## Water tool

The default interaction is `Create Water Body`:

1. Click a basin or draw bounds.
2. Drag the desired surface-elevation gizmo.
3. See live predicted coverage.
4. Inspect warnings for leaks, disconnected pockets, shallow cells, or blocked outlets.
5. Confirm to create one named water entity.

The inspector explains coverage in plain language and exposes generated depth and shoreline overlays. Manual paint exists only as an advanced include/exclude override—not as the primary authoring method.

## World map workspace

A dedicated workspace previews exactly what players will see. Designers can:

- Toggle generated layers
- Edit labels and icons
- Set map visibility rules
- Preview zoom levels
- Inspect dirty tiles
- Regenerate selected areas
- Test player-marker and route behavior
- Author underground and floor maps
