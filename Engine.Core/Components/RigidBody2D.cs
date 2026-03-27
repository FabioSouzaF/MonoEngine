using Microsoft.Xna.Framework;

namespace Engine.Core.Components
{
    public class RigidBody2D : Component
    {
        // A velocidade atual do objeto nos eixos X e Y
        public Vector2 Velocity = Vector2.Zero;
        
        // O peso do objeto (usado para empurrões)
        public float Mass = 1f;
        
        // Multiplicador da gravidade (0 = flutua, 1 = cai normal, 5 = cai como uma bigorna)
        public float GravityScale = 1f;
        
        // Se for Kinematic, a física ignora ele (ele não cai, mas empurra os outros)
        // Perfeito para plataformas móveis ou o chão!
        public bool IsKinematic = false;

        public void AddForce(Vector2 force)
        {
            if (!IsKinematic && Mass > 0)
            {
                Velocity += force / Mass;
            }
        }
    }
}