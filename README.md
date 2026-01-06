# Platform2D (WIP) <!-- Rename to PolyShape2D -->

A Godot 4.x plugin that adds the `Platform2D` node,
which is an extended `Polygon2D` with extra features like
curved edges,
automatic collision shape generation, and
automatic edge sprite placement.

> ⚠ This plugin is still a work in progress.
> It might contain bugs, and
> some of its features might not be fully implemented yet.

## Features

- **Curved Edges.** Draw your platforms using Path2D curves and let the plugin automatically generate the polygon mesh.
- **Automatic Collision Generation.** Collision shapes are automatically generated and kept updated based on the polygon's shape.
- **Automatic Line2D Placement.** Line2D nodes are automatically positioned over the polygon's surface on specified normal angles.

**Planned/Future:**

- **Automatic Light Occlusion.** Automatically generate a `LightOccluder2D` and position it over the polygon's area.
- **Navigation.** Select whether the shape represents an obstacle or a movable area and a `NavigationRegion2D` or a `NavigationObstacle2D` will be automatically generated and positioned accordingly.
- **Automatic Shaping.** Automatically generate the polygon's shape based on parameters like `geometry` (e.g. "circle", "star", "hexagon", etc.), `radius`, `sides`, etc.
- **Split/Merge.** Cut the polygon in the middle to split it into two, or merge two polygons into one.

## Installation

1. Copy the `Platform2D` folder into your project's `addons/` directory
2. Enable the plugin in **Project Settings → Plugins**
3. The `Platform2D` node will now be available in the "Add Node" dialog

<!--
## Usage

### Basic Setup

1. Add a `Platform2D` node to your scene
2. Add one or more `Path2D` nodes as children of the `Platform2D`
3. Edit the Path2D curves to define your platform shape
4. The polygon mesh will automatically generate based on the path points

### Example Scene Structure

```
Platform2D
├── Path2D (defines the platform outline)
├── Path2D (optional: additional polygons)
└── StaticBody2D (add your own collision shapes)
    └── CollisionPolygon2D
```
-->

<!--
## How It Works

- Each `Path2D` child node represents a separate polygon
- The plugin uses the baked points from each path's curve
- Multiple paths create multiple polygons within the same Platform2D node
- Changes to Path2D properties automatically trigger polygon regeneration

## Configuration

Platform2D inherits all properties from `Polygon2D`, including:

- **Texture** - Apply textures to your platform
- **Color** - Tint the platform mesh
- **UV** - Configure texture mapping
- **Skeleton** - Set up skeleton deformation
- And all other standard Polygon2D properties
-->

<!--
## Roadmap

Future planned features:

- ✨ Automatic `StaticBody2D` and `CollisionPolygon2D` generation
- 🎨 Edge texture configuration for automatic sprite placement
- 🌿 Random decoration sprite placement on edges or fill areas
- 🔧 Additional configuration options
-->

<!--
## Requirements

- **Godot**: 4.5.0 or later
- **.NET**: .NET 8.0
- **Language**: C#
-->

## MIT License

See the [LICENSE.txt](LICENSE.txt) file for details.

## Author

Leonardo Raele <leonardoraele@gmail.com>

<!--
## Version

**0.1.0** - Initial release
-->
