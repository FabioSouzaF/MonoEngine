using Engine.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.Core.Modules;

public static class SceneManager
{
    public static Scene ActiveScene { get; private set; }

    public static void LoadScene(Scene scene)
    {
        // Futuramente: Chamar OnDestroy() em todos os objetos da ActiveScene antes de trocar
        ActiveScene = scene;
    }

    public static void Update(GameTime gameTime)
    {
        ActiveScene?.Update(gameTime);
    }

    internal static void Draw(SpriteBatch spriteBatch)
    {
        ActiveScene?.Draw(spriteBatch);
    }
}