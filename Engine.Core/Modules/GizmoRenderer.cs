using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Engine.Core.Components;
using Engine.Core.Entities;

namespace Engine.Core.Modules
{
    public static class GizmoRenderer
    {
        private static Texture2D _pixel;

        public static void Initialize(GraphicsDevice graphicsDevice)
        {
            _pixel = new Texture2D(graphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });
        }

        public static void DrawColliders(SpriteBatch spriteBatch, Scene scene, Matrix viewMatrix)
        {
            if (_pixel == null || scene == null) return;

            var boxColliders = new List<BoxCollider2D>();
            var tileColliders = new List<TilemapCollider2D>(); // Nova lista para os cenários!

            foreach (var root in scene.RootObjects) 
            {
                GatherBoxColliders(root, boxColliders);
                GatherTileColliders(root, tileColliders);
            }

            if (boxColliders.Count == 0 && tileColliders.Count == 0) return;

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, viewMatrix);

            // 1. Desenha os BoxColliders normais (Player, Inimigos, Triggers)
            foreach (var col in boxColliders)
            {
                if (!col.Enabled || !col.GameObject.IsActive) continue;
                Color color = col.IsTrigger ? Color.Yellow : Color.LimeGreen;
                DrawThickBox(spriteBatch, col.Bounds, color, 3);
            }

            // 2. Desenha o Tilemap (Cenário)
            foreach (var tCol in tileColliders)
            {
                if (!tCol.Enabled || !tCol.GameObject.IsActive) continue;
                
                Color color = Color.Cyan; 
                
                // Agora que os retângulos são BoundingBox, passamos direto pra função!
                foreach (var bb in tCol.CollisionRects)
                {
                    DrawThickBox(spriteBatch, bb, color, 2); 
                }
            }
            spriteBatch.End();
        }

        private static void DrawThickBox(SpriteBatch spriteBatch, BoundingBox bb, Color color, int thickness)
        {
            int x = (int)bb.Min.X;
            int y = (int)bb.Min.Y;
            int width = (int)(bb.Max.X - bb.Min.X);
            int height = (int)(bb.Max.Y - bb.Min.Y);

            spriteBatch.Draw(_pixel, new Rectangle(x, y, width, thickness), color);
            spriteBatch.Draw(_pixel, new Rectangle(x, y + height - thickness, width, thickness), color);
            spriteBatch.Draw(_pixel, new Rectangle(x, y, thickness, height), color);
            spriteBatch.Draw(_pixel, new Rectangle(x + width - thickness, y, thickness, height), color);
        }

        private static void GatherBoxColliders(GameObject obj, List<BoxCollider2D> colliders)
        {
            var col = obj.GetComponent<BoxCollider2D>();
            if (col != null) colliders.Add(col);
            foreach (var child in obj.Children) GatherBoxColliders(child, colliders);
        }

        // Função para caçar os colisores de cenário na hierarquia
        private static void GatherTileColliders(GameObject obj, List<TilemapCollider2D> colliders)
        {
            var col = obj.GetComponent<TilemapCollider2D>();
            if (col != null) colliders.Add(col);
            foreach (var child in obj.Children) GatherTileColliders(child, colliders);
        }
    }
}