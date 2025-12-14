using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Raele.Platform2D;

[Tool][GlobalClass]
public partial class PlatformProfile : Resource
{
	// -----------------------------------------------------------------------------------------------------------------
	// STATICS
	// -----------------------------------------------------------------------------------------------------------------

	private static Dictionary<ulong, WeakReference<PlatformProfile>> GlobalInstanceRepository = new();
	public static IEnumerable<PlatformProfile> AllInstances => GlobalInstanceRepository.Values
		.Select(wref => wref.TryGetTarget(out PlatformProfile? profile) ? profile : null)
		.OfType<PlatformProfile>();

	public PlatformProfile() => GlobalInstanceRepository.Add(this.GetInstanceId(), new(this));
	~PlatformProfile() => GlobalInstanceRepository.Remove(this.GetInstanceId());

	// -----------------------------------------------------------------------------------------------------------------
	// EXPORTS
	// -----------------------------------------------------------------------------------------------------------------

	[ExportGroup("Fill Sprites", "Fill")]
	[Export(PropertyHint.GroupEnable)] public bool FillEnabled
		{ get => field; set { field = value; this.EmitChanged(); } } = false;
	[Export] public Texture2D? FillTexture
		{ get => field; set { field = value; this.EmitChanged(); } } = null;
	[Export(PropertyHint.None, "suffix:px")] public Vector2 FillOffset
		{ get => field; set { field = value; this.EmitChanged(); } } = Vector2.Zero;
	[Export(PropertyHint.Link)] public Vector2 FillScale
		{ get => field; set { field = value; this.EmitChanged(); } } = Vector2.One;
	[Export(PropertyHint.Range, "0,2,suffix:Radian Pi")] public float FillRotation
		{ get => field; set { field = value; this.EmitChanged(); } } = 0.0f;

	[ExportGroup("Edge Sprites")]
	[Export] public Godot.Collections.Array<EdgeSettings?>? EdgeTypes
		{ get => field; set { field = value; Utils.ObserveArrayExport(this, field); this.EmitChanged(); } } = [];
	[Export] public Godot.Collections.Array<EdgeIntersectionSpriteSettings?>? EdgeIntersectionCorners
		{ get => field; set { field = value; Utils.ObserveArrayExport(this, field); this.EmitChanged(); } } = [];

	// -----------------------------------------------------------------------------------------------------------------
	// PROPERTIES
	// -----------------------------------------------------------------------------------------------------------------

	// -----------------------------------------------------------------------------------------------------------------
	// METHODS
	// -----------------------------------------------------------------------------------------------------------------

	public void ConfigureTexture(Polygon2D polygon)
	{
		if (!this.FillEnabled)
		{
			polygon.Texture = null;
			return;
		}
		polygon.Texture = this.FillTexture;
		polygon.TextureOffset = this.FillOffset;
		polygon.TextureScale = Vector2.One / this.FillScale;
		polygon.TextureRotation = this.FillRotation * Mathf.Pi;
		polygon.TextureRepeat = CanvasItem.TextureRepeatEnum.Enabled;
	}
}
