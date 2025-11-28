using Godot;
using GodotDictionary = Godot.Collections.Dictionary;

namespace Raele.Platform2D;

[Tool][GlobalClass]
public partial class CornerSpriteSettings : SpriteSettings
{
	// -----------------------------------------------------------------------------------------------------------------
	// STATICS
	// -----------------------------------------------------------------------------------------------------------------

	// public static readonly string MyConstant = "";

	// -----------------------------------------------------------------------------------------------------------------
	// EXPORTS
	// -----------------------------------------------------------------------------------------------------------------

	[Export(PropertyHint.Range, "0,2,suffix:π")] public float MinAngle = 0f;
	[Export(PropertyHint.Range, "0,2,suffix:π")] public float MaxAngle = 0f;
	[Export] public CornerTypeEnum CornerType = CornerTypeEnum.Convex;
	[Export(PropertyHint.Flags)] public uint OnEdges = uint.MaxValue;

	// -----------------------------------------------------------------------------------------------------------------
	// FIELDS
	// -----------------------------------------------------------------------------------------------------------------



	// -----------------------------------------------------------------------------------------------------------------
	// PROPERTIES
	// -----------------------------------------------------------------------------------------------------------------



	// -----------------------------------------------------------------------------------------------------------------
	// SIGNALS
	// -----------------------------------------------------------------------------------------------------------------

	// [Signal] public delegate void EventHandler()

	// -----------------------------------------------------------------------------------------------------------------
	// INTERNAL TYPES
	// -----------------------------------------------------------------------------------------------------------------

	public enum CornerTypeEnum {
		/// <summary>
		/// A corner that points outward, like the top of a hill.
		/// </summary>
		Convex,
		/// <summary>
		/// A corner that points inward, like the inside of a bowl.
		/// </summary>
		Concave,
	}

	// -----------------------------------------------------------------------------------------------------------------
	// GODOT EVENTS
	// -----------------------------------------------------------------------------------------------------------------

	// public override void _EnterTree()
	// {
	// 	base._EnterTree();
	// }

	// public override void _ExitTree()
	// {
	// 	base._ExitTree();
	// }

	// public override void _Ready()
	// {
	// 	base._Ready();
	// }

	// public override void _Process(double delta)
	// {
	// 	base._Process(delta);
	// }

	// public override void _PhysicsProcess(double delta)
	// {
	// 	base._PhysicsProcess(delta);
	// }

	// public override string[] _GetConfigurationWarnings()
	// 	=> Enumerable.Empty<string>()
	// 		.ToArray();

	public override void _ValidateProperty(GodotDictionary property)
	{
		base._ValidateProperty(property);
		if (property["name"].AsString() == nameof(this.OnEdges))
		{
			property["hint_string"] = PlatformProfile.GetEdgeOptionsForCorner(this);
		}
	}

	// -----------------------------------------------------------------------------------------------------------------
	// METHODS
	// -----------------------------------------------------------------------------------------------------------------


}
