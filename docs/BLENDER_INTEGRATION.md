# Blender integration policy

Vexis Engine 2 does not attempt to replace Blender.

The intended pipeline is:

1. Author models, UVs, rigs, and animations in Blender.
2. Export GLB/glTF into the Vexis project asset directory.
3. Vexis detects the change and imports or reimports the asset.
4. Vexis creates thumbnails, runtime mesh data, material bindings, collision settings, sockets, LOD assignments, and dependency records.
5. The asset is placed and configured inside the world editor.
6. The build pipeline cooks the source asset into runtime packages.

Vexis provides lightweight imported-asset configuration only: transforms, pivot metadata, collision, sockets, LODs, materials, animation preview, and gameplay metadata.
