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
        var transform = Transform;
        
        // Agora a câmera lê a resolução real e atualizada diretamente do nosso Gerenciador de Tela!
        float width = Screen.Width > 0 ? Screen.Width : 1280;
        float height = Screen.Height > 0 ? Screen.Height : 720;

        // O centro da tela
        var screenCenter = new Vector3(width * Origin.X, height * Origin.Y, 0);

        // A posição da câmera no mundo
        var pos = transform.Position;
        
        return Matrix.CreateTranslation(-pos) *
               Matrix.CreateRotationZ(-transform.LocalRotation.Z) * Matrix.CreateScale(new Vector3(Zoom, Zoom, 1)) *
               Matrix.CreateTranslation(screenCenter);
    }

    public Vector2 ScreenToWorld(Vector2 screenPosition)
    {
        return Vector2.Transform(screenPosition, Matrix.Invert(GetViewMatrix()));
    }
}