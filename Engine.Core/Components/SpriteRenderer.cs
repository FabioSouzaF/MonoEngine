using Engine.Core.Assets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.Core.Components;

public class SpriteRenderer : Renderer
{
    // public string TexturePath { get; set; }
    public string TexturePath = "";
    public Vector2 Origin { get; set; } = new Vector2(0.5f, 0.5f);

    private Texture2D _texture;
    
    public Color Color = Color.White;
        
    // O campo que vai receber o nome do arquivo!
    

    public override void Start()
    {
        base.Start();
        if (!string.IsNullOrEmpty(TexturePath))
        {
            _texture = AssetManager.LoadTexture(TexturePath);
            UpdateBounds();
        }
    }

    public override void Update(GameTime gameTime)
    {
        // Atualiza a BoundingBox (Caixa de Colisão Visual) a cada frame
        // Isso é o que o Frustum Culling vai usar no futuro para otimização
        if (_texture != null)
        {
            UpdateBounds();
        }
    }

    private void UpdateBounds()
    {
        if (_texture == null) return;

        var pos = Transform.Position;
        var scale = Transform.LocalScale;
        
        float width = _texture.Width * scale.X;
        float height = _texture.Height * scale.Y;

        // Cria uma caixa 3D baseada no tamanho 2D do sprite
        Vector3 min = new Vector3(pos.X - (width * Origin.X), pos.Y - (height * Origin.Y), pos.Z);
        Vector3 max = new Vector3(min.X + (width * (1 - Origin.X)), min.Y + (height * (1 - Origin.Y)), pos.Z);

        Bounds = new BoundingBox(min, max);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (_texture == null && !string.IsNullOrEmpty(TexturePath))
        {
            _texture = AssetManager.LoadTexture(TexturePath);
        }

        if (_texture != null)
        {
            // Lemos os valores DIRETAMENTE do Transform, evitando o "Bug da Matriz Zero"
            var position2D = new Vector2(Transform.Position.X, Transform.Position.Y);
            var scale2D = new Vector2(Transform.LocalScale.X, Transform.LocalScale.Y);
            var originPixels = new Vector2(_texture.Width * Origin.X, _texture.Height * Origin.Y);

            // Calcula a rotação diretamente do Quaternion
            var direction = Vector3.Transform(Vector3.UnitX, Transform.LocalRotation);
            var rotationZ = (float)System.Math.Atan2(direction.Y, direction.X);

            spriteBatch.Draw(
                _texture,
                position2D,
                null,
                Color,
                rotationZ,
                originPixels,
                scale2D, // Agora usa o valor real (1, 1) que está no Inspetor!
                SpriteEffects.None,
                0f
            );
        }
    }
}