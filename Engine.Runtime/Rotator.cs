using Engine.Core;
using Engine.Core.Components;
using Microsoft.Xna.Framework;

namespace Engine.Runtime
{
    public class Rotator : Component
    {
        public float Speed = 1f;

        public override void Update(GameTime gameTime)
        {
            // Roda o objeto no eixo Z (como um ponteiro de relógio)
            Transform.Rotate(Vector3.UnitZ, Speed * Time.DeltaTime);
        }
    }
}