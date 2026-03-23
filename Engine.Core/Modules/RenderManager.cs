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
        // Nota: Futuramente, podemos cachear isso na Scene para não buscar via Reflection todo frame
        var renderers = scene.FindGameObjectsWithComponent<Renderer>()
                             .Select(go => go.GetComponent<Renderer>())
                             .Where(r => r != null && r.Enabled && r.GameObject.IsActive)
                             .ToList();

        // Separa os componentes 2D (SpriteRenderer) e ordena pela camada (Z-Index)
        var sprites = renderers.OfType<SpriteRenderer>()
                               .OrderBy(s => s.OrderInLayer)
                               .ToList();

        // ---------------------------------------------------------
        // INICIA O BATCH COM A CÂMERA APLICADA
        // O SamplerState.PointClamp garante que pixel art não fique borrada. 
        // Se seu jogo for HD, pode mudar para LinearClamp.
        // ---------------------------------------------------------
        spriteBatch.Begin(
            sortMode: SpriteSortMode.Deferred, 
            blendState: BlendState.AlphaBlend, 
            samplerState: SamplerState.PointClamp, 
            depthStencilState: DepthStencilState.None, 
            rasterizerState: RasterizerState.CullNone, 
            effect: null, 
            transformMatrix: viewMatrix); // <-- A Mágica da Câmera acontece aqui!

        // Desenha todos os sprites já ordenados
        foreach (var sprite in sprites)
        {
            sprite.Draw(spriteBatch);
        }

        spriteBatch.End();
        
        // (Futuro: Aqui faremos o loop para MeshRenderer 3D usando GraphicsDevice diretamente)
    }
}