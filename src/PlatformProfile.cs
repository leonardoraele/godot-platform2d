using Godot;

namespace Raele.Platform2D;

[Tool][GlobalClass]
public partial class PlatformProfile : Resource
{
	[ExportGroup("Override Polygon2D Texture Settings", "Fill")]
	[Export(PropertyHint.GroupEnable)] public bool FillEnabled = false;
	[Export] public Texture2D? FillTexture;
	[Export(PropertyHint.None, "suffix:px")] public Vector2 FillOffset = Vector2.Zero;
	[Export(PropertyHint.Link)] public Vector2 FillScale = Vector2.One;
	[Export(PropertyHint.Range, "0,2,suffix:Radian Pi")] public float FillRotation = 0.0f;

	[ExportGroup("Edges")]
	[Export] public Godot.Collections.Array<EdgeSettings> EdgesSettings = [];

	public CheckSumHelper CheckSum => field ??= new CheckSumHelper(() => Utils.HashF(
		this.FillEnabled,
		Variant.From(this.FillTexture),
		this.FillOffset,
		this.FillScale,
		this.FillRotation,
		this.EdgesSettings
	));

	public void ConfigureTexture(Polygon2D polygon)
	{
		if (!this.FillEnabled)
		{
			return;
		}
		polygon.Texture = this.FillTexture;
		polygon.TextureOffset = this.FillOffset;
		polygon.TextureScale = Vector2.One / this.FillScale;
		polygon.TextureRotation = this.FillRotation * Mathf.Pi;
		polygon.TextureRepeat = CanvasItem.TextureRepeatEnum.Enabled;
	}
}
