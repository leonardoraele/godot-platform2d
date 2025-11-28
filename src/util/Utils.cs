using System;
using System.Linq;
using Godot;

namespace Raele.Platform2D;

public static class Utils
{
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
			Variant.Type.Array => variant.AsGodotArray().Select(HashF).Sum(),
			Variant.Type.PackedByteArray => variant.AsByteArray().Select(b => b / (float) byte.MaxValue).Sum(),
			Variant.Type.PackedInt32Array => variant.AsInt32Array().Select(i => HashF(i)).Sum(),
			Variant.Type.PackedInt64Array => variant.AsInt64Array().Select(i => HashF(i)).Sum(),
			Variant.Type.PackedFloat32Array => variant.AsFloat32Array().Select(f => HashF(f)).Sum(),
			Variant.Type.PackedFloat64Array => variant.AsFloat64Array().Select(f => HashF(f)).Sum(),
			Variant.Type.PackedStringArray => variant.AsStringArray().Select(s => HashF(s)).Sum(),
			Variant.Type.PackedVector2Array => variant.AsVector2Array().Select(v => HashF(v)).Sum(),
			Variant.Type.PackedVector3Array => variant.AsVector3Array().Select(v => HashF(v)).Sum(),
			Variant.Type.PackedVector4Array => variant.AsVector4Array().Select(v => HashF(v)).Sum(),
			Variant.Type.PackedColorArray => variant.AsColorArray().Select(c => HashF(c)).Sum(),
			_ => 0f
		};
	public static float HashF(params Variant[] variants) => variants.Select(HashF).Sum();
}
