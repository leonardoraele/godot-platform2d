using System.Collections.Generic;
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

	[Export(PropertyHint.Range, "-180,180,5,radians_as_degrees")] public float BeginAngle = 0f;
	[Export(PropertyHint.Range, "-180,180,5,radians_as_degrees")] public float EndAngle = 0f;
	[Export] public bool Disabled = false;
	// [Export] public bool HasBeginCapSprite = false;
	// [Export] public bool HasEndCapSprite = false;

	// -----------------------------------------------------------------------------------------------------------------
	// FIELDS
	// -----------------------------------------------------------------------------------------------------------------

	private float LastCheckSum = float.NaN;

	// -----------------------------------------------------------------------------------------------------------------
	// PROPERTIES
	// -----------------------------------------------------------------------------------------------------------------

	private float CheckSum => this.BeginAngle
		+ this.EndAngle
		// + (this.HasBeginCapSprite ? 1f : 0f)
		// + (this.HasEndCapSprite ? 1f : 0f)
		;
	private bool HasChanges => !Mathf.IsEqualApprox(this.CheckSum, this.LastCheckSum);

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

	public bool CheckForChanges()
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
