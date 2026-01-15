#if TOOLS
using Godot;

namespace Raele.Polyshape2D;

[Tool]
public partial class Plugin : EditorPlugin
{
	private readonly AngleRangePreviewPlugin edgeAnglePreviewPlugin = new();

	public override void _EnterTree()
	{
		Texture2D platformIcon = GD.Load<Texture2D>($"res://addons/{nameof(Raele.Polyshape2D)}/icons/platform2d.png");
		Texture2D anchorIcon = GD.Load<Texture2D>($"res://addons/{nameof(Raele.Polyshape2D)}/icons/anchor.png");

		this.AddCustomType(nameof(Polyshape2D), nameof(Polygon2D), GD.Load<Script>($"res://addons/{nameof(Raele.Polyshape2D)}/src/{nameof(Polyshape2D)}.cs"), platformIcon);
		this.AddCustomType(nameof(Polyshape2DAnchor), nameof(Node2D), GD.Load<Script>($"res://addons/{nameof(Raele.Polyshape2D)}/src/{nameof(Polyshape2DAnchor)}.cs"), anchorIcon);

		this.AddInspectorPlugin(edgeAnglePreviewPlugin);
	}

	public override void _ExitTree()
	{
		this.RemoveCustomType(nameof(Polyshape2D));
		this.RemoveCustomType(nameof(Polyshape2DAnchor));

		this.RemoveInspectorPlugin(edgeAnglePreviewPlugin);
	}
}
#endif
