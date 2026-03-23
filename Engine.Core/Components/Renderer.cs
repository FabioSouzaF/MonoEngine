using Microsoft.Xna.Framework;

namespace Engine.Core.Components;

public abstract class Renderer : Component
{
    public Color Color { get; set; } = Color.White;
    public int OrderInLayer { get; set; } = 0;
    
    // Essencial para o Frustum Culling no futuro
    public BoundingBox Bounds { get; protected set; } 
}