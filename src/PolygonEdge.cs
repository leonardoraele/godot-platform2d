using Godot;

namespace Raele.Platform2D;

public record PolygonEdge(Vector2 Left, Vector2 Right)
{
	public Vector2 Normal => (this.Right - this.Left).Normalized().Orthogonal(); // Rotates counter-clockwise 90 degrees
	public Vector2 Center => this.Left.Lerp(this.Right, 0.5f);
	// public float Rotation => this.Normal.Angle();
}
