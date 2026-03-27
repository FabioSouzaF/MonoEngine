using ImGuiNET;
using Engine.Core;
using Engine.Core.Modules;
using Engine.Core.Serialization;

namespace Engine.Editor.UI
{
    public class ToolbarWindow : EditorWindow
    {
        public ToolbarWindow() { Name = "Toolbar"; }

        public override void Draw()
        {
            ImGui.Begin("Toolbar", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoScrollWithMouse);
            
            if (EditorState.PlayMode == EditorPlayMode.Edit)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0.2f, 0.8f, 0.2f, 1f)); 
                if (ImGui.Button("▶ PLAY", new System.Numerics.Vector2(100, 30)))
                {
                    EditorState.SceneSnapshot = SceneSerializer.Serialize(SceneManager.ActiveScene);
                    EditorState.PlayMode = EditorPlayMode.Play;
                    EditorState.IsPlaying = true;
                    EditorState.SelectedObject = null; 
                    
                    Physics2DManager.Enabled = true;
                }
                ImGui.PopStyleColor();
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0.8f, 0.2f, 0.2f, 1f)); 
                if (ImGui.Button("■ STOP", new System.Numerics.Vector2(100, 30)))
                {
                    var restoredScene = SceneSerializer.Deserialize(EditorState.SceneSnapshot);
                    if (restoredScene != null) SceneManager.LoadScene(restoredScene);
                    
                    EditorState.SelectedObject = null;
                    EditorState.PlayMode = EditorPlayMode.Edit;
                    EditorState.IsPlaying = false;
                    
                    Physics2DManager.Enabled = false;
                }
                ImGui.PopStyleColor();
            }
            ImGui.End();
        }
    }
}