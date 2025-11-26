// using System;
// using System.IO.Hashing;
// using System.Linq;
// using Godot;

namespace Raele.Platform2D;

public static class Utils
{
	// private static readonly XxHash32 Hasher = new();
	// public static int Hash32(params Variant[] values)
	// {
	// 	Hasher.Append(values.SelectMany(variant => variant.AsByteArray()).ToArray());
	// 	return Math.Abs(BitConverter.ToInt32(Hasher.GetHashAndReset()));
	// }
	// public static string HashStr(params Variant[] values)
	// 	=> $"{Hash32(values):X8}";
	// public static int GetCheckSum(GodotObject subject)
	// 	=> Hash32(
	// 		subject.GetPropertyList()
	// 			.Select(prop => prop["name"].AsString())
	// 			.Select(prop => subject.Get(prop))
	// 			.ToArray()
	// 	);
}
