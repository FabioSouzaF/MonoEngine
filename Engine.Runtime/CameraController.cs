using Engine.Core;
using Engine.Core.Components;
using Engine.Core.Modules;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Engine.Runtime
{
    // Perceba como ele herda de Component, igual ao Unity!
    public class CameraController : Component 
    {
        public float Speed = 200f;
        public float ZoomSpeed = 1f;

        public override void Update(GameTime gameTime)
        {
            var cam = GameObject.GetComponent<Camera>();
            if (cam == null) return;

            // Usando as nossas abstrações limpas de Input e Time da Engine.Core!
            if (Input.GetKey(Keys.Left)) Transform.Translate(new Vector3(-Speed * Time.DeltaTime, 0, 0));
            if (Input.GetKey(Keys.Right)) Transform.Translate(new Vector3(Speed * Time.DeltaTime, 0, 0));
            if (Input.GetKey(Keys.Up)) Transform.Translate(new Vector3(0, -Speed * Time.DeltaTime, 0));
            if (Input.GetKey(Keys.Down)) Transform.Translate(new Vector3(0, Speed * Time.DeltaTime, 0));

            if (Input.GetKey(Keys.Z)) cam.Zoom += ZoomSpeed * Time.DeltaTime;
            if (Input.GetKey(Keys.X)) cam.Zoom -= ZoomSpeed * Time.DeltaTime;
        }
    }
}