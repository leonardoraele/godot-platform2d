using System;
using System.Collections.Generic;
using System.IO.Hashing;
using System.Linq;
using Godot;
using Godot.Collections;

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

	public static readonly string EdgeLinesGroup = $"{nameof(Platform2D)}.{nameof(EdgeLinesGroup)}";

	// -----------------------------------------------------------------------------------------------------------------
	// EXPORTS
	// -----------------------------------------------------------------------------------------------------------------

	/// <summary>
	/// Tool button to refresh the polygon vertexes from child Path2D nodes.
	/// </summary>
	[ExportToolButton("Manual Refresh")] Callable ToolButtonRefresh => Callable.From(this.Refresh);

	[ExportGroup("Collider")]
	[Export(PropertyHint.GroupEnable)] public bool AutoUpdateColliderEnabled = false;
	[ExportToolButton("Create StaticBody2D")] Callable ToolButtonCreateCollider => Callable.From(this.OnCreateColliderPressed);

	[ExportGroup("Edges")]
	[Export] public Godot.Collections.Array<EdgeSettings> EdgesSettings = [];

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
		if (Engine.IsEditorHint())
		{
			this.Refresh();
		}
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		if (Engine.IsEditorHint() && this.EdgesSettings.Count(setting => setting.CheckForChanges()) > 0) {
			this.RefreshEdges();
		}
	}

	// public override void _PhysicsProcess(double delta)
	// {
	// 	base._PhysicsProcess(delta);
	// }

	public override void _ValidateProperty(Dictionary property)
	{
		if (property["name"].AsString() == nameof(this.ToolButtonCreateCollider))
		{
			property["usage"] = this.Collider == null
				? Variant.From(PropertyUsageFlags.Default)
				: Variant.From(PropertyUsageFlags.NoEditor);
		}
	}

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

	private void OnPathChanged(Path2D path) => this.Refresh();

	public void Refresh()
	{
		this.RefreshPolygonsVertexes();
		this.RefreshEdges();
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
		if (!this.AutoUpdateColliderEnabled || this.Collider == null)
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

	private void OnCreateColliderPressed()
	{
		CollisionObject2D collider = this.Collider ?? new StaticBody2D() { Name = nameof(StaticBody2D) };
		if (collider.GetParent() != this)
		{
			this.AddChild(collider);
			collider.Owner = this.Owner;
		}
		this.AddChild(collider);
	}

	private void RefreshEdges()
	{
		HashSet<Line2D> lineSet = new();
		XxHash3 hasher = new();

		string Hash(params int[] values) {
			hasher.Append(values.SelectMany(BitConverter.GetBytes).ToArray());
			return $"{BitConverter.ToUInt16(hasher.GetHashAndReset()):X4}";
		}

		foreach ((int index, Vector2[] vertexes) polygon in this.PolygonsVertexes.ToList().Index())
		{
			PolygonEdge[] edges = this.GetPolygonEdges(polygon.index).ToArray();
			foreach ((int index, EdgeSettings settings) edgeInfo in this.EdgesSettings.Where(settings => !settings.Disabled).Index()) {
				foreach ((int index, Vector2[] vertexes) segment in edgeInfo.settings.FindSegments(edges).ToList().Index())
				{
					string hash = Hash(polygon.index, edgeInfo.index, segment.index);
					Line2D line = this.GetOrCreateEdgeLine(hash);
					line.Points = segment.vertexes;
					line.Closed = segment.vertexes.Length == edges.Length;
					lineSet.Add(line);
				}
			}
		}

		// Clean up unused edge lines
		this.GetChildren()
			.OfType<Line2D>()
			.Where(line => line.IsInGroup(Platform2D.EdgeLinesGroup) && !lineSet.Contains(line))
			.ToList()
			.ForEach(Engine.IsEditorHint() ? line => line.QueueFree() : line => line.Visible = false);
	}

	private IEnumerable<PolygonEdge> GetPolygonEdges(int polygonIndex)
	{
		Vector2[] vertexes = this.PolygonsVertexes[polygonIndex];
		foreach ((int i, Vector2 vertex) in vertexes.Index())
		{
			yield return new PolygonEdge(vertex, vertexes[(i + 1) % vertexes.Length]);
		}
	}

	private Line2D GetOrCreateEdgeLine(string lineId) => this.GetEdgeLine(lineId) ?? this.CreateEdgeLine(lineId);
	private Line2D? GetEdgeLine(string id) => this.GetNodeOrNull<Line2D>(this.GetLineName(id));
	private Line2D CreateEdgeLine(string id)
	{
		Line2D line = new Line2D
		{
			Name = this.GetLineName(id),
		};
		this.AddChild(line);
		line.Owner = this.Owner;
		line.AddToGroup(Platform2D.EdgeLinesGroup);
		return line;
	}
	private string GetLineName(string id) => $"{nameof(Line2D)} (Edge #{id})";
}
