using Engine.Core.Entities;

namespace Engine.Editor
{
    public enum EditorPlayMode { Edit, Play }

    public static class EditorState
    {
        
        // --- GESTÃO DE PROJETO ---
        public static string CurrentProjectPath { get; set; } = "";
        
        // Guarda os scripts customizados do utilizador carregados na memória do Editor
        public static System.Reflection.Assembly UserAssembly { get; set; } = null;
        
        // Retorna verdadeiro se já escolhemos um projeto
        public static bool IsProjectLoaded => !string.IsNullOrEmpty(CurrentProjectPath);
        
        // Guarda globalmente qual objeto o utilizador clicou
        public static GameObject SelectedObject { get; set; } = null;
        public static GameObject DraggedObject { get; set; } = null;
        
        public static string DraggedFilePath { get; set; } = "";
        public static string CurrentScenePath { get; set; } = "";
        
        // Estado do botão Play/Stop
        public static EditorPlayMode PlayMode { get; set; } = EditorPlayMode.Edit;
        public static bool IsPlaying { get; set; } = false;
        
        // A "fotografia" da cena para restaurar quando fizermos Stop
        public static string SceneBackupJson { get; set; } = "";
    }
}