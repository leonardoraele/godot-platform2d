using System;
using System.Collections.Generic;
using System.IO.Hashing;
using System.Linq;
using Godot;
using GodotDictionary = Godot.Collections.Dictionary;

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

	public static readonly string EdgeLineMetaKey = $"{nameof(Platform2D)}__{nameof(EdgeLineMetaKey)}";

	// -----------------------------------------------------------------------------------------------------------------
	// EXPORTS
	// -----------------------------------------------------------------------------------------------------------------

	/// <summary>
	/// Tool button to refresh the polygon vertexes from child Path2D nodes.
	/// </summary>
	[ExportToolButton("Manual Refresh")] Callable ToolButtonRefresh => Callable.From(this.Refresh);

	[Export] public PlatformProfile? Profile;

	[ExportGroup("Collider")]
	[Export(PropertyHint.GroupEnable)] public bool AutoUpdateColliderEnabled = false;
	[ExportToolButton("Create StaticBody2D")] Callable ToolButtonCreateCollider => Callable.From(this.OnCreateColliderPressed);

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
	private float CheckSum => Utils.HashF(this.AllVertexes);
	private float LastCheckSum = float.NaN;
	private IEnumerable<EdgeSettings> EdgesSettings => this.Profile?.EdgesSettings.Where(setting => setting != null) ?? [];

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
		if (Engine.IsEditorHint())
		{
			// TODO FIXME Commenting this for now because the refresh interrupts the user editing the polygon. We should
			// find a better way to automatically detect changes to the polygon and call Refresh without impacting the
			// user experience. For now, the user has to manually press the "Manual Refresh" button.
			// if (this.ConsumeChanges())
			// {
			// 	this.Refresh();
			// }
			// else
			if (this.EdgesSettings.Count(setting => setting.ConsumeChanges()) > 0)
			{
				this.RefreshEdges();
			}
		}
	}

	// public override void _PhysicsProcess(double delta)
	// {
	// 	base._PhysicsProcess(delta);
	// }

	public override void _ValidateProperty(GodotDictionary property)
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
	// EVENT HANDLERS
	// -----------------------------------------------------------------------------------------------------------------

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

	// -----------------------------------------------------------------------------------------------------------------
	// METHODS
	// -----------------------------------------------------------------------------------------------------------------

	/// <summary>
	/// Updates this Polygon2D's vertexes based on child Path2D nodes, and updates child Line2D and collision polygons
	/// to match the current shape of this Polygon2D. (accounting for edge settings when updating the Line2D nodes)
	/// </summary>
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

	/// <summary>
	/// Sets this Polygon2D's vertex positions based on the child Path2D nodes, if any.
	/// </summary>
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

	/// <summary>
	/// Determines whether a vertex can be omitted when reconstructing the polygon shape from a Path2D. It checks if
	/// multiple vertexes are lined up in a straight line and returns true for the ones in the middle. This is an
	/// important optimization and is necessary because Path2D nodes are always baked based on distance even if multiple
	/// vertexes are lined up straight.
	/// </summary>
	private static bool IsOmittable(Vector2[] vertexes, int index)
	{
		bool isfirst = index == 0;
		bool islast = index == vertexes.Length - 1;
		bool iscurve = !isfirst && !islast && IsCurve(vertexes[index], vertexes[index - 1], vertexes[index + 1]);
		return !isfirst && !islast && !iscurve;
	}

	/// <summary>
	/// Checks if the angle formed by three vertexes indicates a curve (as opposed to a straight line).
	/// </summary>
	private static bool IsCurve(Vector2 vertex, Vector2 previous, Vector2 next)
	{
		var angle = Math.Abs((vertex - previous).AngleTo(next - vertex));
		// TODO Instead of comparing two adjacent vertexes against a fixed angle threshold, should accumulate the angle
		// over a series of vertexes until a maximum distance or number of vertexes is reached. The way it is now, we
		// miss curves that are made of many small angle changes.
		bool iscurve = angle > 0.001f;
		return iscurve;
	}

	/// <summary>
	/// Updates child CollisionPolygon2D nodes to match the shape of this Polygon2D, creating or deleting nodes as
	/// needed. There should be one CollisionPolygon2D node for each one polygon that exists in this Polygon2D.
	/// </summary>
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

	/// <summary>
	/// Updates child Line2D points to match the current polygon edges, following the edge settings defined by the user.
	/// New Line2D nodes are automatically created as needed, reused on future refreshes when possible, and deleted if
	/// no longer needed.
	/// </summary>
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
				EdgeSettings.FindSegmentsResult result = edgeInfo.settings.FindSegments(edges);
				foreach ((int index, Vector2[] vertexes) segment in result.Segments.Index())
				{
					string hash = Hash(polygon.index, edgeInfo.index, segment.index);
					Line2D line = this.GetEdgeLine(hash) ?? this.CreateEdgeLine(hash);
					edgeInfo.settings.ConfigureLine(line);
					line.Name = $"Edge #{hash} [{nameof(Line2D)}]";
					line.Points = segment.vertexes;
					line.Closed = result.Closed;
					lineSet.Add(line);
				}
			}
		}

		// Clean up unused edge lines
		this.GetChildren()
			.OfType<Line2D>()
			.Where(line => line.HasMeta(Platform2D.EdgeLineMetaKey))
			.ToHashSet()
			.Except(lineSet)
			.ToList()
			.ForEach(Engine.IsEditorHint() ? line => line.QueueFree() : line => line.Visible = false);
	}

	/// <summary>
	/// Yields all edges of the polygon at the given index. (one Polygon2D node can contain multiple polygons)
	/// </summary>
	private IEnumerable<PolygonEdge> GetPolygonEdges(int polygonIndex)
	{
		Vector2[] vertexes = this.PolygonsVertexes[polygonIndex];
		foreach ((int i, Vector2 vertex) in vertexes.Index())
		{
			yield return new PolygonEdge(vertex, vertexes[(i + 1) % vertexes.Length]);
		}
	}

	/// <summary>
	/// Gets the child Line2D node that corresponds to the given edge ID, or null if not found.
	/// </summary>
	private Line2D? GetEdgeLine(string id) => this.GetChildren()
		.OfType<Line2D>()
		.FirstOrDefault(line => this.GetLineId(line) == id);

	/// <summary>
	/// Creates a new Line2D node with the given edge ID. The node is added as a child of this Platform2D and its
	/// owner is set to match this Platform2D's owner.
	/// </summary>
	private Line2D CreateEdgeLine(string id)
	{
		Line2D line = new();
		this.SetLineId(line, id);
		this.AddChild(line);
		line.Owner = this.Owner;
		return line;
	}

	/// <summary>
	/// Assigns an ID to the given Line2D node as metadata.
	/// </summary>
	private void SetLineId(Line2D line, string id) => line.SetMeta(Platform2D.EdgeLineMetaKey, id);

	/// <summary>
	/// Retrieves the ID that was assigned to a Line2D with the <see cref="SetLineId"/> method, or an empty string if
	/// it's not found.
	/// </summary>
	private string GetLineId(Line2D line) => line.GetMeta(Platform2D.EdgeLineMetaKey, "").AsString();

	/// <summary>
	/// Checks if any relevant property has changed since the last time this method was called. It reads
	/// <see cref="CheckSum"/> to determine if anything changed, and updates <see cref="LastCheckSum"/> accordingly.
	/// </summary>
	private bool ConsumeChanges()
	{
		float currentCheckSum = this.CheckSum;
		if (currentCheckSum == this.LastCheckSum)
		{
			return false;
		}
		this.LastCheckSum = currentCheckSum;
		return true;
	}
}
