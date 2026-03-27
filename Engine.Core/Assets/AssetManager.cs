using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System; // Necessário para o Console e AppDomain
using Microsoft.Xna.Framework.Graphics;

namespace Engine.Core.Assets
{
    public static class AssetManager
    {
        private static GraphicsDevice _graphicsDevice;
        private static Dictionary<string, Texture2D> _textures = new Dictionary<string, Texture2D>();
        
        // --- A MÁGICA AQUI ---
        // Agora é público! O Editor pode mudar isso a qualquer momento.
        public static string ProjectRootPath { get; set; } = AppDomain.CurrentDomain.BaseDirectory;
        
        private static ZipArchive _pakArchive;

        // O Initialize volta a ser simples, apenas para a placa de vídeo e para o Runtime (.pak)
        public static void Initialize(GraphicsDevice graphicsDevice)
        {
            _graphicsDevice = graphicsDevice;
            
            // Se estivermos no jogo final, abrimos o arquivo .pak!
            string pakPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets.pak");
            if (File.Exists(pakPath))
            {
                _pakArchive = ZipFile.OpenRead(pakPath);
            }
        }

        public static Texture2D LoadTexture(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (_textures.ContainsKey(path)) return _textures[path];

            Texture2D texture = null;

            if (_pakArchive != null)
            {
                string entryName = path.Replace("Assets/", "").Replace("Assets\\", "");
                var entry = _pakArchive.GetEntry(entryName);
                if (entry != null)
                {
                    using (var stream = entry.Open())
                    {
                        texture = Texture2D.FromStream(_graphicsDevice, stream);
                    }
                }
            }
            else
            {
                string fileName = Path.GetFileName(path);
                
                // Agora ele SEMPRE usa o ProjectRootPath atualizado!
                string fullPath = Path.Combine(ProjectRootPath, "Assets", fileName);
                
                if (File.Exists(fullPath))
                {
                    using (var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read))
                    {
                        texture = Texture2D.FromStream(_graphicsDevice, stream);
                    }
                }
                else if (File.Exists(path)) 
                {
                    using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                    {
                        texture = Texture2D.FromStream(_graphicsDevice, stream);
                    }
                }
                else
                {
                    Console.WriteLine($"[AVISO] Textura não encontrada no disco: {fullPath}");
                }
            }

            if (texture != null)
            {
                _textures[path] = texture;
                return texture;
            }

            return null;
        }
    }
}