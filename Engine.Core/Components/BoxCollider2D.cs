using Engine.Core.Entities;
using Microsoft.Xna.Framework;

namespace Engine.Core.Components
{
    public class BoxCollider2D : Component
    {
        public Vector2 Size = new Vector2(50, 50);
        public Vector2 Offset = Vector2.Zero;
        public bool IsTrigger = false;

        // Flag secreta: Garante que a auto-configuração rode apenas UMA vez!
        private bool _isInitialized = false;

        [Newtonsoft.Json.JsonIgnore]
        public BoundingBox Bounds
        {
            get
            {
                // 1. O "SNAP" INICIAL (Ao adicionar o componente)
                if (!_isInitialized && GameObject != null)
                {
                    var sprite = GameObject.GetComponent<SpriteRenderer>();
                    
                    // Verifica se existe um sprite e se a textura dele já foi calculada
                    if (sprite != null && sprite.Bounds.Max.X > sprite.Bounds.Min.X)
                    {
                        // A. Calcula o tamanho real (removendo a escala para descobrirmos a base real em pixels)
                        Vector3 spriteSize = sprite.Bounds.Max - sprite.Bounds.Min;
                        Vector3 currentScale = Transform != null ? Transform.LocalScale : Vector3.One;
                        
                        Size = new Vector2(
                            spriteSize.X / (currentScale.X != 0 ? currentScale.X : 1), 
                            spriteSize.Y / (currentScale.Y != 0 ? currentScale.Y : 1)
                        );

                        // B. O Truque Mágico do Alinhamento (Offset):
                        // Calcula o deslocamento correto baseado na Origem do seu Sprite.
                        // Se a origem for (0.5, 0.5), o Offset fica zero (centro perfeito).
                        // Se for (0,0), ele empurra o colisor para a direita e para baixo para encaixar na imagem!
                        Offset = new Vector2(
                            (0.5f - sprite.Origin.X) * Size.X,
                            (0.5f - sprite.Origin.Y) * Size.Y
                        );
                    }
                    
                    // Marca como inicializado para nunca mais mexer nos seus números sem permissão!
                    _isInitialized = true;
                }

                // 2. O CÁLCULO DA CAIXA (Agora com Suporte a Escala!)
                Vector3 pos = Transform != null ? Transform.Position : Vector3.Zero;
                Vector3 scale = Transform != null ? Transform.LocalScale : Vector3.One;

                Vector3 center = new Vector3(pos.X + Offset.X, pos.Y + Offset.Y, pos.Z);
                
                // Multiplicamos o tamanho pela escala do Transform.
                // Se você dobrar o tamanho do objeto na cena, o colisor dobra junto automaticamente!
                Vector3 extents = new Vector3((Size.X * scale.X) / 2f, (Size.Y * scale.Y) / 2f, 1f); 

                return new BoundingBox(center - extents, center + extents);
            }
        }
    }
}