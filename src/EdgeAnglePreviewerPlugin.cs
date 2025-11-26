using Godot;

namespace Raele.Platform2D;

public partial class EdgeAnglePreviewerPlugin : EditorInspectorPlugin
{
	public override bool _CanHandle(GodotObject subject) => subject is EdgeSettings;
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
		if (name == nameof(EdgeSettings.BeginAngle) && subject is EdgeSettings settings)
		{
			EdgeAnglePreviewer preview = new()
			{
				GetBeginAngle = () => settings.BeginAngle,
				GetEndAngle = () => settings.EndAngle,
			};
			this.AddCustomControl(preview);
			settings.Changed += preview.QueueRedraw;
		}
		return false;
	}
}
