using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.Core.Assets
{
    public static class AssetManager
    {
        private static GraphicsDevice _graphicsDevice;
        private static Dictionary<string, Texture2D> _textures = new Dictionary<string, Texture2D>();
        
        // Mantém o nosso "Cofre" aberto na memória
        private static ZipArchive _pakArchive;

        public static void Initialize(GraphicsDevice graphicsDevice)
        {
            _graphicsDevice = graphicsDevice;
            
            // Se estivermos no jogo final, abrimos o arquivo .pak!
            string pakPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Assets.pak");
            if (File.Exists(pakPath))
            {
                _pakArchive = ZipFile.OpenRead(pakPath);
            }
        }

        public static Texture2D LoadTexture(string path)
        {
            if (_textures.ContainsKey(path)) return _textures[path];

            Texture2D texture = null;

            if (_pakArchive != null)
            {
                // MODO RUNTIME: Lendo de dentro do cofre .pak
                // Limpa o "Assets/" da string para procurar apenas o nome do arquivo dentro do zip
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
                // MODO EDITOR: Lendo da pasta normal de desenvolvimento
                if (File.Exists(path))
                {
                    using (var stream = new FileStream(path, FileMode.Open))
                    {
                        texture = Texture2D.FromStream(_graphicsDevice, stream);
                    }
                }
            }

            if (texture != null)
            {
                _textures[path] = texture;
                return texture;
            }

            System.Console.WriteLine($"[AVISO] Textura não encontrada: {path}");
            return null;
        }
    }
}