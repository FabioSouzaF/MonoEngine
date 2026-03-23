using Microsoft.Xna.Framework;

namespace Engine.Core.Modules;

public static class Screen
{
    internal static GraphicsDeviceManager Graphics { get; private set; }

    public static int Width => Graphics.PreferredBackBufferWidth;
    public static int Height => Graphics.PreferredBackBufferHeight;

    public static void Initialize(GraphicsDeviceManager graphics)
    {
        Graphics = graphics;
    }

    public static void SetResolution(int width, int height, bool fullscreen)
    {
        Graphics.PreferredBackBufferWidth = width;
        Graphics.PreferredBackBufferHeight = height;
        Graphics.IsFullScreen = fullscreen;
        Graphics.ApplyChanges();
    }
}