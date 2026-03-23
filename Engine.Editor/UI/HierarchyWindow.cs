using Engine.Core;
using Engine.Core.Entities;
using ImGuiNET;
using System;
using System.Linq;
using Engine.Core.Modules;

namespace Engine.Editor.UI
{
    public class HierarchyWindow : EditorWindow
    {
        public HierarchyWindow()
        {
            Name = "Hierarquia";
        }

        public override unsafe void Draw()
        {
            ImGui.Begin("Hierarquia");
            var scene = SceneManager.ActiveScene;
            if (scene != null)
            {
                // 1. ÁREA PARA SOLTAR NA RAIZ (Desvincular de um pai)
                ImGui.Dummy(new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, 10)); // Espacinho no topo
                if (ImGui.BeginDragDropTarget())
                {
                    var payload = ImGui.AcceptDragDropPayload("GAMEOBJECT");
                
                    // Mudamos para null aqui também!
                    if (payload.NativePtr != null && EditorState.DraggedObject != null)
                    {
                        var dragged = EditorState.DraggedObject;
                        if (dragged.Parent != null)
                        {
                            dragged.Parent.Children.Remove(dragged);
                            dragged.Parent = null;
                            scene.AddGameObject(dragged); // Volta a ser raiz
                        }
                        EditorState.DraggedObject = null;
                    }
                    ImGui.EndDragDropTarget();
                }

                foreach (var rootObj in scene.RootObjects.ToList()) 
                {
                    DrawNodeRecursive(scene, rootObj);
                }

                if (ImGui.BeginPopupContextWindow("HierarchyContext"))
                {
                    if (ImGui.MenuItem("Criar Objeto Vazio"))
                        scene.AddGameObject(new GameObject { Name = "Novo Objeto" });

                    if (EditorState.SelectedObject != null)
                    {
                        ImGui.Separator();
                        if (ImGui.MenuItem($"Deletar '{EditorState.SelectedObject.Name}'"))
                        {
                            if (EditorState.SelectedObject.Parent != null)
                                EditorState.SelectedObject.Parent.Children.Remove(EditorState.SelectedObject);
                            else
                                scene.RemoveGameObject(EditorState.SelectedObject);
                            
                            EditorState.SelectedObject = null;
                        }
                    }
                    ImGui.EndPopup();
                }
            }
            ImGui.End();
        }
        
        private static unsafe void DrawNodeRecursive(Scene scene, GameObject obj)
        {
            ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick | ImGuiTreeNodeFlags.DefaultOpen;
            if (obj.Children.Count == 0) flags |= ImGuiTreeNodeFlags.Leaf;
            if (EditorState.SelectedObject == obj) flags |= ImGuiTreeNodeFlags.Selected;

            bool nodeIsOpen = ImGui.TreeNodeEx(obj.Name, flags);
            if (ImGui.IsItemClicked()) EditorState.SelectedObject = obj;

            // --- INÍCIO DO ARRASTO (SOURCE) ---
            if (ImGui.BeginDragDropSource())
            {
                EditorState.DraggedObject = obj;
                ImGui.SetDragDropPayload("GAMEOBJECT", IntPtr.Zero, 0); // O ImGui exige um ID de payload
                ImGui.Text($"Movendo {obj.Name}"); // O texto fantasma que aparece ao arrastar
                ImGui.EndDragDropSource();
            }

            // --- FIM DO ARRASTO (TARGET / RECEBEDOR) ---
            if (ImGui.BeginDragDropTarget())
            {
                var payload = ImGui.AcceptDragDropPayload("GAMEOBJECT");
                if (payload.NativePtr != null && EditorState.DraggedObject != null)
                {
                    var dragged = EditorState.DraggedObject;
                    
                    // Proteção: Não deixa arrastar para si mesmo, nem arrastar um Pai para dentro de um Filho
                    if (dragged != obj && !IsDescendant(obj, dragged))
                    {
                        // Remove do pai antigo (ou da raiz)
                        if (dragged.Parent != null) dragged.Parent.Children.Remove(dragged);
                        else scene.RemoveGameObject(dragged);

                        // Adiciona ao novo pai
                        obj.AddChild(dragged);
                    }
                    EditorState.DraggedObject = null; // Limpa a mochila
                }
                ImGui.EndDragDropTarget();
            }

            if (nodeIsOpen)
            {
                foreach (var child in obj.Children.ToList()) DrawNodeRecursive(scene, child);
                ImGui.TreePop();
            }
        }

        // Função recursiva para impedir que o "Avo" seja colocado dentro do "Neto"
        private static bool IsDescendant(GameObject potentialDescendant, GameObject potentialAncestor)
        {
            if (potentialDescendant == null) return false;
            if (potentialDescendant.Parent == potentialAncestor) return true;
            return IsDescendant(potentialDescendant.Parent, potentialAncestor);
        }
    }
}