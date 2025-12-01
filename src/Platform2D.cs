using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using GodotDictionary = Godot.Collections.Dictionary;

namespace Raele.Platform2D;

// TODO Add option to randomly place selected texture sprites on the polygon (on either edge or fill) — for decorations
// or variation.
// TODO Add option to rotate edge line texture and sprite textures without having to use shaders.
// TODO Account for whether the polygon is inverted for collision shape generation and assumptions about angle concavity
// for corner sprites.
// TODO Consider removing inherited skeleton-based deformation editor fields from Polygon2D.
// TODO Remove Polygon2D.Offset field from the editor and add instant actions to reposition the polygon origin, such as
// "move origin to center" and "move origin to bottom". To do that, translate the node's position so that the origin is
// at the desired location, then translate all vertexes in the opposite direction so that they remain in place.
// TODO Consider how to handle "holes" in polygons (e.g. donut shapes).

[Tool]
public partial class Platform2D : Polygon2D
{
	// -----------------------------------------------------------------------------------------------------------------
	// STATICS
	// -----------------------------------------------------------------------------------------------------------------

	public static readonly string EdgeLineGroupName = $"{nameof(Platform2D)}__{nameof(EdgeLineGroupName)}";
	public static readonly string LineSpritesGroupName = $"{nameof(Platform2D)}__{nameof(LineSpritesGroupName)}";

	// -----------------------------------------------------------------------------------------------------------------
	// EXPORTS
	// -----------------------------------------------------------------------------------------------------------------

	/// <summary>
	/// Tool button to refresh the polygon vertexes from child Path2D nodes.
	/// </summary>
	[ExportToolButton("Manual Refresh")] Callable ToolButtonRefresh => Callable.From(this.Refresh);

	[Export] public PlatformProfile? Profile
	{
		get => field;
		set {
			field = value;
			if (field != null)
			{
				field.Changed += this.Refresh;
			}
			this.Refresh();
		}
	} = null;
	[Export] public bool MimicChildPath
		{ get => field; set { field = value; this.Refresh(); } } = false;

	[ExportGroup("Has Collision")]
	[Export(PropertyHint.GroupEnable)] public bool CollisionEnabled
		{ get => field; set { field = value; this.RefreshCollisionPolygons(); this.NotifyPropertyListChanged(); } }
		= false;
	[ExportToolButton("Create StaticBody2D")] Callable ToolButtonCreateCollider
		=> Callable.From(this.OnCreateColliderPressed);
	[Export] public CollisionPolygon2D? CollisionPolygon2D
		{ get => field; set { field = value; this.RefreshCollisionPolygons(); } } = null;

	[ExportGroup("Debug Options")]
	[Export] public bool ShowChildrenInSceneTree
		{ get => field; set { field = value; this.Refresh(); } } = false;

	// -----------------------------------------------------------------------------------------------------------------
	// FIELDS
	// -----------------------------------------------------------------------------------------------------------------

	private Path2D? ChildPath2D => this.GetChildren().OfType<Path2D>().FirstOrDefault();

	// -----------------------------------------------------------------------------------------------------------------
	// PROPERTIES
	// -----------------------------------------------------------------------------------------------------------------

	public Vector2[] Vertexes
	{
		get => this.Polygon ?? [];
		set => this.Polygon = value;
	}
	// /// <summary>
	// /// Alias for <see cref="Polygon2D.Polygon"/>. Contains a list of all vertexes that are part of any of the polygons
	// /// in this platform.
	// /// </summary>
	// public Vector2[] AllVertexes
	// {
	// 	get => this.Polygon ?? [];
	// 	set => this.Polygon = value;
	// }
	// /// <summary>
	// /// An array of polygons, where each polygon is an array of the vertexes that compose the polygon. This is a "sugar
	// /// property" to ease reading and writing polygon data. (<see cref="Polygon2D.Polygon"/> and
	// /// <see cref="Polygon2D.Polygons"/>)
	// /// </summary>
	// public Vector2[][] PolygonsVertexes
	// {
	// 	get => this.Polygons.Count > 0
	// 		? this.Polygons.Select(vertexes => vertexes.AsInt32Array().Select(index => this.AllVertexes[index]).ToArray()).ToArray()
	// 		: [this.AllVertexes];
	// 	set
	// 	{
	// 		this.AllVertexes = value.SelectMany(vertexes => vertexes).ToArray();
	// 		this.Polygons = new(
	// 			value.Select(
	// 				(polygon, i) => Variant.From(
	// 					polygon.Select(
	// 							(_, j) => value.Take(i)
	// 								.Select(vertexes => vertexes.Length)
	// 								.Sum()
	// 								+ j
	// 						)
	// 						.ToArray()
	// 				)
	// 			)
	// 		);
	// 	}
	// }
	public CollisionObject2D? CollisionObject
		=> this.GetChildren().FirstOrDefault(child => child is CollisionObject2D) as CollisionObject2D;
	private float LastCheckSum = float.NaN;
	private IEnumerable<EdgeSettings> EdgesSettings => this.Profile?.EdgeTypes?.OfType<EdgeSettings>() ?? [];

	// -----------------------------------------------------------------------------------------------------------------
	// SIGNALS
	// -----------------------------------------------------------------------------------------------------------------

	// [Signal] public delegate void EventHandler()

	// -----------------------------------------------------------------------------------------------------------------
	// INTERNAL TYPES
	// -----------------------------------------------------------------------------------------------------------------

	public enum UnusedEdgesStrategyEnum
	{
		Hide,
		Delete,
	}

	// -----------------------------------------------------------------------------------------------------------------
	// GODOT EVENTS
	// -----------------------------------------------------------------------------------------------------------------

	public override void _EnterTree()
	{
		base._EnterTree();
		this.ChildEnteredTree += this.OnChildEnteredTree;
		// this.ChildExitingTree += this.OnChildExitingTree;
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		this.ChildEnteredTree -= this.OnChildEnteredTree;
		// this.ChildExitingTree -= this.OnChildExitingTree;
	}

	public override void _Ready()
	{
		base._Ready();
		// if (Engine.IsEditorHint())
		// {
		// 	this.Refresh();
		// }
		this.Refresh();
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		if (Engine.IsEditorHint())
		{
			this.CheckForChanges();
		}
	}

	// public override void _PhysicsProcess(double delta)
	// {
	// 	base._PhysicsProcess(delta);
	// }

	public override void _ValidateProperty(GodotDictionary property)
	{
		switch (property["name"].AsString())
		{
			case "texture":
			case "texture_offset":
			case "texture_scale":
			case "texture_rotation":
			case "offset":
			case "polygons":
			case "internal_vertex_count":
				property["usage"] = Variant.From(PropertyUsageFlags.NoEditor);
				break;
			case nameof(this.ToolButtonCreateCollider):
				property["usage"] = this.CollisionObject == null || this.CollisionPolygon2D == null
					? Variant.From(PropertyUsageFlags.Default)
					: Variant.From(PropertyUsageFlags.NoEditor);
				break;
		}
	}

	public override string[] _GetConfigurationWarnings()
		=> new List<string>()
			.Concat(this.Profile == null ? ["No profile assigned. Please assign a new profile resource in the inspector."] : [])
			.Concat(
				this.MimicChildPath
					? this.GetChildren().OfType<Path2D>().Count() switch
					{
						0 => [$"Option {nameof(MimicChildPath)} is enabled, but there is no child Path2D node. Please add one."],
						1 => [],
						_ => ["Multiple child Path2D nodes found. There should be only a single one. Paths after the first will be ignored."],
					}
					: []
			)
			.ToArray();

	// -----------------------------------------------------------------------------------------------------------------
	// EVENT HANDLERS
	// -----------------------------------------------------------------------------------------------------------------

	private void OnCreateColliderPressed()
	{
		if (this.CollisionObject == null)
		{
			StaticBody2D collider = new StaticBody2D() { Name = nameof(Godot.StaticBody2D) };
			this.AddChild(collider);
			collider.Owner = this.Owner;
		}
		if (this.CollisionPolygon2D == null)
		{
			this.CollisionPolygon2D = new CollisionPolygon2D() { Name = nameof(Godot.CollisionPolygon2D) };
			this.CollisionObject!.AddChild(this.CollisionPolygon2D);
			this.CollisionPolygon2D.Owner = this.CollisionObject.Owner;
		}
		this.RefreshCollisionPolygons();
		this.NotifyPropertyListChanged();
	}

	private void OnChildEnteredTree(Node child)
	{
		if (child is Path2D path && path.GetParent() == this)
		{
			if (Engine.IsEditorHint())
			{
				Path2DObserver observer = new();
				observer.PathChanged += this.OnPathChanged;
				path.AddChild(observer);
			}
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
		this.RefreshFillTexture();
		this.RefreshMimicPath2DVertexes();
		this.RefreshEdges();
		this.RefreshCollisionPolygons();
		this.Polygons = default;
		this.InternalVertexCount = default;
	}

	private void RefreshFillTexture() => this.Profile?.ConfigureTexture(this);

	/// <summary>
	/// Sets this Polygon2D's vertex positions based on the child Path2D nodes, if any.
	/// </summary>
	private void RefreshMimicPath2DVertexes()
	{
		if (!this.MimicChildPath || this.ChildPath2D == null)
		{
			return;
		}
		Vector2[] vertexes = this.ChildPath2D.Curve.GetBakedPoints()
			.Select((vertex, index) => this.ChildPath2D.Transform * vertex)
			.ToArray();
		this.Vertexes = vertexes.Where((vertex, i) => !IsOmittable(vertexes, i)).ToArray();
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
		if (!this.CollisionEnabled || this.CollisionPolygon2D == null)
		{
			return;
		}

		// this.CollisionPolygon2D.GlobalPosition = this.GlobalPosition;
		// this.CollisionPolygon2D.GlobalRotation = this.GlobalRotation;
		// this.CollisionPolygon2D.GlobalScale = this.GlobalScale;
		// this.CollisionPolygon2D.GlobalSkew = this.GlobalSkew;
		this.CollisionPolygon2D.Polygon = this.Vertexes;
		this.CollisionPolygon2D.BuildMode = this.InvertEnabled
			? CollisionPolygon2D.BuildModeEnum.Segments
			: CollisionPolygon2D.BuildModeEnum.Solids;
	}

	/// <summary>
	/// Updates child Line2D points to match the current polygon edges, following the edge settings defined by the user.
	/// New Line2D nodes are automatically created as needed, reused on future refreshes when possible, and deleted if
	/// no longer needed.
	/// </summary>
	private void RefreshEdges()
	{
		IEnumerator<(int index, Line2D line)> lineQueue = this.GetChildren()
			.OfType<Line2D>()
			.Where(line => line.IsInGroup(Platform2D.EdgeLineGroupName))
			.Index()
			.GetEnumerator();

		PolygonEdge[] edges = this.GetEdges().ToArray();
		foreach ((int index, EdgeSettings settings) edgeInfo in this.EdgesSettings.Where(settings => !settings.Disabled).Index()) {
			EdgeSettings.FindSegmentsResult result = edgeInfo.settings.FindSegments(edges);
			foreach ((int index, Vector2[] vertexes) segment in result.Segments.Index())
			{
				Line2D line = lineQueue.MoveNext() ? lineQueue.Current.line : new Line2D();
				// Must assign Points before calling EdgeSettings.ConfigureLine() because that method reads and
				// updates the line's points.
				line.Points = segment.vertexes;
				edgeInfo.settings.Apply(line);
				line.Closed = result.Closed;
				line.Owner = this.ShowChildrenInSceneTree ? this.Owner : null;
				line.AddToGroup(Platform2D.EdgeLineGroupName);
				if (line.GetParent() != this)
				{
					this.AddChild(line);
				}
				this.MoveChild(line, lineQueue.Current.index);
				this.RefreshEdgeSprites(edgeInfo.settings, line);
			}
		}

		// Clean up unused edge lines
		while (lineQueue.MoveNext())
		{
			if (Engine.IsEditorHint()) lineQueue.Current.line.QueueFree();
			else lineQueue.Current.line.Visible = false;
		}
	}

	private void RefreshEdgeSprites(EdgeSettings edgeSettings, Line2D line)
	{
		IEnumerator<Sprite2D> lineSprites = line.GetChildren().OfType<Sprite2D>()
			.Where(sprite => sprite.IsInGroup(Platform2D.LineSpritesGroupName))
			.GetEnumerator();

		// Begin cap sprite
		if (edgeSettings.BeginCapSprite != null)
		{
			Sprite2D sprite = lineSprites.MoveNext() ? lineSprites.Current : this.CreateLineSprite(line);
			sprite.Position = line.Points[0];
			edgeSettings.BeginCapSprite.Apply(sprite);
		}

		// Corner sprites
		foreach ((int index, Vector2 position) point in line.Points.Index())
		{
			CornerSpriteSettings? cornerSettings = edgeSettings.CornerSprites
				?.Where(settings => settings?.Test(line.Points, point.index) == true)
				.OrderByDescending(settings => settings!.MinCornerAngle)
				.FirstOrDefault();

			if (cornerSettings == null)
			{
				continue;
			}

			Sprite2D sprite = lineSprites.MoveNext() ? lineSprites.Current : this.CreateLineSprite(line);
			sprite.Position = point.position;
			cornerSettings.Apply(sprite);
		}

		// End cap sprite
		if (edgeSettings.EndCapSprite != null)
		{
			Sprite2D sprite = lineSprites.MoveNext() ? lineSprites.Current : this.CreateLineSprite(line);
			sprite.Position = line.Points[^1];
			edgeSettings.EndCapSprite.Apply(sprite);
		}

		while (lineSprites.MoveNext())
		{
			if (Engine.IsEditorHint()) lineSprites.Current.QueueFree();
			else lineSprites.Current.Visible = false;
		}
	}

	private Sprite2D CreateLineSprite(Line2D line)
	{
		Sprite2D sprite = new();
		line.AddChild(sprite);
		sprite.AddToGroup(Platform2D.LineSpritesGroupName);
		sprite.Owner = this.ShowChildrenInSceneTree ? this.Owner : null;
		return sprite;
	}

	/// <summary>
	/// Yields all edges of the polygon at the given index. (one Polygon2D node can contain multiple polygons)
	/// </summary>
	private IEnumerable<PolygonEdge> GetEdges()
	{
		foreach ((int i, Vector2 vertex) in this.Vertexes.Index())
		{
			yield return new PolygonEdge(vertex, this.Vertexes[(i + 1) % this.Vertexes.Length]);
		}
	}

	/// <summary>
	/// Checks for changes in the polygon data and refreshes if any are detected.
	/// </summary>
	private void CheckForChanges()
	{
		float checksum = Utils.HashF(this.Vertexes, this.InvertEnabled, this.InvertBorder);
		if (this.LastCheckSum != checksum)
		{
			this.Refresh();
		}
		this.LastCheckSum = checksum;
	}
}
