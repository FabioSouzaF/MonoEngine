using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Engine.Core; 
using Engine.Core.Modules; 
using Engine.Core.Entities;
using Engine.Core.Components;
using Engine.Core.Assets;
using System.IO;
using System;

namespace Engine.Runtime
{
    public class Game1Solar : EngineApp 
    {
        public Game1Solar() : base() { }

        protected override void Initialize()
        {
            base.Initialize(); 
            Screen.SetResolution(1280, 720, false);
        }

        protected override void LoadContent()
        {
            base.LoadContent(); 
            AssetManager.Initialize(GraphicsDevice);

            string testTexturePath = Path.Combine(Directory.GetCurrentDirectory(), "test_square.png");
            CreateDebugTexture(testTexturePath); 

            var scene = new Scene() { Name = "Solar System Scene" };

            // 1. CÂMARA
            var camObj = new GameObject() { Name = "Main Camera" };
            var camera = new Camera();
            camera.Zoom = 0.8f; // Começa um pouco afastada
            camObj.AddComponent(camera);
            camObj.AddComponent(new CameraController() { Speed = 400f }); 
            camObj.Transform.LocalPosition = new Vector3(0, 0, 0);
            scene.AddGameObject(camObj);

            // 2. FUNDO (Campo de Estrelas)
            Random rng = new Random();
            for (int i = 0; i < 500; i++)
            {
                var star = new GameObject() { Name = $"Star_{i}" };
                star.Transform.LocalPosition = new Vector3(rng.Next(-2000, 2000), rng.Next(-1500, 1500), 0);
                
                // Estrelas de tamanhos diferentes
                float starSize = rng.Next(2, 6);
                star.Transform.LocalScale = new Vector3(starSize, starSize, 1);
                
                var starSprite = new SpriteRenderer { TexturePath = testTexturePath };
                // Faz as estrelas ficarem ligeiramente transparentes/cinzentas
                starSprite.Color = new Color(Color.White, rng.NextSingle() * 0.5f + 0.2f);
                starSprite.OrderInLayer = -10; // Desenha atrás de tudo
                star.AddComponent(starSprite);
                
                scene.AddGameObject(star);
            }

            // 3. O CENTRO DO SISTEMA SOLAR (Objeto Vazio, Escala 1x1)
            var solarSystemCenter = new GameObject() { Name = "Solar System Center" };
            scene.AddGameObject(solarSystemCenter);

            // VISUAL DO SOL (Filho do Centro, aqui sim usamos a escala 150)
            var sunVisual = new GameObject() { Name = "Sun Visual" };
            sunVisual.Transform.LocalScale = new Vector3(150, 150, 1);
            sunVisual.AddComponent(new SpriteRenderer { TexturePath = testTexturePath, Color = Color.Yellow });
            sunVisual.AddComponent(new Rotator { Speed = 0.5f }); // O Sol gira no próprio eixo
            solarSystemCenter.AddChild(sunVisual);

            // 4. PIVÔ DA TERRA E O CENTRO DA TERRA
            var earthPivot = new GameObject() { Name = "Earth Pivot" };
            earthPivot.AddComponent(new Rotator { Speed = 1.5f });
            solarSystemCenter.AddChild(earthPivot); 

            // Corpo Lógico da Terra: Controla a distância do Sol, mas a escala DEVE ser 1x1!
            var earthLogic = new GameObject() { Name = "Earth Logic" };
            earthLogic.Transform.LocalPosition = new Vector3(400, 0, 0); 
            earthPivot.AddChild(earthLogic);

            // Corpo Visual da Terra: Controla o Sprite e a Rotação da imagem, escala 50x50!
            var earthVisual = new GameObject() { Name = "Earth Visual" };
            earthVisual.Transform.LocalScale = new Vector3(50, 50, 1);
            earthVisual.AddComponent(new SpriteRenderer { TexturePath = testTexturePath, Color = Color.DeepSkyBlue });
            earthVisual.AddComponent(new Rotator { Speed = 3f });
            earthLogic.AddChild(earthVisual);

            // 5. PIVÔ DA LUA E A LUA
            var moonPivot = new GameObject() { Name = "Moon Pivot" };
            moonPivot.AddComponent(new Rotator { Speed = 4f });
            // Adicionamos o pivô ao Lógico (Escala 1x1), e não ao Visual (Escala 50x50)!
            earthLogic.AddChild(moonPivot); 

            var moonVisual = new GameObject() { Name = "Moon Visual" };
            moonVisual.Transform.LocalPosition = new Vector3(80, 0, 0); // Agora 80 x 1 = 80 pixels reais
            moonVisual.Transform.LocalScale = new Vector3(15, 15, 1); // Agora 15 x 1 = 15 pixels reais
            moonVisual.AddComponent(new SpriteRenderer { TexturePath = testTexturePath, Color = Color.LightGray });
            moonPivot.AddChild(moonVisual);

            // 6. MARTE
            var marsPivot = new GameObject() { Name = "Mars Pivot" };
            marsPivot.AddComponent(new Rotator { Speed = 0.8f });
            solarSystemCenter.AddChild(marsPivot);

            var mars = new GameObject() { Name = "Mars" };
            mars.Transform.LocalPosition = new Vector3(700, 0, 0);
            mars.Transform.LocalScale = new Vector3(40, 40, 1);
            mars.AddComponent(new SpriteRenderer { TexturePath = testTexturePath, Color = Color.OrangeRed });
            marsPivot.AddChild(mars);

            SceneManager.LoadScene(scene);
        }
        
        private void CreateDebugTexture(string path)
        {
            if (File.Exists(path)) return;
            Texture2D tex = new Texture2D(GraphicsDevice, 1, 1);
            tex.SetData(new Color[] { Color.White });
            using (var stream = File.Create(path)) tex.SaveAsPng(stream, 1, 1);
            tex.Dispose();
        }

        protected override void Update(GameTime gameTime)
        {
            if (Input.GetKeyDown(Keys.Escape)) Exit();

            if (Input.GetKeyDown(Keys.S))
            {
                Engine.Core.Serialization.SceneSerializer.SaveToFile(SceneManager.ActiveScene, "SistemaSolar.scene");
                System.Console.WriteLine("Universo Gravado com Sucesso!");
            }

            if (Input.GetKeyDown(Keys.L))
            {
                var cenaCarregada = Engine.Core.Serialization.SceneSerializer.LoadFromFile("SistemaSolar.scene");
                if (cenaCarregada != null)
                {
                    SceneManager.LoadScene(cenaCarregada);
                    Console.WriteLine("Universo Carregado com Sucesso!");
                }
            }

            base.Update(gameTime);
        }
    }
}