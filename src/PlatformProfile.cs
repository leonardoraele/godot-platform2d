using Godot;

namespace Raele.Platform2D;

public partial class PlatformProfile : Resource
{
	[Export] public Godot.Collections.Array<EdgeSettings> EdgesSettings = [];
}
