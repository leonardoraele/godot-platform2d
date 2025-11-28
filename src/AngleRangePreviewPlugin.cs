#if TOOLS
using Godot;

namespace Raele.Platform2D;

[Tool]
public partial class AngleRangePreviewPlugin : EditorInspectorPlugin
{
	public override bool _CanHandle(GodotObject subject) => subject is AngleRangePreview.IHasAngleRange;
	public override bool _ParseProperty(
		GodotObject subject,
		Variant.Type type,
		string name,
		PropertyHint hintType,
		string hintString,
		PropertyUsageFlags usageFlags,
		bool wide
	)
	{
		if ((subject as AngleRangePreview.IHasAngleRange)?.ShouldAddAngleRangePreview(name) == true)
		{
			this.AddCustomControl(new AngleRangePreview()
			{
				AngleRangeSource = (subject as AngleRangePreview.IHasAngleRange)!,
			});
		}
		return false;
	}
}
#endif
