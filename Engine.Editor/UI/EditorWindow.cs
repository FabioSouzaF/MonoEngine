using ImGuiNET;

namespace Engine.Editor.UI
{
    public abstract class EditorWindow
    {
        public string Name { get; protected set; }
        public bool IsOpen { get; set; } = true;

        // Cada janela sabe desenhar-se a si mesma
        public abstract void Draw();
    }
}