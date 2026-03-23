using Engine.Core.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine.Core.Entities;

public class GameObject
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "New Object";
    public bool IsActive { get; set; } = true;

    // O Transform é cacheado aqui, mas vive dentro da lista 'Components'
    [JsonIgnore]
    public Transform Transform { get; private set; }

    [JsonProperty(ItemTypeNameHandling = TypeNameHandling.Auto)]
    public List<Component> Components { get; private set; } = new List<Component>();

    // Hierarquia
    public List<GameObject> Children { get; private set; } = new List<GameObject>();

    [JsonIgnore]
    public GameObject Parent { get; set; }

    public GameObject()
    {
        // Garante que todo GO tenha um Transform ao nascer
        var t = new Transform();
        AddComponent(t); 
        // Nota: AddComponent já seta a propriedade Transform
    }

    public void AddComponent(Component component)
    {
        if (component is Transform t)
        {
            // Se já existe um transform, removemos o antigo da lista para não duplicar
            if (Transform != null) Components.Remove(Transform);
            Transform = t;
        }

        component.Attach(this);
        if (!Components.Contains(component))
        {
            Components.Add(component);
            
            component.Start();
        }
    }

    public T GetComponent<T>() where T : Component
    {
        return Components.OfType<T>().FirstOrDefault();
    }

    public void AddChild(GameObject child)
    {
        if (child.Parent != null)
        {
            child.Parent.Children.Remove(child);
        }

        child.Parent = this;
        Children.Add(child);
    }

    public void Update(GameTime gameTime)
    {
        if (!IsActive) return;

        for (int i = 0; i < Components.Count; i++)
        {
            if (Components[i].Enabled)
                Components[i].Update(gameTime);
        }

        foreach (var child in Children)
        {
            child.Update(gameTime);
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (!IsActive) return;

        for (int i = 0; i < Components.Count; i++)
        {
            if (Components[i].Enabled)
                Components[i].Draw(spriteBatch);
        }

        foreach (var child in Children)
        {
            child.Draw(spriteBatch);
        }
    }
}