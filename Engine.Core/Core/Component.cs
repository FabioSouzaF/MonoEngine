using Engine.Core.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace Engine.Core.Components;

public abstract class Component
{
    [JsonIgnore]
    public GameObject GameObject { get; private set; }
    
    [JsonIgnore]
    public Transform Transform => GameObject?.Transform;
    
    public bool Enabled { get; set; } = true;

    public void Attach(GameObject owner)
    {
        GameObject = owner;
        OnAttached();
    }

    public virtual void OnAttached() { }
    
    public virtual void Start(){ }
    
    public virtual void Update(GameTime gameTime) { }
    
    public virtual void Draw(SpriteBatch spriteBatch) { }
}