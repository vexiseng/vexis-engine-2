# Vexis Engine 2 World Foundation

## Non-negotiable rule

A map region is a streaming and persistence partition. It is not an isolated miniature world.

Terrain vertices, water bodies, roads, navigation, environment volumes, and world-map imagery are authored in global world coordinates. Region files store chunks of that data, but neighboring chunks never own duplicate border truth.

## Terrain

Engine 1 attempted to repair independently owned region edges. Engine 2 removes the underlying cause:

- A 64x64-cell region exposes 65x65 vertices.
- Border vertices use canonical global coordinates.
- Region `(0,0)` local vertex `(64,12)` and region `(1,0)` local vertex `(0,12)` are the same key.
- Normal editing therefore cannot produce a terrain seam.
- External imports use an explicit conflict policy.
- Large elevation differences are handled using a transition band, terrace, retaining wall, cliff, or deliberate discontinuity—not blind edge averaging.

Planned editor behavior:

- Terrain brushes cross region boundaries normally.
- The editor streams neighboring edit margins automatically.
- A border diagnostic overlay shows slope, normal, material, water, navigation, and geometry continuity.
- Imported heightmaps display conflicts before applying them.
- Designers choose `Blend`, `Cliff`, `Terrace`, `Retaining Wall`, or `Replace` for substantial border conflicts.

## Water

Water is a semantic world entity, not an unexplained painted material.

Each water body owns:

- A visible name and stable ID
- One or more source/seed points
- Surface elevation or river profile
- Explicit solve bounds/watershed
- Optional barriers, outlets, flow direction, and material profile
- Generated coverage, depth, shore mask, foam mask, and underwater volume

The initial deterministic lake solver floods globally connected terrain below the chosen surface. This means:

- The editor can explain exactly why a cell is wet.
- Region borders are irrelevant to coverage.
- Water surfaces are generated as one logical body and chunked only for rendering.
- Shoreline and depth are derived, not hand-painted.
- Holes appear only when terrain or a deliberate barrier prevents connectivity.

Planned river support will use authored centerline splines and longitudinal elevation profiles, then solve banks against terrain. Oceans use bounded/coastline-aware bodies.

## Proper world map

The world map is a first-class engine product, not a screenshot.

The same world database will generate a layered, zoomable map with:

- Terrain and biome coloration
- Water, shorelines, bridges, and waterfalls
- Roads, paths, walls, and buildings
- Settlements, regions, dungeons, and interiors
- Labels and icon categories
- Player marker, facing, destination, and route
- Quest markers and area highlights
- Teleports and transportation links
- Coordinate lookup and click-to-waypoint
- Fog of war and discovered locations
- Multiple floors and underground layers

The map baker will generate a tile pyramid similar to online slippy maps. Dirty-region tracking will rebuild only affected tiles. World-to-map projection is shared by baking and runtime interaction so icons and clicks cannot drift from the rendered map.

## Runtime UI

No Engine 1 runtime UI code or visual system is being retained. Engine 2 will receive a new data-driven UI system and editor designed around Vaelor's needs. The world-map UI will use that new system.
