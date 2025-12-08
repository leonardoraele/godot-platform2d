#if TOOLS
using Godot;

namespace Raele.Platform2D;

[Tool]
public partial class Plugin : EditorPlugin
{
	private readonly AngleRangePreviewPlugin edgeAnglePreviewPlugin = new();

	public override void _EnterTree()
	{
		Texture2D platformIcon = GD.Load<Texture2D>($"res://addons/{nameof(Raele.Platform2D)}/icons/platform2d.png");
		Texture2D anchorIcon = GD.Load<Texture2D>($"res://addons/{nameof(Raele.Platform2D)}/icons/anchor.png");

		this.AddCustomType(nameof(Platform2D), nameof(Polygon2D), GD.Load<Script>($"res://addons/{nameof(Raele.Platform2D)}/src/{nameof(Platform2D)}.cs"), platformIcon);
		this.AddCustomType(nameof(Platform2DAnchor), nameof(Node2D), GD.Load<Script>($"res://addons/{nameof(Raele.Platform2D)}/src/{nameof(Platform2DAnchor)}.cs"), anchorIcon);

		this.AddInspectorPlugin(edgeAnglePreviewPlugin);
	}

	public override void _ExitTree()
	{
		this.RemoveCustomType(nameof(Platform2D));
		this.RemoveCustomType(nameof(Platform2DAnchor));

		this.RemoveInspectorPlugin(edgeAnglePreviewPlugin);
	}
}
#endif
