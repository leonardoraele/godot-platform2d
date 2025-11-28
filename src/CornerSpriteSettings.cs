using System;
using Godot;

namespace Raele.Platform2D;

[Tool][GlobalClass]
public partial class CornerSpriteSettings : SpriteSettings, AngleRangePreview.IHasAngleRange
{
	// -----------------------------------------------------------------------------------------------------------------
	// STATICS
	// -----------------------------------------------------------------------------------------------------------------

	// public static readonly string MyConstant = "";

	// -----------------------------------------------------------------------------------------------------------------
	// EXPORTS
	// -----------------------------------------------------------------------------------------------------------------

	[Export] public CornerTypeEnum CornerType
		{ get => field; set { field = value; this.EmitChanged(); } } = CornerTypeEnum.Convex;
	[Export(PropertyHint.Range, "0,180,5,radians_as_degrees")] public float MinCornerAngle
		{ get => field; set { field = value; this.EmitChanged(); } } = Mathf.DegToRad(30);
	[Export] public bool DistortSprite // TODO Not implemented yet.
		{ get => field; set { field = value; this.EmitChanged(); } } = true;
	// TODO // FIXME This option is temporarily removed because turning it off causes index out of bounds error in the
	// test methods.
	// [Export] public bool IgnoreEdgeCaps
	// 	{ get => field; set { field = value; this.EmitChanged(); } } = false;
	[Export(PropertyHint.Range, "-180,180,5,radians_as_degrees")] public float BeginNormalAngle
		{ get => field; set { field = value; this.EmitChanged(); } } = -180f;
	[Export(PropertyHint.Range, "-180,180,5,radians_as_degrees")] public float EndNormalAngle
		{ get => field; set { field = value; this.EmitChanged(); } } = 180f;

	// -----------------------------------------------------------------------------------------------------------------
	// FIELDS
	// -----------------------------------------------------------------------------------------------------------------



	// -----------------------------------------------------------------------------------------------------------------
	// PROPERTIES
	// -----------------------------------------------------------------------------------------------------------------

	float AngleRangePreview.IHasAngleRange.BeginAngleRangeRad => this.BeginNormalAngle;
	float AngleRangePreview.IHasAngleRange.EndAngleRangeRad => this.EndNormalAngle;
	Func<string, bool> AngleRangePreview.IHasAngleRange.ShouldAddAngleRangePreview => prop => prop == nameof(this.BeginNormalAngle);

	// -----------------------------------------------------------------------------------------------------------------
	// SIGNALS
	// -----------------------------------------------------------------------------------------------------------------

	// [Signal] public delegate void EventHandler()

	// -----------------------------------------------------------------------------------------------------------------
	// INTERNAL TYPES
	// -----------------------------------------------------------------------------------------------------------------

	public enum CornerTypeEnum {
		/// <summary>
		/// A corner that points outward, like the top of a hill.
		/// </summary>
		Convex,
		/// <summary>
		/// A corner that points inward, like the inside of a bowl.
		/// </summary>
		Concave,
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

	// public override string[] _GetConfigurationWarnings()
	// 	=> Enumerable.Empty<string>()
	// 		.ToArray();

	// -----------------------------------------------------------------------------------------------------------------
	// METHODS
	// -----------------------------------------------------------------------------------------------------------------

	public bool Test(Vector2[] vertexes, int i)
		=> this.TestEdgeCap(vertexes, i)
			&& this.TestCornerAngle(vertexes, i)
			&& this.TestNormalAngle(vertexes, i);

	private bool TestEdgeCap(Vector2[] vertexes, int i)
		=> (i > 0 && i < vertexes.Length - 1) || /*this.IgnoreEdgeCaps*/ false; // TODO See "FIXME" comment for `IgnoreEdgeCaps` field.

	private bool TestCornerAngle(Vector2[] vertexes, int i)
	{
		Vector2 vertex = vertexes[i];
		Vector2 left = vertexes[i - 1];
		Vector2 right = vertexes[i + 1];
		float angle = left.DirectionTo(vertex).AngleTo(vertex.DirectionTo(right));
		return this.CornerType switch
			{
				CornerTypeEnum.Convex => angle > this.MinCornerAngle - Mathf.Epsilon,
				CornerTypeEnum.Concave => angle < this.MinCornerAngle * -1 + Mathf.Epsilon,
				_ => false,
			};
	}
	private bool TestNormalAngle(Vector2[] vertexes, int i)
	{
		Vector2 vertex = vertexes[i];
		Vector2 left = vertexes[i - 1];
		Vector2 right = vertexes[i + 1];
		float normalAngle = (left.DirectionTo(vertex) + right.DirectionTo(vertex)).Angle();

		return this.BeginNormalAngle > this.EndNormalAngle
			? normalAngle >= this.BeginNormalAngle || normalAngle <= this.EndNormalAngle
			: normalAngle >= this.BeginNormalAngle && normalAngle <= this.EndNormalAngle;
	}
}
