using Godot;

namespace Raele.Platform2D;

[Tool][GlobalClass]
public partial class PlatformProfile : Resource
{
	[Export] public Godot.Collections.Array<EdgeSettings> EdgesSettings = [];
}
