using System;
using System.Linq;
using Godot;
using static Godot.GodotObject;

namespace Raele.Platform2D;

public static class Utils
{
	// TODO We should probably use variant.GetHashCode() and/or HashCode.Combine()
	/// <summary>
	/// Gets a variant and returns a float that describes that value near-uniquely, similar to a hash. The return is not
	/// guaranteed to be unique, but different inputs should ideally return different outputs. The returned float is
	/// always between 0 and the number of bytes in the data.
	/// </summary>
	public static float HashF(Variant variant)
		=> variant.VariantType switch
		{
			Variant.Type.Bool => variant.AsBool() ? 1f : 0f,
			Variant.Type.Int => HashF(BitConverter.GetBytes(variant.AsSingle())),
			Variant.Type.Float => HashF(BitConverter.GetBytes(variant.AsSingle())),
			Variant.Type.String => variant.AsString().Hash() / (float) uint.MaxValue,
			Variant.Type.Vector2 => HashF(variant.AsVector2().X, variant.AsVector2().Y),
			Variant.Type.Vector2I => HashF(variant.AsVector2I().X, variant.AsVector2I().Y),
			Variant.Type.Rect2 => HashF(variant.AsRect2().Position, variant.AsRect2().Size),
			Variant.Type.Rect2I => HashF(variant.AsRect2I().Position, variant.AsRect2I().Size),
			Variant.Type.Vector3 => HashF(variant.AsVector3().X, variant.AsVector3().Y, variant.AsVector3().Z),
			Variant.Type.Vector3I => HashF(variant.AsVector3I().X, variant.AsVector3I().Y, variant.AsVector3I().Z),
			Variant.Type.Transform2D => HashF(
				variant.AsTransform2D()[0].X,
				variant.AsTransform2D()[0].Y,
				variant.AsTransform2D()[1].X,
				variant.AsTransform2D()[1].Y,
				variant.AsTransform2D()[2].X,
				variant.AsTransform2D()[2].Y
			),
			Variant.Type.Vector4 => HashF(variant.AsVector4().X, variant.AsVector4().Y, variant.AsVector4().Z, variant.AsVector4().W),
			Variant.Type.Vector4I => HashF(variant.AsVector4I().X, variant.AsVector4I().Y, variant.AsVector4I().Z, variant.AsVector4I().W),
			Variant.Type.Plane => HashF(variant.AsPlane().Normal.X, variant.AsPlane().Normal.Y, variant.AsPlane().Normal.Z, variant.AsPlane().D),
			Variant.Type.Quaternion => HashF(variant.AsQuaternion().X, variant.AsQuaternion().Y, variant.AsQuaternion().Z, variant.AsQuaternion().W),
			Variant.Type.Aabb => HashF(variant.AsAabb().Position, variant.AsAabb().Size),
			Variant.Type.Basis => HashF(
				variant.AsBasis()[0].X,
				variant.AsBasis()[0].Y,
				variant.AsBasis()[0].Z,
				variant.AsBasis()[1].X,
				variant.AsBasis()[1].Y,
				variant.AsBasis()[1].Z,
				variant.AsBasis()[2].X,
				variant.AsBasis()[2].Y,
				variant.AsBasis()[2].Z
			),
			Variant.Type.Transform3D => HashF(
				variant.AsTransform3D()[0].X,
				variant.AsTransform3D()[0].Y,
				variant.AsTransform3D()[0].Z,
				variant.AsTransform3D()[1].X,
				variant.AsTransform3D()[1].Y,
				variant.AsTransform3D()[1].Z,
				variant.AsTransform3D()[2].X,
				variant.AsTransform3D()[2].Y,
				variant.AsTransform3D()[2].Z,
				variant.AsTransform3D()[3].X,
				variant.AsTransform3D()[3].Y,
				variant.AsTransform3D()[3].Z
			),
			Variant.Type.Projection => HashF(
				variant.AsProjection().X,
				variant.AsProjection().Y,
				variant.AsProjection().Z,
				variant.AsProjection().W
			),
			Variant.Type.Color => HashF(BitConverter.GetBytes(variant.AsColor().ToRgba32())),
			Variant.Type.StringName => HashF(variant.AsStringName().ToString()),
			Variant.Type.NodePath => HashF(variant.AsNodePath().ToString()),
			Variant.Type.Rid => HashF(variant.AsRid().Id),
			Variant.Type.Object => HashF(
				variant.AsGodotObject() is Resource res
					? res.ResourcePath
					: variant.AsGodotObject().GetInstanceId()
			),
			Variant.Type.Callable => 1f,
			Variant.Type.Signal => 1f,
			Variant.Type.Dictionary => HashF(
				Variant.From(variant.AsGodotDictionary().Keys),
				Variant.From(variant.AsGodotDictionary().Values)
			),
			Variant.Type.Array => variant.AsGodotArray().Sum(HashF),
			Variant.Type.PackedByteArray => variant.AsByteArray().Sum(b => b / (float) byte.MaxValue),
			Variant.Type.PackedInt32Array => variant.AsInt32Array().Sum(i => HashF(i)),
			Variant.Type.PackedInt64Array => variant.AsInt64Array().Sum(i => HashF(i)),
			Variant.Type.PackedFloat32Array => variant.AsFloat32Array().Sum(f => HashF(f)),
			Variant.Type.PackedFloat64Array => variant.AsFloat64Array().Sum(f => HashF(f)),
			Variant.Type.PackedStringArray => variant.AsStringArray().Sum(s => HashF(s)),
			Variant.Type.PackedVector2Array => variant.AsVector2Array().Sum(v => HashF(v)),
			Variant.Type.PackedVector3Array => variant.AsVector3Array().Sum(v => HashF(v)),
			Variant.Type.PackedVector4Array => variant.AsVector4Array().Sum(v => HashF(v)),
			Variant.Type.PackedColorArray => variant.AsColorArray().Sum(c => HashF(c)),
			_ => 0f
		};

	public static float HashF(params Variant[] variants) => variants.Sum(HashF);

	public static void ObserveArrayExport<[MustBeVariant] T>(Resource subject, Godot.Collections.Array<T?>? array)
	{
		foreach (Resource resource in array?.OfType<Resource>() ?? [])
		{
			Utils.TryConnect(resource, Resource.SignalName.Changed, new Callable(subject,Resource.MethodName.EmitChanged));
		}
	}

	public static bool TryConnect(GodotObject subject, StringName signalName, Action action, params ConnectFlags[] flags)
		=> Utils.TryConnect(subject, signalName, Callable.From(action), flags);

	/// <summary>
	/// Similar to <see cref="Godot.Connect"/>, but if the signal is already connected, returns `false` instead of
	/// throwing an exception. If the signal connection is created, returns `true`.
	/// </summary>
	public static bool TryConnect(GodotObject subject, StringName signalName, Callable callable, params ConnectFlags[] flags)
	{
		uint flagsInt = flags.Select(flag => (uint) flag).Aggregate(0u, (a, b) => a | b);
		return Utils.TryConnect(subject, signalName, callable, flagsInt);
	}

	public static bool TryConnect(GodotObject subject, StringName signalName, Callable callable, long flags)
	{
		if (Utils.TestSignalConnected(subject, signalName, callable, flags))
		{
			return false;
		}
		subject.Connect(signalName, callable, (uint) flags);
		return true;
	}

	private static bool TestSignalConnected(
		GodotObject subject,
		StringName signalName,
		Callable test,
		long flags
	)
		=> subject.GetSignalConnectionList(signalName)
			.Where(signal => signal["flags"].AsInt64() == flags)
			.Select(signal => signal["callable"].AsCallable())
			.Any(callable => CallableEquals(callable, test));

	public static bool CallableEquals(Callable lhs, Callable rhs)
		=> lhs.Target == rhs.Target
			&& (lhs.Method ?? lhs.Delegate.Method.Name) == (rhs.Method ?? rhs.Delegate.Method.Name);
}
