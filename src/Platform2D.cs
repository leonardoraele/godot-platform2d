using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Raele.Platform2D;

// TODO Add boolean option to automatically create StaticBody2D and CollisionPolygon2D children nodes based on the
// polygon shape.
// TODO Add option to configure textures for the edges.
// TODO Add option to randomly place selected texture sprites on the polygon (on either edge or fill) — for decorations
// or variation.

[Tool]
public partial class Platform2D : Polygon2D
{
	// -----------------------------------------------------------------------------------------------------------------
	// STATICS
	// -----------------------------------------------------------------------------------------------------------------

	// public static readonly string MyConstant = "";

	// -----------------------------------------------------------------------------------------------------------------
	// EXPORTS
	// -----------------------------------------------------------------------------------------------------------------

	/// <summary>
	/// Tool button to refresh the polygon vertexes from child Path2D nodes.
	/// </summary>
	[ExportToolButton("Refresh")] Callable ToolButtonRefresh => Callable.From(this.RefreshPolygons);

	// -----------------------------------------------------------------------------------------------------------------
	// FIELDS
	// -----------------------------------------------------------------------------------------------------------------

	/// <summary>
	/// Cache of child <see cref="Path2D"/> nodes.
	/// </summary>
	private List<Path2D> ChildPathsCache { get; init; } = new();

	// -----------------------------------------------------------------------------------------------------------------
	// PROPERTIES
	// -----------------------------------------------------------------------------------------------------------------

	/// <summary>
	/// Alias for <see cref="Polygon2D.Polygon"/>.
	/// </summary>
	public Vector2[] Vertexes
	{
		get => this.Polygon;
		set => this.Polygon = value;
	}

	// -----------------------------------------------------------------------------------------------------------------
	// SIGNALS
	// -----------------------------------------------------------------------------------------------------------------

	// [Signal] public delegate void EventHandler()

	// -----------------------------------------------------------------------------------------------------------------
	// INTERNAL TYPES
	// -----------------------------------------------------------------------------------------------------------------

	// private enum Type {
	// 	Value1,
	// }

	// -----------------------------------------------------------------------------------------------------------------
	// GODOT EVENTS
	// -----------------------------------------------------------------------------------------------------------------

	public override void _EnterTree()
	{
		base._EnterTree();
		this.ChildEnteredTree += this.OnChildEnteredTree;
		this.ChildExitingTree += this.OnChildExitingTree;
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		this.ChildEnteredTree -= this.OnChildEnteredTree;
		this.ChildExitingTree -= this.OnChildExitingTree;
	}

	public override void _Ready()
	{
		base._Ready();
		this.RefreshPolygons();
		this.PropertyListChanged += this.RefreshPolygons;
	}

	// public override void _Process(double delta)
	// {
	// 	base._Process(delta);
	// }

	// public override void _PhysicsProcess(double delta)
	// {
	// 	base._PhysicsProcess(delta);
	// }

	// public override string[] _GetConfigurationWarnings()
	// 	=> base._PhysicsProcess(delta);

	// -----------------------------------------------------------------------------------------------------------------
	// METHODS
	// -----------------------------------------------------------------------------------------------------------------

	public void RefreshPolygons()
	{
		if (this.ChildPathsCache.Count == 0)
		{
			return;
		}
		this.Vertexes = this.ChildPathsCache.SelectMany(path => path.Curve.GetBakedPoints()).ToArray();
		this.Polygons = this.BuildPolygons();
	}

	private Godot.Collections.Array BuildPolygons()
	{
		Godot.Collections.Array result = new();
		for (int i = 0; i < this.ChildPathsCache.Count; i++)
		{
			int startIndex = this.ChildPathsCache.Take(i).Sum(p => p.Curve.GetBakedPoints().Length);
			int[] polygonVertexIndexes = Enumerable.Range(startIndex, this.ChildPathsCache[i].Curve.GetBakedPoints().Length).ToArray();
			result.Add(polygonVertexIndexes);
		}
		return result;
	}

	private void OnChildEnteredTree(Node child)
	{
		if (child is Path2D path && path.GetParent() == this)
		{
			this.ChildPathsCache.Add(path);
			path.PropertyListChanged += this.RefreshPolygons;
		}
	}

	private void OnChildExitingTree(Node child)
	{
		if (child is Path2D path)
		{
			path.PropertyListChanged -= this.RefreshPolygons;
			this.ChildPathsCache.Remove(path);
		}
	}
}
