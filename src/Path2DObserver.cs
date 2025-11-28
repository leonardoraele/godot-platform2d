using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Raele.Platform2D;

/// <summary>
/// Observer node for <see cref="Path2D"/> nodes to notify changes to parent <see cref="Platform2D"/> nodes.
/// </summary>
[Tool]
public partial class Path2DObserver : Node
{
	// -----------------------------------------------------------------------------------------------------------------
	// STATICS
	// -----------------------------------------------------------------------------------------------------------------

	// public static readonly TimeSpan CheckForChangesInterval = TimeSpan.FromSeconds(1) / 6;
	public static readonly TimeSpan LAG_DELAY = TimeSpan.FromMicroseconds(150);

	// -----------------------------------------------------------------------------------------------------------------
	// EXPORTS
	// -----------------------------------------------------------------------------------------------------------------

	// [Export] public

	// -----------------------------------------------------------------------------------------------------------------
	// FIELDS
	// -----------------------------------------------------------------------------------------------------------------

	private float LastFrameChecksum = float.NaN;
	private ulong CheckAfterTime = 0;

	// -----------------------------------------------------------------------------------------------------------------
	// PROPERTIES
	// -----------------------------------------------------------------------------------------------------------------

	// -----------------------------------------------------------------------------------------------------------------
	// SIGNALS
	// -----------------------------------------------------------------------------------------------------------------

	[Signal] public delegate void PathChangedEventHandler(Path2D path);
	[Signal] public delegate void LagDetectedEventHandler();

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

	public override void _Process(double delta)
	{
		base._Process(delta);
		double fps = 1.0 / delta;
		if (fps > 59 && Time.GetTicksMsec() >= this.CheckAfterTime)
		{
			this.CheckForChanges();
		}
		else if (fps <= 10)
		{
			this.EmitSignal(SignalName.LagDetected);
			GD.PushWarning($"{nameof(Platform2D)}: Updates to Path2D nodes are taking too long do process. ({fps:F0} FPS) Please increase the baked interval of your Path2D nodes.");
			this.CheckAfterTime = Time.GetTicksMsec() + (ulong) LAG_DELAY.TotalMilliseconds;
		}
	}

	// public override void _PhysicsProcess(double delta)
	// {
	// 	base._PhysicsProcess(delta);
	// }

	// public override string[] _GetConfigurationWarnings()
	// 	=> base._PhysicsProcess(delta);

	// -----------------------------------------------------------------------------------------------------------------
	// METHODS
	// -----------------------------------------------------------------------------------------------------------------

	// TODO If CheckForChanges detects a change, we should keep checking for changes at every frame for a time to
	// improve user experience. (so that the updates are not so laggy) If we do so, we can probably increase the timer
	// interval a bit to reduce performance impact.
	private void CheckForChanges()
	{
		float checksum = GetDataPoints().Sum();
		IEnumerable<float> GetDataPoints()
		{
			if (this.GetParent() is not Path2D parent)
			{
				GD.PushError($"{nameof(Path2DObserver)} broke. Cause: Unexpected parent node. Parent: {this.GetParent()?.GetType().Name ?? "null"}. Expected: {nameof(Path2D)}.");
				this.QueueFree();
				yield break;
			}
			yield return parent.Position.Angle();
			yield return parent.Scale.Angle();
			yield return parent.Rotation;
			yield return parent.Skew;
			yield return parent.Curve.BakeInterval;
			for (int i = 0; i < parent.Curve.PointCount; i++)
			{
				yield return parent.Curve.GetPointPosition(i).Angle();
				if (i != 0)
				{
					yield return parent.Curve.GetPointIn(i).Angle();
				}
				if (i != parent.Curve.PointCount -1)
				{
					yield return parent.Curve.GetPointOut(i).Angle();
				}
			}
		}
		if (this.LastFrameChecksum != float.NaN && checksum != this.LastFrameChecksum)
		{
			this.EmitSignal(SignalName.PathChanged, this.GetParent<Path2D>());
		}
		this.LastFrameChecksum = checksum;
	}
}
