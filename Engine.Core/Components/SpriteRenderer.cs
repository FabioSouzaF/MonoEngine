using Engine.Core.Assets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.Core.Components;

public class SpriteRenderer : Renderer
{
    public string TexturePath = "";
    
    // O nosso "recortador" de imagens para o Animator!
    public Rectangle? SourceRectangle { get; set; } = null;
    
    public Vector2 Origin { get; set; } = new Vector2(0.5f, 0.5f);

    private Texture2D _texture;
    
    public Color Color = Color.White;
    public bool FlipX = false;
        
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
        
        // --- NOVIDADE: A caixa de colisão respeita o recorte! ---
        float texWidth = SourceRectangle.HasValue ? SourceRectangle.Value.Width : _texture.Width;
        float texHeight = SourceRectangle.HasValue ? SourceRectangle.Value.Height : _texture.Height;

        float width = texWidth * scale.X;
        float height = texHeight * scale.Y;

        Vector3 min = new Vector3(pos.X - (width * Origin.X), pos.Y - (height * Origin.Y), pos.Z);
        Vector3 max = new Vector3(pos.X + (width * (1 - Origin.X)), pos.Y + (height * (1 - Origin.Y)), pos.Z);

        Bounds = new BoundingBox(min, max);
    }

    public override void Draw(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch)
    {
        if (_texture == null || !Enabled) return;

        float rotX = Microsoft.Xna.Framework.MathHelper.ToRadians(Transform.LocalEulerAngles.X);
        float rotY = Microsoft.Xna.Framework.MathHelper.ToRadians(Transform.LocalEulerAngles.Y);
        float rotZ = Microsoft.Xna.Framework.MathHelper.ToRadians(Transform.LocalEulerAngles.Z);

        float finalScaleX = Transform.LocalScale.X * (float)System.Math.Cos(rotY); 
        float finalScaleY = Transform.LocalScale.Y * (float)System.Math.Cos(rotX); 

        Microsoft.Xna.Framework.Vector2 finalScale = new Microsoft.Xna.Framework.Vector2(finalScaleX, finalScaleY);
        Microsoft.Xna.Framework.Vector2 drawPosition = new Microsoft.Xna.Framework.Vector2(Transform.LocalPosition.X, Transform.LocalPosition.Y);

        // --- NOVIDADE: O centro da imagem respeita o recorte! ---
        float texWidth = SourceRectangle.HasValue ? SourceRectangle.Value.Width : _texture.Width;
        float texHeight = SourceRectangle.HasValue ? SourceRectangle.Value.Height : _texture.Height;

        Microsoft.Xna.Framework.Vector2 pixelOrigin = new Microsoft.Xna.Framework.Vector2(
            texWidth * Origin.X, 
            texHeight * Origin.Y
        );
        
        var effect = FlipX ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        
        spriteBatch.Draw(
            _texture,
            drawPosition,
            SourceRectangle, // <-- A MÁGICA: Passamos o recorte para a placa de vídeo!
            Color,
            rotZ,           
            pixelOrigin,    
            finalScale,     
            effect,
            OrderInLayer    
        );
    }
    
    // --- NOVIDADE: O Animator vai chamar isso para trocar o arquivo da imagem ---
    public void SetTexture(string newTexturePath)
    {
        if (TexturePath == newTexturePath) return; // Não carrega de novo se já for a mesma!
        
        TexturePath = newTexturePath;
        if (!string.IsNullOrEmpty(TexturePath))
        {
            _texture = Engine.Core.Assets.AssetManager.LoadTexture(TexturePath);
            UpdateBounds();
        }
    }
    
}