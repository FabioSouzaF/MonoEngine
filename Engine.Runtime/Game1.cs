using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Engine.Core;
using Engine.Core.Modules;
using Engine.Core.Serialization;
using System.IO;
using System.Linq;
using System;
using System.Reflection; // Importante para o Assembly

namespace Engine.Runtime
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private ProjectConfig _config;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // ==========================================================
            // 1. O INJETOR DE SCRIPTS (Antes de qualquer coisa existir!)
            // ==========================================================
            string userDllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UserScripts.dll");
            if (File.Exists(userDllPath))
            {
                try
                {
                    var a = Assembly.LoadFrom(userDllPath);
                    Console.WriteLine("[RUNTIME] UserScripts.dll carregada nativamente com sucesso!");
    
                    // O Raio-X: Vamos imprimir o nome de cada classe que existe na DLL!
                    foreach(var tipo in a.GetTypes())
                    {
                        Console.WriteLine($"[RAIO-X DLL] Achei a classe: {tipo.Name}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[RUNTIME ERRO] Falha ao injetar scripts: {ex.Message}");
                }
            }

            // ==========================================================
            // 2. LER AS CONFIGURAÇÕES DO PROJETO
            // ==========================================================
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string configFile = Directory.GetFiles(baseDir, "*.monoengine").FirstOrDefault();

            if (configFile != null)
            {
                _config = ProjectConfig.Load(configFile);
                Window.Title = _config.ProjectName;
                
                _graphics.PreferredBackBufferWidth = _config.WindowWidth;
                _graphics.PreferredBackBufferHeight = _config.WindowHeight;
                _graphics.ApplyChanges();
            }
            else
            {
                _config = new ProjectConfig();
                Window.Title = "Erro: Projeto Não Encontrado";
            }

            Screen.Initialize(_graphics);
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            Engine.Core.Assets.AssetManager.Initialize(GraphicsDevice);

            // ==========================================================
            // 3. CARREGAR A CENA (Com os scripts já na memória!)
            // ==========================================================
            string scenePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _config.InitialScene);
            
            // TROCAMOS AQUI: De LoadFromFile para LoadEncrypted!
            var scene = SceneSerializer.LoadEncrypted(scenePath);
            
            if (scene != null)
            {
                SceneManager.LoadScene(scene);
            }
            else
            {
                Console.WriteLine($"[ERRO] Não foi possível carregar a cena em: {scenePath}");
                SceneManager.LoadScene(new Engine.Core.Scene { Name = "Cena Vazia de Fallback" });
            }
        }

        protected override void Update(GameTime gameTime)
        {
            Time.Update(gameTime);
            Input.Update();
            SceneManager.Update(gameTime);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            RenderManager.Draw(_spriteBatch);
            base.Draw(gameTime);
        }
    }
}