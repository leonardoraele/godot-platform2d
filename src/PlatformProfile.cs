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

	private static Dictionary<ulong, WeakReference<PlatformProfile>> GlobalInstanceRepository = new();

	public static bool TryGetProfileForCornerSettings(CornerSpriteSettings settings, [NotNullWhen(true)] out PlatformProfile? result)
	{
		foreach (WeakReference<PlatformProfile> wref in GlobalInstanceRepository.Values)
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

	public PlatformProfile() => GlobalInstanceRepository.Add(this.GetInstanceId(), new WeakReference<PlatformProfile>(this));
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
	[Export] public Godot.Collections.Array<EdgeSettings> EdgeTypes
		{ get => field; set { field = value; this.ObserveArray(field); this.EmitChanged(); } } = [];
	[Export] public Godot.Collections.Array<EdgeIntersectionSpriteSettings> EdgeIntersectionCorners
		{ get => field; set { field = value; this.ObserveArray(field); this.EmitChanged(); } } = [];

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

	private void ObserveArray<[MustBeVariant] T>(Godot.Collections.Array<T> array)
	{
		foreach (var item in array)
		{
			if (item is Resource resource)
			{
				bool signalAlreadyConnected = resource.GetSignalConnectionList(PlatformProfile.SignalName.Changed)
					.Select(signal => signal["callable"].AsCallable())
					.Any(callable =>
					{
						return callable.Target == this && callable.Method == nameof(this.EmitChanged)
							|| callable.Delegate.Target == this && callable.Delegate.Method.Name == nameof(this.EmitChanged);
					});
				if (!signalAlreadyConnected)
				{
					resource.Changed += () =>
					{
						this.EmitChanged();
					};
				}
			}
		}
	}
}
