using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Raele.Platform2D;

[Tool][GlobalClass]
public partial class EdgeSettings : Resource, AngleRangePreview.IHasAngleRange
{
	// -----------------------------------------------------------------------------------------------------------------
	// STATICS
	// -----------------------------------------------------------------------------------------------------------------

	// public static readonly string MyConstant = "";

	// -----------------------------------------------------------------------------------------------------------------
	// EXPORTS
	// -----------------------------------------------------------------------------------------------------------------

	[Export] public string Name = Guid.NewGuid().ToString().Substring(0, 4).ToUpper();
	// TODO If this field is false, we only create straight Line2D segments so that there are no deformations.
	// [Export] public bool WeldCorners
	// 	{ get => field; set { field = value; this.EmitChanged(); } } = true;

	// [Export] public PackedScene? Template;
	[ExportCategory("Edge Location")]
	[Export(PropertyHint.Range, "-180,180,5,radians_as_degrees")] public float BeginAngle
		{ get => field; set { field = value; this.EmitChanged(); } } = 0f;
	[Export(PropertyHint.Range, "-180,180,5,radians_as_degrees")] public float EndAngle
		{ get => field; set { field = value; this.EmitChanged(); } } = 0f;
	// TODO Make this field an array and a boolean to determine whether multiple overlaping Line2D nodes with each
	// sprite should be created, or only a single Line2D with a random sample of the list of textures.
	[Export] public Texture2D? Texture
		{ get => field; set { field = value; this.EmitChanged(); } } = null;
	[Export] public bool Disabled
		{ get => field; set { field = value; this.EmitChanged(); } } = false;

	[ExportGroup("Texture Options")]
	[Export(PropertyHint.Range, "0,1,or_greater,or_less")] public float Offset
		{ get => field; set { field = value; this.EmitChanged(); } } = 0.5f;
	[Export(PropertyHint.Range, "0,2,0.01,or_greater,or_less")] public float Width
		{ get => field; set { field = value; this.EmitChanged(); } } = 1f;
	[Export] public Line2D.LineTextureMode TextureMode
		{ get => field; set { field = value; this.EmitChanged(); } } = Line2D.LineTextureMode.Tile;
	[Export] public Line2D.LineJointMode JointMode
		{ get => field; set { field = value; this.EmitChanged(); } } = Line2D.LineJointMode.Round;
	[Export] public Color Tint
		{ get => field; set { field = value; this.EmitChanged(); } } = Colors.White;
	[Export] public Gradient? Gradient
		{ get => field; set { field = value; this.EmitChanged(); } } = null;
	[Export] public Material? Material
		{ get => field; set { field = value; this.EmitChanged(); } } = null;

	[ExportGroup("Corner Sprites")]
	[Export] public Godot.Collections.Array<CornerSpriteSettings> CornerSprites
		{ get => field; set { field = value; this.EmitChanged(); } } = [];

	[ExportGroup("Cap Sprites")]
	[Export] public Texture2D? BeginCapSprite;
	[Export] public Texture2D? EndCapSprite;

	// -----------------------------------------------------------------------------------------------------------------
	// FIELDS
	// -----------------------------------------------------------------------------------------------------------------

	// -----------------------------------------------------------------------------------------------------------------
	// PROPERTIES
	// -----------------------------------------------------------------------------------------------------------------

	public bool Closed => Mathf.IsEqualApprox(this.BeginAngle + Mathf.Tau, this.EndAngle);
	float AngleRangePreview.IHasAngleRange.BeginAngleRangeRad => this.BeginAngle;
	float AngleRangePreview.IHasAngleRange.EndAngleRangeRad => this.EndAngle;
	Func<string, bool> AngleRangePreview.IHasAngleRange.ShouldAddAngleRangePreview
		=> prop => prop == nameof(this.BeginAngle);

	// -----------------------------------------------------------------------------------------------------------------
	// SIGNALS
	// -----------------------------------------------------------------------------------------------------------------

	// [Signal] public delegate void EventHandler();

	// -----------------------------------------------------------------------------------------------------------------
	// INTERNAL TYPES
	// -----------------------------------------------------------------------------------------------------------------

	public record FindSegmentsResult {
		public required Vector2[][] Segments { get; init; }
		public required bool Closed { get; init; }
	}

	// -----------------------------------------------------------------------------------------------------------------
	// GODOT EVENTS
	// -----------------------------------------------------------------------------------------------------------------

	// public override void _EnterTree()
	// {
	// 	base._EnterTree();
	// }

	// public override void _ExitTree()
	// {
	// 	base._ExitTree();
	// }

	// public override void _Ready()
	// {
	// 	base._Ready();
	// }

	// public override void _Process(double delta)
	// {
	// 	base._Process(delta);
	// }

	// public override void _PhysicsProcess(double delta)
	// {
	// 	base._PhysicsProcess(delta);
	// }

	// -----------------------------------------------------------------------------------------------------------------
	// METHODS
	// -----------------------------------------------------------------------------------------------------------------

	public bool Test(PolygonEdge edge) => this.Test(edge.Normal);
	public bool Test(Vector2 normal) => this.Test(normal.Angle());
	public bool Test(float rotation) => this.BeginAngle <= this.EndAngle
		? this.BeginAngle <= rotation && rotation <= this.EndAngle
			// This is necessary because if the surface normal is exactly Vector2.Left, the rotation angle will be
			// negative Pi, thus outside of the [BeginAngle, EndAngle] interval.
			|| Mathf.IsEqualApprox(rotation + Mathf.Tau, this.EndAngle)
		: rotation <= this.EndAngle || this.BeginAngle <= rotation;

	public void Apply(Line2D line)
	{
		line.Points = this.ExpandShape(line.Points);
		line.Texture = this.Texture;
		line.Width = (this.Texture?.GetHeight() ?? 10f) * this.Width;
		line.TextureMode = this.TextureMode;
		line.JointMode = this.JointMode;
		line.DefaultColor = this.Tint;
		line.Gradient = this.Gradient;
		line.Material = this.Material;
		line.TextureRepeat = CanvasItem.TextureRepeatEnum.Enabled;
	}

	private Vector2[] ExpandShape(Vector2[] vertexes)
	{
		if (this.Texture == null || vertexes.Length < 2)
		{
			return vertexes;
		}
		float distance = this.Texture.GetHeight() * (this.Offset - 0.5f) * this.Width;
		if (Mathf.IsEqualApprox(distance, 0f))
		{
			return vertexes;
		}
		IEnumerable<Vector2> _ExpandShape()
		{
			yield return vertexes[0] + vertexes[0].DirectionTo(vertexes[1]).Orthogonal() * distance;
			for (int i = 1; i < vertexes.Length - 1; i++)
			{
				Vector2 curr = vertexes[i];
				Vector2 left = curr.DirectionTo(vertexes[i - 1]);
				Vector2 right = curr.DirectionTo(vertexes[i + 1]);
				int direction = Mathf.Sign(left.Cross(right));
				Vector2 normal = direction == 0 ? right.Orthogonal() : (left + right).Normalized() * direction;
				yield return curr + normal * distance;
			}
			yield return vertexes[^1] + vertexes[^2].DirectionTo(vertexes[^1]).Orthogonal() * distance;
		}
		return _ExpandShape().ToArray();
	}

	public FindSegmentsResult FindSegments(PolygonEdge[] edges)
	{
		// If the first and last edges pass, ignore the first segment until we find an edge that does not pass. The
		// first segment will be handled as part of the last segment at the end of the loop.
		int startIndex = this.Test(edges[0]) && this.Test(edges[^1])
			? edges.Index().FirstOrDefault(tuple => !this.Test(tuple.Item), (-1, null!)).Index
			: 0;

		// If there is no start index (i.e. all edges passed the test, meaning the edge loops around the entire
		// polygon), then yield the entire polygon surface perimeter as a single segment.
		if (startIndex == -1)
		{
			return new()
			{
				Segments = [edges.Select(edge => edge.Left).ToArray()],
				Closed = true,
			};
		}

		IEnumerable<Vector2[]> _FindSegments()
		{
			List<Vector2> buffer = new();
			for (int i = startIndex; i < edges.Length || buffer.Count > 0; i++)
			{
				PolygonEdge GetEdge() => edges[i % edges.Length];
				if (this.Test(GetEdge()))
				{
					buffer.Add(GetEdge().Left);
				}
				else if (buffer.Count > 0)
				{
					buffer.Add(GetEdge().Left);
					yield return buffer.ToArray();
					buffer.Clear();
				}
			}
		}
		return  new()
		{
			Segments = _FindSegments().ToArray(),
			Closed = false,
		};
	}
}
