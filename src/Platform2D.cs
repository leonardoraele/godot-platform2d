using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Raele.Platform2D;

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
	[ExportToolButton("Manual Refresh")] Callable ToolButtonRefresh => Callable.From(this.Refresh);

	[Export] public bool CreateAndUpdateCollider
	{
		get => field;
		set
		{
			field = value;
			if (Engine.IsEditorHint() && field && this.IsNodeReady())
			{
				this.Refresh();
			}
		}
	} = false;
	// [ExportToolButton("Create Static Collider")] Callable ToolButtonCreateStaticCollider => Callable.From(this.CreateStaticCollider);

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
	public CollisionObject2D? Collider
	{
		get => this.GetChildren().FirstOrDefault(child => child is CollisionObject2D) as CollisionObject2D;
		set => this.AddChild(value);
	}
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
			this.ChildPathsCache.Add(path);
			if (Engine.IsEditorHint())
			{
				Path2DObserver observer = new();
				observer.PathChanged += this.OnPathChanged;
				path.AddChild(observer);
			}
		}
	}

	private void OnChildExitingTree(Node child)
	{
		if (child is Path2D path)
		{
			this.ChildPathsCache.Remove(path);
		}
	}

	private void OnPathChanged(Path2D path)
	{
		this.Refresh();
	}

	public void Refresh()
	{
		this.RefreshPolygonsVertexes();
		this.RefreshCollisionPolygons();
		this.CollisionPolygons.ToList()
			.ForEach(polygon => polygon.BuildMode = this.InvertEnabled
				? CollisionPolygon2D.BuildModeEnum.Segments
				: CollisionPolygon2D.BuildModeEnum.Solids
			);
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
					.Select((vertex, index) => path.Transform * vertex)
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
		return !isfirst && !islast && !iscurve;
	}

	private static bool IsCurve(Vector2 vertex, Vector2 previous, Vector2 next)
	{
		var angle = Math.Abs((vertex - previous).AngleTo(next - vertex));
		// TODO Instead of comparing two adjacent vertexes against a fixed angle threshold, should accumulate the angle
		// over a series of vertexes until a maximum distance or number of vertexes is reached. The way it is now, we
		// miss curves that are made of many small angle changes.
		bool iscurve = angle > 0.001f;
		return iscurve;
	}

	private void RefreshCollisionPolygons()
	{
		if (!this.CreateAndUpdateCollider)
		{
			return;
		}

		this.Collider ??= this.CreateStaticCollider();

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

	private CollisionObject2D CreateStaticCollider()
	{
		CollisionObject2D collider = this.Collider ?? new StaticBody2D() { Name = nameof(StaticBody2D) };
		if (collider.GetParent() != this)
		{
			this.AddChild(collider);
			collider.Owner = this.Owner;
		}
		return collider;
	}
}
