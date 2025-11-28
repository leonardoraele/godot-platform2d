using Godot;

namespace Raele.Platform2D;

[Tool]
public abstract partial class SpriteSettings : Resource
{
	// -----------------------------------------------------------------------------------------------------------------
	// STATICS
	// -----------------------------------------------------------------------------------------------------------------

	// public static readonly string MyConstant = "";

	// -----------------------------------------------------------------------------------------------------------------
	// EXPORTS
	// -----------------------------------------------------------------------------------------------------------------

	// TODO Make this field an array and a boolean to determine whether multiple overlaping sprites with each texture
	// should be created, or only a single sprite with a random sample of this list of textures.
	[Export] public Texture2D? Texture
		{ get => field; set { field = value; this.EmitChanged(); } } = null;

	[ExportGroup("Texture Options")]
	[Export(PropertyHint.None, "suffix:px")] public Vector2 Offset
		{ get => field; set { field = value; this.EmitChanged(); } } = Vector2.Zero;
	[Export(PropertyHint.Link)] public Vector2 Scale
		{ get => field; set { field = value; this.EmitChanged(); } } = Vector2.One;
	[Export(PropertyHint.Range, "0,2,suffix:π")] public float Rotation
		{ get => field; set { field = value; this.EmitChanged(); } } = 0f;
	[Export] public bool FlipH
		{ get => field; set { field = value; this.EmitChanged(); } } = false;
	[Export] public bool FlipV
		{ get => field; set { field = value; this.EmitChanged(); } } = false;

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

	// private enum Type {
	// 	Value1,
	// }

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
	// 	=> base._PhysicsProcess(delta);

	// -----------------------------------------------------------------------------------------------------------------
	// METHODS
	// -----------------------------------------------------------------------------------------------------------------

	public virtual void Apply(Sprite2D sprite)
	{
		sprite.Texture = this.Texture;
		sprite.Centered = true;
		sprite.Offset = this.Offset;
		sprite.Scale = this.Scale;
		sprite.Rotation = this.Rotation;
		sprite.FlipH = this.FlipH;
		sprite.FlipV = this.FlipV;
	}
}
