using System;
using System.Collections.Generic;
using System.IO.Hashing;
using System.Linq;
using Godot;

namespace Raele.Platform2D;

[Tool][GlobalClass]
public partial class EdgeSettings : Resource
{
	// -----------------------------------------------------------------------------------------------------------------
	// STATICS
	// -----------------------------------------------------------------------------------------------------------------

	// public static readonly string MyConstant = "";

	// -----------------------------------------------------------------------------------------------------------------
	// EXPORTS
	// -----------------------------------------------------------------------------------------------------------------

	// [Export] public PackedScene? Template;
	[Export(PropertyHint.Range, "-180,180,5,radians_as_degrees")] public float BeginAngle = 0f;
	[Export(PropertyHint.Range, "-180,180,5,radians_as_degrees")] public float EndAngle = 0f;
	[Export] public bool Disabled = false;
	// [Export] public bool HasBeginCapSprite = false;
	// [Export] public bool HasEndCapSprite = false;

	[ExportGroup("Texture")]
	[Export] public Texture2D? Texture;
	[Export(PropertyHint.Range, "0,2,0.01,or_greater,or_less")] public float WidthMultiplier = 1f;
	[Export] public Line2D.LineTextureMode TextureMode = Line2D.LineTextureMode.Tile;
	[Export] public Line2D.LineJointMode JointMode = Line2D.LineJointMode.Round;
	[Export] public Color Tint = Colors.White;
	[Export] public Gradient? Gradient;
	[Export] public Material? Material;

	[ExportGroup("Cap Sprites")]
	[Export(PropertyHint.GroupEnable)] public bool HasCapSprites = false;
	[Export] public Texture2D? BeginCapSprite;
	[Export] public Texture2D? EndCapSprite;

	// -----------------------------------------------------------------------------------------------------------------
	// FIELDS
	// -----------------------------------------------------------------------------------------------------------------

	private float LastCheckSum = -1;

	// -----------------------------------------------------------------------------------------------------------------
	// PROPERTIES
	// -----------------------------------------------------------------------------------------------------------------

	private bool HasChanges => this.CheckSum != this.LastCheckSum;
	private float CheckSum =>
		this.BeginAngle
		+ this.EndAngle
		+ Convert.ToSingle(this.Disabled)
		+ (this.Texture?.GetHashCode() ?? 0)
		+ this.WidthMultiplier
		+ (float) this.TextureMode
		+ (float) this.JointMode
		+ this.Tint.ToRgba32()
		+ (this.Gradient?.GetHashCode() ?? 0)
		+ (this.Material?.GetHashCode() ?? 0)
		+ Convert.ToSingle(this.HasCapSprites)
		+ (this.BeginCapSprite?.GetHashCode() ?? 0)
		+ (this.EndCapSprite?.GetHashCode() ?? 0);

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

	public bool ConsumeChanges()
	{
		bool result = this.HasChanges;
		if (result)
		{
			this.EmitSignal(Resource.SignalName.Changed);
			this.Reset();
		}
		return result;
	}

	private void Reset() => this.LastCheckSum = this.CheckSum;

	public bool Test(PolygonEdge edge) => this.Test(edge.Normal);
	public bool Test(Vector2 normal) => this.Test(normal.Angle());
	public bool Test(float rotation) => this.BeginAngle <= this.EndAngle
		? this.BeginAngle <= rotation && rotation <= this.EndAngle
		: rotation <= this.EndAngle || this.BeginAngle <= rotation;

	public void ConfigureLine(Line2D line)
	{
		line.Texture = this.Texture;
		line.TextureRepeat = CanvasItem.TextureRepeatEnum.Enabled;
		line.Width = (this.Texture?.GetHeight() ?? 10f) * this.WidthMultiplier;
		line.TextureMode = this.TextureMode;
		line.JointMode = this.JointMode;
		line.DefaultColor = this.Tint;
		line.Gradient = this.Gradient;
		line.Material = this.Material;
	}

	public IEnumerable<Vector2[]> FindSegments(PolygonEdge[] edges)
	{
		// Find a starting index where the edge does not pass the test. This ensures we start outside a segment, and
		// prevents yielding an incomplete segment at the end of the loop.
		int startIndex = edges.Index().FirstOrDefault(tuple => !this.Test(tuple.Item)).Index;

		// Check if all edges passed the test, then yield the entire polygon surface perimeter as a single segment.
		if (startIndex == 0 && this.Test(edges[0]))
		{
			yield return edges.Select(edge => edge.Left).ToArray();
			yield break;
		}

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
}
