using Microsoft.Xna.Framework;

namespace Engine.Core;

public static class Time
{
    /// <summary>
    /// Tempo em segundos desde o último frame (ex: 0.016 para 60fps).
    /// Use isso para multiplicar movimentos: position += velocity * Time.DeltaTime;
    /// </summary>
    public static float DeltaTime { get; private set; }

    /// <summary>
    /// Tempo total desde o início do jogo.
    /// </summary>
    public static float TotalTime { get; private set; }

    public static void Update(GameTime gameTime)
    {
        DeltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        TotalTime = (float)gameTime.TotalGameTime.TotalSeconds;
    }
}