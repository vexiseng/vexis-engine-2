# Blender-like creation inside Vexis Studio

Vexis Studio includes a native Modeling workspace rather than requiring every small game-asset edit to round-trip through Blender.

## Implemented in this development build

- Object, vertex, edge, and face selection modes
- Cube, plane, and configurable cylinder primitives
- Orbit, pan, and zoom viewport navigation
- Move, rotate, and scale mesh operations
- Face extrusion with topology regeneration
- Scene outliner, inspector, asset browser, console, AI panel, timeline placeholder, and workspace switching
- Isolated play-in-editor scene copy
- World-map workspace preview

## Planned production modeling stack

- Loop/ring selection, box/circle/lasso selection
- Inset, bevel, bridge, knife, loop cut, merge, dissolve, fill, triangulate, normals
- Modifier stack: mirror, array, subdivision, solidify, decimate, boolean, weighted normals
- Sculpt brushes with multiresolution and remeshing
- UV unwrap, seams, packing, texel density, texture painting
- Material graph and shader preview
- Armature creation, skin weights, posing, constraints, animation curves, dope sheet, NLA
- Collision, sockets, attachment points, LODs, impostors, nav blockers
- glTF-first import/export and Blender bridge for advanced workflows

The goal is not to clone all of Blender. It is to provide the modeling, sculpting, rigging, animation, and game-authoring operations needed for Vaelor in one integrated environment, while preserving Blender interoperability.
