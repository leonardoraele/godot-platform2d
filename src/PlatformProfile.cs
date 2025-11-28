using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Godot;

namespace Raele.Platform2D;

[Tool][GlobalClass]
public partial class PlatformProfile : Resource
{
	// -----------------------------------------------------------------------------------------------------------------
	// STATICS
	// -----------------------------------------------------------------------------------------------------------------

	private static Dictionary<ulong, WeakReference<PlatformProfile>> AllProfiles = new();

	public static bool TryGetProfileForCornerSettings(CornerSpriteSettings settings, [NotNullWhen(true)] out PlatformProfile? result)
	{
		foreach (WeakReference<PlatformProfile> wref in AllProfiles.Values)
		{
			if (wref.TryGetTarget(out PlatformProfile? profile) && profile.EdgeTypes.Any(edge => edge.CornerSprites.Contains(settings)))
			{
				result = profile;
				return true;
			}
		}
		result = null;
		return false;
	}

	public PlatformProfile() => AllProfiles.Add(this.GetInstanceId(), new WeakReference<PlatformProfile>(this));
	~PlatformProfile() => AllProfiles.Remove(this.GetInstanceId());

	// -----------------------------------------------------------------------------------------------------------------
	// EXPORTS
	// -----------------------------------------------------------------------------------------------------------------

	[ExportGroup("Fill Sprites", "Fill")]
	[Export(PropertyHint.GroupEnable)] public bool FillEnabled = false;
	[Export] public Texture2D? FillTexture;
	[Export(PropertyHint.None, "suffix:px")] public Vector2 FillOffset = Vector2.Zero;
	[Export(PropertyHint.Link)] public Vector2 FillScale = Vector2.One;
	[Export(PropertyHint.Range, "0,2,suffix:Radian Pi")] public float FillRotation = 0.0f;

	[ExportGroup("Edge Sprites")]
	[Export] public Godot.Collections.Array<EdgeSettings> EdgeTypes = [];
	[Export] public Godot.Collections.Array<EdgeIntersectionSpriteSettings> EdgeIntersectionCorners = [];

	// -----------------------------------------------------------------------------------------------------------------
	// PROPERTIES
	// -----------------------------------------------------------------------------------------------------------------

	public CheckSumHelper CheckSum => field ??= new CheckSumHelper(() => Utils.HashF(
		this.FillEnabled,
		Variant.From(this.FillTexture),
		this.FillOffset,
		this.FillScale,
		this.FillRotation,
		this.EdgeTypes
	));

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
