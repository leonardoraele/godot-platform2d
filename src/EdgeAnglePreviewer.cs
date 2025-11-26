using System;
using System.Linq;
using Godot;

namespace Raele.Platform2D;

public partial class EdgeAnglePreviewer : Control
{
	// -----------------------------------------------------------------------------------------------------------------
	// STATICS
	// -----------------------------------------------------------------------------------------------------------------

	private static readonly int CIRCLE_DOTS_COUNT = 64;
	private static readonly float ARC_WIDTH = 32f;
	private static readonly float MARGIN = 16f;

	// -----------------------------------------------------------------------------------------------------------------
	// EXPORTS
	// -----------------------------------------------------------------------------------------------------------------

	// [Export] public

	// -----------------------------------------------------------------------------------------------------------------
	// FIELDS
	// -----------------------------------------------------------------------------------------------------------------

	public required Func<float> GetBeginAngle { get; init; }
	public required Func<float> GetEndAngle { get; init; }

	// -----------------------------------------------------------------------------------------------------------------
	// PROPERTIES
	// -----------------------------------------------------------------------------------------------------------------

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
		this.CustomMinimumSize = this.Size = new Vector2(256, 256);
		this.SizeFlagsHorizontal = SizeFlags.Fill;
	}

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
	// 	=> base._PhysicsProcess(delta);


	public override void _Draw()
	{
		base._Draw();

		Vector2 center = this.GetSize() / 2;
		float radius = Math.Min(center.X, center.Y) - MARGIN;
		float beginAngle = this.GetBeginAngle();
		float endAngle = this.GetEndAngle() + (this.GetBeginAngle() > this.GetEndAngle() ? Mathf.Tau : 0f);

		// this.DrawCircle(center, radius + ARC_WIDTH / 2, Colors.DarkSlateGray);
		this.DrawArc(center, radius, 0, Mathf.Tau, CIRCLE_DOTS_COUNT, Colors.DarkSlateGray, ARC_WIDTH);
		this.DrawArc(center, radius, beginAngle, endAngle, CIRCLE_DOTS_COUNT, Colors.White, ARC_WIDTH);
		this.DrawArc(center, radius - ARC_WIDTH / 2, 0, Mathf.Tau, CIRCLE_DOTS_COUNT, Colors.Black, 2f);
		this.DrawArc(center, radius + ARC_WIDTH / 2, 0, Mathf.Tau, CIRCLE_DOTS_COUNT, Colors.Black, 2f);
		Enumerable.Range(0, 4).Select(i => Vector2.Right.Rotated(Mathf.Tau * i / 4))
			.ToList()
			.ForEach(normal =>
			{
				this.DrawLine(
					center + normal * (radius - ARC_WIDTH / 2),
					center + normal * (radius + ARC_WIDTH / 2),
					Colors.Black,
					6f
				);
			});
		Enumerable.Range(0, 4).Select(i => Vector2.Right.Rotated(Mathf.Pi / 4 + Mathf.Tau * i / 4))
			.ToList()
			.ForEach(normal =>
			{
				this.DrawLine(
					center + normal * (radius - ARC_WIDTH / 2),
					center + normal * (radius + ARC_WIDTH / 2),
					Colors.Black,
					2f
				);
			});
		Enumerable.Range(0, 12).Select(i => Vector2.Right.Rotated(Mathf.Tau * i / 12))
			.ToList()
			.ForEach(normal =>
			{
				this.DrawLine(
					center + normal * (radius - ARC_WIDTH / 2),
					center + normal * (radius + ARC_WIDTH / 2),
					Colors.Black,
					1f
				);
			});
		if (this.GetBeginAngle() == this.GetEndAngle())
		{
			this.DrawLine(
				center + Vector2.Right.Rotated(beginAngle) * (radius - ARC_WIDTH / 2),
				center + Vector2.Right.Rotated(endAngle) * (radius + ARC_WIDTH / 2),
				Colors.White,
				1f
			);
		}
		this.DrawLine(
			center + Vector2.Left * (radius - ARC_WIDTH / 2),
			center + Vector2.Left * (radius + ARC_WIDTH / 2),
			Colors.Red,
			2f
		);
	}

	// -----------------------------------------------------------------------------------------------------------------
	// METHODS
	// -----------------------------------------------------------------------------------------------------------------


}
