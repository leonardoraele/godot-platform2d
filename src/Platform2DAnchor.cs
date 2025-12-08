using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Raele.Platform2D;

[Tool]
public partial class Platform2DAnchor : Node2D
{
	// -----------------------------------------------------------------------------------------------------------------
	// STATICS
	// -----------------------------------------------------------------------------------------------------------------

	// public static readonly string MyConstant = "";

	// -----------------------------------------------------------------------------------------------------------------
	// EXPORTS
	// -----------------------------------------------------------------------------------------------------------------

	// [Export] public

	// -----------------------------------------------------------------------------------------------------------------
	// FIELDS
	// -----------------------------------------------------------------------------------------------------------------

	private Platform2D? Parent;
	private Vector2 LastCalculatedPosition = Vector2.Inf;

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
		this.Parent = this.GetParent() as Platform2D;
	}

	// public override void _ExitTree()
	// {
	// 	base._ExitTree();
	// }

	// public override void _Ready()
	// {
	// 	base._Ready();
	// }

	public override void _Process(double delta)
	{
		base._Process(delta);
		if (!Engine.IsEditorHint())
		{
			this.SetProcess(false);
			return;
		}
		if (this.LastCalculatedPosition == this.Position)
		{
			return;
		}
		if (this.Parent == null || Parent.Vertexes.Length == 0)
		{
			return;
		}
		else if (this.Parent.Vertexes.Length == 1)
		{
			this.Position = this.Parent.Vertexes[0];
			return;
		}
		(Vector2 position, Vector2 direction) = Enumerable.Range(0, this.Parent.Vertexes.Length)
			.Select(i => (
				this.Parent.Vertexes[i],
				this.Parent.Vertexes[(i + 1) % this.Parent.Vertexes.Length]
			))
			.Select(edge => (
				Geometry2D.GetClosestPointToSegment(this.Position, edge.Item1, edge.Item2),
				edge.Item1.DirectionTo(edge.Item2)
			))
			.Aggregate<(Vector2 position, Vector2 direction)>((solutionA, solutionB) =>
				solutionA.position.DistanceSquaredTo(this.Position)
				< solutionB.position.DistanceSquaredTo(this.Position)
					? solutionA
					: solutionB
			);
		this.LastCalculatedPosition = this.Position = position;
		this.Rotation = direction.Orthogonal().Angle();
	}

	// public override void _PhysicsProcess(double delta)
	// {
	// 	base._PhysicsProcess(delta);
	// }

	public override string[] _GetConfigurationWarnings()
		=> new List<string>()
			.Concat(
				this.Parent == null
					? [$"The {nameof(Platform2DAnchor)} node must be a child of a {nameof(Platform2D)} node."]
					: []
			)
			.ToArray();

	// -----------------------------------------------------------------------------------------------------------------
	// METHODS
	// -----------------------------------------------------------------------------------------------------------------


}
