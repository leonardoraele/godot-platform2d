#if TOOLS
using Godot;

namespace Raele.Platform2D;

[Tool]
public partial class Plugin : EditorPlugin
{
	public override void _EnterTree()
	{
		this.AddCustomType(nameof(Platform2D), nameof(Polygon2D), GD.Load<Script>($"res://addons/{nameof(Raele.Platform2D)}/src/{nameof(Platform2D)}.cs"), null);
	}

	public override void _ExitTree()
	{
		this.RemoveCustomType(nameof(Platform2D));
	}
}
#endif
