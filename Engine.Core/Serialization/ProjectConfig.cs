using Newtonsoft.Json;
using System.IO;

namespace Engine.Core.Serialization
{
    public class ProjectConfig
    {
        public string ProjectName { get; set; } = "Novo Projeto";
        public int WindowWidth { get; set; } = 1280;
        public int WindowHeight { get; set; } = 720;
        public string InitialScene { get; set; } = "Scenes/CenaInicial.scene";
        // public string InitialScene { get; set; } = "SistemaSolar.scene";
        
        // Métodos auxiliares para ler e guardar
        public static ProjectConfig Load(string filePath)
        {
            if (!File.Exists(filePath)) return new ProjectConfig();
            string json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<ProjectConfig>(json);
        }

        public void Save(string filePath)
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }
    }
}