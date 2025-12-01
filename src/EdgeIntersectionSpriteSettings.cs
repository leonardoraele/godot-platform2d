using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Godot;
using GodotDictionary = Godot.Collections.Dictionary;

namespace Raele.Platform2D;

[Tool][GlobalClass]
public partial class EdgeIntersectionSpriteSettings : CornerSpriteSettings
{
	// -----------------------------------------------------------------------------------------------------------------
	// STATICS
	// -----------------------------------------------------------------------------------------------------------------

	// public static readonly string MyConstant = "";

	// -----------------------------------------------------------------------------------------------------------------
	// EXPORTS
	// -----------------------------------------------------------------------------------------------------------------

	[ExportGroup("Applicable Edges")]
	[Export(PropertyHint.GroupEnable)] public bool ApplicableEdgesEnabled
		{ get => field; set { field = value; this.EmitChanged(); } } = false;
	/// <summary>
	/// This is an ethemeral export, displayed in the editor but not saved in the resource file. It is used as a
	/// bitfield to select which edges this corner sprite applies to. Changing this field updates the
	/// <see cref="ApplicableEdgeNames"/> field.
	/// </summary>
	[Export(PropertyHint.Flags)] public uint ApplicableEdges
	{
		get
		{
			return field = this.ApplicableEdgeNames.Select(name => this.TryGetFlagByName(name, out uint flag) ? flag : 0)
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
				.ForEach(name => this.ApplicableEdgeNames.Remove(name!)); // TODO Should assign a new AppliedEdgeNames instead of modifying the existing one

			// Add newly selected edges
			Enumerable.Range(0, 32)
				.Select(i => ((uint) 1) << i)
				.Where(flag => (value & flag) > (field & flag))
				.Select(flag => this.TryGetNameByFlag(flag, out string? name) ? name : null)
				.Where(name => !string.IsNullOrEmpty(name))
				.ToList()
				.ForEach(name => this.ApplicableEdgeNames.Add(name!)); // TODO Should assign a new AppliedEdgeNames instead of modifying the existing one
		}
	}
	[ExportToolButton("Select All")] public Callable SelectAllToolButton => Callable.From(this.SelectAllEdges);
	[ExportToolButton("Deselect All")] public Callable DeselectAllToolButton => Callable.From(this.DeselectAllEdges);
	/// <summary>
	/// This is a computed export, hidden in the editor but saved in the resource file. It is updated by the
	/// <see cref="ApplicableEdges"/> property.
	/// </summary>
	[Export] public Godot.Collections.Array<string> ApplicableEdgeNames
		{ get => field; set { field = value; this.EmitChanged(); } } = [];

	// -----------------------------------------------------------------------------------------------------------------
	// FIELDS
	// -----------------------------------------------------------------------------------------------------------------



	// -----------------------------------------------------------------------------------------------------------------
	// PROPERTIES
	// -----------------------------------------------------------------------------------------------------------------

	private List<(uint flag, string name)> EdgeFlagNames
		=> PlatformProfile.TryGetProfileForCornerSettings(this, out PlatformProfile? profile)
			? profile.EdgeTypes?.Index()
				.OfType<(int index, EdgeSettings settings)>()
				.Select(edge =>
				{
					uint flag = ((uint) 1) << edge.index;
					return (flag, edge.settings.Name);
				})
				.ToList()
				?? []
			: [];

	// -----------------------------------------------------------------------------------------------------------------
	// SIGNALS
	// -----------------------------------------------------------------------------------------------------------------

	// [Signal] public delegate void EventHandler()

	// -----------------------------------------------------------------------------------------------------------------
	// INTERNAL TYPES
	// -----------------------------------------------------------------------------------------------------------------

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
		if (property["name"].AsString() == nameof(this.ApplicableEdges))
		{
			property["usage"] = (long) PropertyUsageFlags.Editor; // Shown in the editor but not saved
			property["hint_string"] = this.EdgeFlagNames.Select(tuple => $"{tuple.name}:{tuple.flag}")
				.Aggregate("", (a, b) => $"{a},{b}");
		}
		else if (property["name"].AsString() == nameof(this.ApplicableEdgeNames))
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

	private void SelectAllEdges() => this.ApplicableEdgeNames = [.. this.EdgeFlagNames.Select(edge => edge.name)];
	private void DeselectAllEdges() => this.ApplicableEdgeNames = [];
}
