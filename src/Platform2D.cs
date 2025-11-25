using System;
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
	[ExportToolButton("Refresh")] Callable ToolButtonRefresh => Callable.From(this.Refresh);

	[Export] public bool Test;

	[ExportGroup("Collider")]
	[ExportToolButton("Create Static Collider")] Callable ToolButtonCreateStaticCollider => Callable.From(this.CreateStaticCollider);

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
	/// Alias for <see cref="Polygon2D.Polygon"/>. Contains a list of all vertexes that are part of any of the polygons
	/// in this platform.
	/// </summary>
	public Vector2[] AllVertexes
	{
		get => this.Polygon ?? [];
		set => this.Polygon = value;
	}
	/// <summary>
	/// An array of polygons, where each polygon is an array of the vertexes that compose the polygon. This is a "sugar
	/// property" to ease reading and writing polygon data. (<see cref="Polygon2D.Polygon"/> and
	/// <see cref="Polygon2D.Polygons"/>)
	/// </summary>
	public Vector2[][] PolygonsVertexes
	{
		get => this.Polygons.Count > 0
			? this.Polygons.Select(vertexes => vertexes.AsInt32Array().Select(index => this.AllVertexes[index]).ToArray()).ToArray()
			: [this.AllVertexes];
		set
		{
			this.AllVertexes = value.SelectMany(vertexes => vertexes).ToArray();
			this.Polygons = new(
				value.Select(
					(polygon, i) => Variant.From(
						polygon.Select(
								(_, j) => value.Take(i)
									.Select(vertexes => vertexes.Length)
									.Sum()
									+ j
							)
							.ToArray()
					)
				)
			);
		}
	}
	public int PolygonCount => this.Polygons.Count > 0 ? this.Polygons.Count : this.AllVertexes.Length > 0 ? 1 : 0;
	public CollisionObject2D? Collider => this.GetChildren().FirstOrDefault(child => child is CollisionObject2D) as CollisionObject2D;
	public IEnumerable<CollisionPolygon2D> CollisionPolygons => this.Collider?.GetChildren().OfType<CollisionPolygon2D>() ?? [];

	// -----------------------------------------------------------------------------------------------------------------
	// SIGNALS
	// -----------------------------------------------------------------------------------------------------------------

	// [Signal] public delegate void EventHandler()

	// -----------------------------------------------------------------------------------------------------------------
	// INTERNAL TYPES
	// -----------------------------------------------------------------------------------------------------------------

	// -----------------------------------------------------------------------------------------------------------------
	// GODOT EVENTS
	// -----------------------------------------------------------------------------------------------------------------

	public override void _EnterTree()
	{
		base._EnterTree();
		this.PropertyListChanged += this.OnPropertyListChanged;
		this.ChildEnteredTree += this.OnChildEnteredTree;
		this.ChildExitingTree += this.OnChildExitingTree;
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		this.PropertyListChanged -= this.OnPropertyListChanged;
		this.ChildEnteredTree -= this.OnChildEnteredTree;
		this.ChildExitingTree -= this.OnChildExitingTree;
	}

	public override void _Ready()
	{
		base._Ready();
		this.Refresh();
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

	private void OnChildEnteredTree(Node child)
	{
		if (child is Path2D path && path.GetParent() == this)
		{
			GD.PrintS("New Path2D child detected.");
			this.ChildPathsCache.Add(path);
			path.PropertyListChanged += this.Refresh;
		}
	}

	private void OnChildExitingTree(Node child)
	{
		if (child is Path2D path)
		{
			GD.PrintS("Path2D child exiting tree.");
			path.PropertyListChanged -= this.Refresh;
			this.ChildPathsCache.Remove(path);
		}
	}

	public void Refresh()
	{
		this.RefreshPolygonsVertexes();
		this.RefreshCollisionPolygons();
	}

	private void RefreshPolygonsVertexes()
	{
		if (this.ChildPathsCache.Count() == 0)
		{
			return;
		}
		this.PolygonsVertexes = this.ChildPathsCache.Select(path =>
			{
				Vector2[] vertexes = path.Curve.GetBakedPoints()
					.Select((vertex, index) => vertex + path.Position)
					.ToArray();
				return vertexes.Where((vertex, i) => !IsOmittable(vertexes, i)).ToArray();
			})
			.ToArray();
	}

	private static bool IsOmittable(Vector2[] vertexes, int index)
	{
		bool isfirst = index == 0;
		bool islast = index == vertexes.Length - 1;
		bool iscurve = !isfirst && !islast && IsCurve(vertexes[index], vertexes[index - 1], vertexes[index + 1]);
		GD.PrintS(new { index, isfirst, islast, iscurve });
		return !isfirst && !islast && !iscurve;
	}

	private static bool IsCurve(Vector2 vertex, Vector2 previous, Vector2 next)
	{
		var angle = Math.Abs((vertex - previous).AngleTo(next - vertex));
		bool iscurve = angle > 0.01f;
		GD.PrintS(new { vertex, previous, next, angle, iscurve });
		return iscurve;
	}

	private void CreateStaticCollider()
	{
		CollisionObject2D collider = this.Collider ?? new StaticBody2D() { Name = nameof(StaticBody2D) };
		if (collider.GetParent() != this)
		{
			this.AddChild(collider);
			collider.Owner = this.Owner;
		}
		this.RefreshCollisionPolygons();
	}

	private void RefreshCollisionPolygons()
	{
		if (this.Collider == null)
		{
			return;
		}
		// Add missing collision polygons
		for (int i = 0; i < this.PolygonCount - this.CollisionPolygons.Count(); i++)
		{
			CollisionPolygon2D collisionPolygon = new CollisionPolygon2D() { Name = nameof(CollisionPolygon2D) };
			this.Collider.AddChild(collisionPolygon);
			collisionPolygon.Owner = this.Owner;
		}
		// Remove extra collision polygons
		this.CollisionPolygons.Skip(this.PolygonCount).ToList().ForEach(polygon => polygon.QueueFree());
		// Update collision polygons
		CollisionPolygon2D[] collisionPolygons = this.CollisionPolygons.ToArray();
		for (int i = 0; i < collisionPolygons.Length; i++)
		{
			collisionPolygons[i].Polygon = this.PolygonsVertexes[i];
		}
	}

	private void OnPropertyListChanged()
	{
		GD.PrintS($"{nameof(Platform2D)}.{nameof(OnPropertyListChanged)}()");
	}
}
