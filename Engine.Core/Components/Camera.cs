using Engine.Core.Entities;
using Engine.Core.Modules; // Adicionamos isto para ter acesso ao Screen!
using Microsoft.Xna.Framework;

namespace Engine.Core.Components;

public class Camera : Component
{
    public float Zoom { get; set; } = 1.0f;
    
    // Origem da câmera (0.5, 0.5 = foca o objeto exatamente no centro da tela)
    public Vector2 Origin { get; set; } = new Vector2(0.5f, 0.5f);
    
    public override void Start()
    {
        base.Start();
        // Se a cena ainda não tem uma câmera principal, eu sou a câmera principal!
        if (SceneManager.ActiveScene != null && SceneManager.ActiveScene.ActiveCamera == null)
        {
            SceneManager.ActiveScene.ActiveCamera = this;
        }
    }
    
    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        
        if (SceneManager.ActiveScene != null && SceneManager.ActiveScene.ActiveCamera == null)
        {
            SceneManager.ActiveScene.ActiveCamera = this;
        }
    }

    public Matrix GetViewMatrix()
    {
        // O Centro da Tela (para o Zoom ir para o meio e não para o canto)
        // No Editor, você pode pegar o tamanho do RenderTarget atual!
        float centerX = 1280 / 2f; 
        float centerY = 720 / 2f;  

        return 
            // 1. Move a câmera para a posição do Transform (Ignoramos o Z, usamos só X e Y)
            Matrix.CreateTranslation(new Vector3(-Transform.LocalPosition.X, -Transform.LocalPosition.Y, 0.0f)) *
                
            // 2. Rotaciona a câmera (Eixo Z)
            Matrix.CreateRotationZ(Microsoft.Xna.Framework.MathHelper.ToRadians(Transform.LocalEulerAngles.Z)) *
                
            // 3. Aplica o Zoom por Escala (O segredo do 2D!)
            Matrix.CreateScale(new Vector3(Zoom, Zoom, 1.0f)) *
                
            // 4. Centraliza a câmera na tela
            Matrix.CreateTranslation(new Vector3(centerX, centerY, 0.0f));
    }

    public Vector2 ScreenToWorld(Vector2 screenPosition)
    {
        return Vector2.Transform(screenPosition, Matrix.Invert(GetViewMatrix()));
    }
}