using Engine.Core.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;

namespace Engine.Core.Modules;

public static class RenderManager
{
    public static void Draw(SpriteBatch spriteBatch)
    {
        var scene = SceneManager.ActiveScene;
        if (scene == null) return;

        var camera = scene.ActiveCamera;
        
        // Se houver uma câmera, pegamos a Matriz dela. Se não, usamos a Matriz Identidade (padrão)
        Matrix viewMatrix = camera != null ? camera.GetViewMatrix() : Matrix.Identity;

        // Busca todos os renderers ativos na cena
        var renderers = scene.FindGameObjectsWithComponent<Renderer>()
                             .Select(go => go.GetComponent<Renderer>())
                             .Where(r => r != null && r.Enabled && r.GameObject.IsActive)
                             .ToList();

        // --- A GRANDE SACADA ---
        // Pega tanto os Sprites quanto os Tilemaps e cria uma fila única!
        var renderers2D = renderers.Where(r => r is SpriteRenderer || r is Tilemap)
                                   .OrderBy(r => 
                                   {
                                       // Descobre de quem é o OrderInLayer para organizar o Z-Index
                                       if (r is SpriteRenderer s) return s.OrderInLayer;
                                       if (r is Tilemap t) return t.OrderInLayer;
                                       return 0f; 
                                   })
                                   .ToList();

        // ---------------------------------------------------------
        // INICIA O BATCH COM A CÂMERA APLICADA
        // ---------------------------------------------------------
        spriteBatch.Begin(
            sortMode: SpriteSortMode.Deferred, 
            blendState: BlendState.AlphaBlend, 
            samplerState: SamplerState.PointClamp, 
            depthStencilState: DepthStencilState.None, 
            rasterizerState: RasterizerState.CullNone, 
            effect: null, 
            transformMatrix: viewMatrix); 

        // Agora desenhamos TODO MUNDO da lista (O método Draw vem da classe base Component!)
        foreach (var renderer in renderers2D)
        {
            renderer.Draw(spriteBatch);
        }

        spriteBatch.End();
        
        GizmoRenderer.DrawColliders(spriteBatch, SceneManager.ActiveScene, viewMatrix);
    }
}