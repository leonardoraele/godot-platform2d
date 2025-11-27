using System;
using System.Linq;
using Godot;

namespace Raele.Platform2D;

public static class Utils
{
	/// <summary>
	/// Gets a variant and returns a float between 0 and 1 that describes that value near-uniquely, similar to a hash.
	/// </summary>
	public static float HashF(Variant variant)
		=> variant.VariantType switch
		{
			Variant.Type.Bool => variant.AsBool() ? 1f : 0f,
			Variant.Type.Int => HashF(BitConverter.GetBytes(variant.AsSingle())),
			Variant.Type.Float => HashF(BitConverter.GetBytes(variant.AsSingle())),
			Variant.Type.String => variant.AsString().Hash() / (float) uint.MaxValue,
			Variant.Type.Vector2 => new Variant[] { variant.AsVector2().X, variant.AsVector2().Y }.Select(HashF).Average(),
			Variant.Type.Vector2I => new Variant[] { variant.AsVector2I().X, variant.AsVector2I().Y }.Select(HashF).Average(),
			Variant.Type.Rect2 => new Variant[] { variant.AsRect2().Position, variant.AsRect2().Size }.Select(HashF).Average(),
			Variant.Type.Rect2I => new Variant[] { variant.AsRect2I().Position, variant.AsRect2I().Size }.Select(HashF).Average(),
			Variant.Type.Vector3 => new Variant[] { variant.AsVector3().X, variant.AsVector3().Y, variant.AsVector3().Z }.Select(HashF).Average(),
			Variant.Type.Vector3I => new Variant[] { variant.AsVector3I().X, variant.AsVector3I().Y, variant.AsVector3I().Z }.Select(HashF).Average(),
			Variant.Type.Transform2D => new Variant[]
			{
				variant.AsTransform2D()[0].X,
				variant.AsTransform2D()[0].Y,
				variant.AsTransform2D()[1].X,
				variant.AsTransform2D()[1].Y,
				variant.AsTransform2D()[2].X,
				variant.AsTransform2D()[2].Y
			}.Select(HashF).Average(),
			Variant.Type.Vector4 => new Variant[] { variant.AsVector4().X, variant.AsVector4().Y, variant.AsVector4().Z, variant.AsVector4().W }.Select(HashF).Average(),
			Variant.Type.Vector4I => new Variant[] { variant.AsVector4I().X, variant.AsVector4I().Y, variant.AsVector4I().Z, variant.AsVector4I().W }.Select(HashF).Average(),
			Variant.Type.Plane => new Variant[] { variant.AsPlane().Normal.X, variant.AsPlane().Normal.Y, variant.AsPlane().Normal.Z, variant.AsPlane().D }.Select(HashF).Average(),
			Variant.Type.Quaternion => new Variant[] { variant.AsQuaternion().X, variant.AsQuaternion().Y, variant.AsQuaternion().Z, variant.AsQuaternion().W }.Select(HashF).Average(),
			Variant.Type.Aabb => new Variant[] { variant.AsAabb().Position, variant.AsAabb().Size }.Select(HashF).Average(),
			Variant.Type.Basis => new Variant[]
			{
				variant.AsBasis()[0].X,
				variant.AsBasis()[0].Y,
				variant.AsBasis()[0].Z,
				variant.AsBasis()[1].X,
				variant.AsBasis()[1].Y,
				variant.AsBasis()[1].Z,
				variant.AsBasis()[2].X,
				variant.AsBasis()[2].Y,
				variant.AsBasis()[2].Z
			}.Select(HashF).Average(),
			Variant.Type.Transform3D => new Variant[]
			{
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
			}.Select(HashF).Average(),
			Variant.Type.Projection => 1f,
			Variant.Type.Color => HashF(BitConverter.GetBytes(variant.AsColor().ToRgba32())),
			Variant.Type.StringName => HashF(variant.AsStringName().ToString()),
			Variant.Type.NodePath => HashF(variant.AsNodePath().ToString()),
			Variant.Type.Rid => HashF(variant.AsRid().Id),
			Variant.Type.Object => HashF(variant.AsGodotObject() is Resource res ? res.ResourcePath : variant.AsGodotObject().GetInstanceId()),
			Variant.Type.Callable => 1f,
			Variant.Type.Signal => 1f,
			Variant.Type.Dictionary => new Variant[] { Variant.From(variant.AsGodotDictionary().Keys), Variant.From(variant.AsGodotDictionary().Values) }.Select(HashF).Average(),
			Variant.Type.Array => variant.AsGodotArray().Select(HashF).Average(),
			Variant.Type.PackedByteArray => variant.AsByteArray().Select(b => b / (float) byte.MaxValue).Average(),
			Variant.Type.PackedInt32Array => variant.AsInt32Array().Select(i => HashF(i)).Average(),
			Variant.Type.PackedInt64Array => variant.AsInt64Array().Select(i => HashF(i)).Average(),
			Variant.Type.PackedFloat32Array => variant.AsFloat32Array().Select(f => HashF(f)).Average(),
			Variant.Type.PackedFloat64Array => variant.AsFloat64Array().Select(f => HashF(f)).Average(),
			Variant.Type.PackedStringArray => variant.AsStringArray().Select(s => HashF(s)).Average(),
			Variant.Type.PackedVector2Array => variant.AsVector2Array().Select(v => HashF(v)).Average(),
			Variant.Type.PackedVector3Array => variant.AsVector3Array().Select(v => HashF(v)).Average(),
			Variant.Type.PackedVector4Array => variant.AsVector4Array().Select(v => HashF(v)).Average(),
			Variant.Type.PackedColorArray => variant.AsColorArray().Select(c => HashF(c)).Average(),
			_ => 0f
		};
	public static float HashF(params Variant[] variant) => variant.Select(HashF).Sum();
}
