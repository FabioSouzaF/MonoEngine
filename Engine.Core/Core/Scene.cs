using Engine.Core.Entities;
using Engine.Core.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Engine.Core;

public class Scene
{
    public string Name { get; set; } = "Untitled Scene";
    public List<GameObject> RootObjects { get; set; } = new List<GameObject>();

    public Camera ActiveCamera;

    private List<GameObject> _pendingAdd = new List<GameObject>();
    private List<GameObject> _pendingRemove = new List<GameObject>();

    // --- FUNCIONALIDADES DE BUSCA ---

    public GameObject FindGameObjectWithComponent<T>() where T : Component
    {
        foreach (var rootObj in RootObjects)
        {
            var found = FindRecursive<T>(rootObj);
            if (found != null) return found;
        }
        return null;
    }

    public List<GameObject> FindGameObjectsWithComponent<T>() where T : Component
    {
        var results = new List<GameObject>();
        foreach (var rootObj in RootObjects)
        {
            FindRecursiveList<T>(rootObj, results);
        }
        return results;
    }

    private GameObject FindRecursive<T>(GameObject current) where T : Component
    {
        if (current.GetComponent<T>() != null) return current;

        foreach (var child in current.Children)
        {
            var found = FindRecursive<T>(child);
            if (found != null) return found;
        }

        return null;
    }

    private void FindRecursiveList<T>(GameObject current, List<GameObject> results) where T : Component
    {
        if (current.GetComponent<T>() != null) results.Add(current);

        foreach (var child in current.Children)
        {
            FindRecursiveList<T>(child, results);
        }
    }

    // ------------------------------------

    public void AddGameObject(GameObject go)
    {
        PropagateSceneReference(go);
        _pendingAdd.Add(go);
        UpdateActiveCamera(go);
    }

    public void RemoveGameObject(GameObject go)
    {
        _pendingRemove.Add(go);
    }

    public void Update(GameTime gameTime)
    {
        ProcessPendingObjects();

        for (int i = 0; i < RootObjects.Count; i++)
        {
            var obj = RootObjects[i];
            obj.Update(gameTime);
        }
        
        if (_pendingRemove.Count > 0) ProcessPendingObjects();
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        // Alteração sutil: Deixamos o RenderManager cuidar do Draw!
        // No futuro, se houver UI da cena, chamaremos aqui.
        foreach (var go in RootObjects)
        {
            go.Draw(spriteBatch);
        }
    }

    private void ProcessPendingObjects()
    {
        if (_pendingAdd.Count > 0)
        {
            RootObjects.AddRange(_pendingAdd);
            _pendingAdd.Clear();
        }

        if (_pendingRemove.Count > 0)
        {
            foreach (var go in _pendingRemove) RootObjects.Remove(go);
            _pendingRemove.Clear();
        }
    }
    
    private void PropagateSceneReference(GameObject go)
    {
        if (ActiveCamera == null)
        {
             var cam = go.GetComponent<Camera>();
             if (cam != null) ActiveCamera = cam;
        }

        foreach(var child in go.Children)
        {
            PropagateSceneReference(child);
        }
    }
    
    private void UpdateActiveCamera(GameObject go)
    {
        if (ActiveCamera == null)
        {
            ActiveCamera = go.GetComponent<Camera>();
        }
    }

    // --- NOVO: Religando a Cena após o Load do JSON ---
    public void OnAfterDeserialize()
    {
        // Procura a Câmera principal de volta na hierarquia
        var camObj = FindGameObjectWithComponent<Camera>();
        if (camObj != null)
        {
            ActiveCamera = camObj.GetComponent<Camera>();
        }
    }
}