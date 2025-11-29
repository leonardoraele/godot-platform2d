using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
	[Export(PropertyHint.Range, "0,2,suffix:π")] public float MaxAngle = 2f;
	[Export] public CornerTypeEnum CornerType = CornerTypeEnum.Convex;
	/// <summary>
	/// This is an ethemeral export, displayed in the editor but not saved in the resource file. It is used as a
	/// bitfield to select which edges this corner sprite applies to. Changing this field updates the
	/// <see cref="AppliedEdgeNames"/> field.
	/// </summary>
	[Export(PropertyHint.Flags)] public uint OnEdges
	{
		get
		{
			return field = this.AppliedEdgeNames.Select(name => this.TryGetFlagByName(name, out uint flag) ? flag : 0)
				.Aggregate((uint) 0, (a, b) => a | b);
		}
		set
		{
			// Remove unselected edges
			Enumerable.Range(0, 32)
				.Select(i => ((uint) 1) << i)
				.Where(flag => (field & flag) > (value & flag))
				.Select(flag => this.TryGetNameByFlag(flag, out string? name) ? name : null)
				.Where(name => !string.IsNullOrEmpty(name))
				.ToList()
				.ForEach(name => this.AppliedEdgeNames.Remove(name!));

			// Add newly selected edges
			Enumerable.Range(0, 32)
				.Select(i => ((uint) 1) << i)
				.Where(flag => (value & flag) > (field & flag))
				.Select(flag => this.TryGetNameByFlag(flag, out string? name) ? name : null)
				.Where(name => !string.IsNullOrEmpty(name))
				.ToList()
				.ForEach(name => this.AppliedEdgeNames.Add(name!));
		}
	}
	/// <summary>
	/// This is a computed export, hidden in the editor but saved in the resource file. It is updated by the
	/// <see cref="OnEdges"/> property.
	/// </summary>
	[Export] public Godot.Collections.Array<string> AppliedEdgeNames = [];

	// -----------------------------------------------------------------------------------------------------------------
	// FIELDS
	// -----------------------------------------------------------------------------------------------------------------



	// -----------------------------------------------------------------------------------------------------------------
	// PROPERTIES
	// -----------------------------------------------------------------------------------------------------------------

	private List<(uint flag, string name)> EdgeFlagNames
		=> PlatformProfile.TryGetProfileForCornerSettings(this, out PlatformProfile? profile)
			? profile.EdgesSettings.Index()
				.Where(edge => edge.Item != null)
				.Select(edge =>
				{
					uint flag = ((uint) 1) << edge.Index;
					return (flag, edge.Item.Name);
				})
				.ToList()
			: [];

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
			property["usage"] = (long) PropertyUsageFlags.Editor; // Shown in the editor but not saved
			property["hint_string"] = this.EdgeFlagNames.Select(tuple => $"{tuple.name}:{tuple.flag}")
				.Aggregate((a, b) => $"{a},{b}");
		}
		else if (property["name"].AsString() == nameof(this.AppliedEdgeNames))
		{
			property["usage"] = (long) PropertyUsageFlags.NoEditor; // Hidden in editor but saved
		}
	}

	// -----------------------------------------------------------------------------------------------------------------
	// METHODS
	// -----------------------------------------------------------------------------------------------------------------

	private bool TryGetFlagByName(string name, out uint flag)
	{
		flag = this.EdgeFlagNames.FirstOrDefault(en => en.name == name).flag;
		return flag != 0;
	}

	private bool TryGetNameByFlag(uint flag, [NotNullWhen(true)] out string? name)
	{
		name = this.EdgeFlagNames.FirstOrDefault(en => en.flag == flag).name;
		return !string.IsNullOrEmpty(name);
	}
}
