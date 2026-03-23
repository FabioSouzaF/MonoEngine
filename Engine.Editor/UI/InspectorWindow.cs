using System;
using System.Linq;
using System.Reflection;
using Engine.Core.Components;
using Engine.Core.Entities;
using ImGuiNET;

namespace Engine.Editor.UI
{
    public class InspectorWindow : EditorWindow
    {
        private string _searchString = "";
        private Component _componentToRemove = null;

        public InspectorWindow() { Name = "Inspetor"; }

        public override void Draw()
        {
            ImGui.Begin("Inspetor");
            if (EditorState.SelectedObject != null)
            {
                var obj = EditorState.SelectedObject;
                string name = obj.Name;
                if (ImGui.InputText("Nome", ref name, 100)) obj.Name = name;
                ImGui.TextDisabled($"ID: {obj.Id}");
                ImGui.Separator();

                _componentToRemove = null;

                // Usamos ToList() para não dar erro se removermos um componente durante o loop
                foreach (var component in obj.Components.ToList())
                {
                    // O Transform é a alma do objeto, não pode ser removido
                    bool isTransform = component is Transform;

                    bool isOpen = ImGui.CollapsingHeader(component.GetType().Name, ImGuiTreeNodeFlags.DefaultOpen);

                    // --- MENU DE CONTEXTO (Botão Direito no Cabeçalho) ---
                    if (!isTransform && ImGui.BeginPopupContextItem($"Context_{component.GetHashCode()}"))
                    {
                        if (ImGui.MenuItem("Remover Componente"))
                        {
                            _componentToRemove = component;
                        }
                        ImGui.EndPopup();
                    }

                    if (isOpen)
                    {
                        
                        if (isTransform)
                        {
                            ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0.3f, 0.3f, 0.3f, 1f));
                            if (ImGui.Button("Resetar Transform", new System.Numerics.Vector2(-1, 24)))
                            {
                                var t = (Transform)component;
                                t.LocalPosition = Microsoft.Xna.Framework.Vector3.Zero;
                                t.LocalScale = Microsoft.Xna.Framework.Vector3.One;
                                t.LocalRotation = Microsoft.Xna.Framework.Quaternion.Identity;
                            }
                            ImGui.PopStyleColor();
                            ImGui.Separator();
                        }
                        
                        var fields = component.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
                        foreach (var field in fields)
                        {
                            var value = field.GetValue(component);
                            var newValue = DrawFieldUI(field.Name, value);
                            if (newValue != null && !newValue.Equals(value)) field.SetValue(component, newValue);
                        }

                        var properties = component.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                        foreach (var prop in properties)
                        {
                            if (!prop.CanWrite || !prop.CanRead || prop.Name == "GameObject" || prop.Name == "Transform") continue;
                            var value = prop.GetValue(component);
                            var newValue = DrawFieldUI(prop.Name, value);
                            if (newValue != null && !newValue.Equals(value)) prop.SetValue(component, newValue);
                        }
                    }
                }

                // Processa a remoção de forma segura fora do loop
                if (_componentToRemove != null)
                {
                    obj.Components.Remove(_componentToRemove);
                }
                
                ImGui.Separator();
                
                // --- BOTÃO DE ADICIONAR COMPONENTE ---
                if (ImGui.Button("Adicionar Componente", new System.Numerics.Vector2(-1, 30)))
                {
                    ImGui.OpenPopup("AddComponentMenu");
                    _searchString = ""; // Limpa a pesquisa ao abrir
                }

                if (ImGui.BeginPopup("AddComponentMenu"))
                {
                    ImGui.InputText("Pesquisar", ref _searchString, 100);
                    ImGui.Separator();

                    // MÁGICA: Encontra todas as classes do Core que herdam de Component
                    // var componentTypes = Assembly.GetAssembly(typeof(Component)).GetTypes()
                    //     .Where(t => t.IsSubclassOf(typeof(Component)) && !t.IsAbstract);
                    
                    // MÁGICA: Pega nos componentes nativos da Engine
                    var componentTypes = Assembly.GetAssembly(typeof(Component)).GetTypes()
                        .Where(t => t.IsSubclassOf(typeof(Component)) && !t.IsAbstract).ToList(); // Transformamos numa Lista editável

                    // MÁGICA 2: Pega nos componentes criados pelo utilizador!
                    if (EditorState.UserAssembly != null)
                    {
                        var userTypes = EditorState.UserAssembly.GetTypes()
                            .Where(t => t.IsSubclassOf(typeof(Component)) && !t.IsAbstract);
                        componentTypes.AddRange(userTypes);
                    }

                    foreach (var type in componentTypes)
                    {
                        // Pula o Transform, pois o objeto já nasce com um
                        if (type == typeof(Transform)) continue;

                        // Filtro de Pesquisa (ignora maiúsculas/minúsculas)
                        if (!string.IsNullOrEmpty(_searchString) && !type.Name.Contains(_searchString, StringComparison.OrdinalIgnoreCase))
                            continue;

                        if (ImGui.MenuItem(type.Name))
                        {
                            // Verifica se o objeto já tem este componente (evita duplicados)
                            if (!obj.Components.Any(c => c.GetType() == type))
                            {
                                // Cria a instância do script e anexa ao objeto!
                                var newComponent = (Component)Activator.CreateInstance(type);
                                obj.AddComponent(newComponent);
                            }
                        }
                    }

                    ImGui.EndPopup();
                }
            }
            else
            {
                ImGui.Text("Selecione um objeto na Hierarquia.");
            }
            
            ImGui.End();
        }
        
        // ... (O SEU MÉTODO DrawFieldUI COM O UNSAFE CONTINUA EXATAMENTE IGUAL AQUI EM BAIXO!) ...
        private static unsafe object DrawFieldUI(string name, object value)
        {
            // Cole aqui o seu código do DrawFieldUI que fizemos no passo anterior!
            if (value == null) return null;
            if (value is float f) { ImGui.DragFloat(name, ref f, 0.1f); return f; }
            if (value is int i) { ImGui.DragInt(name, ref i); return i; }
            
            if (value is string s) 
            { 
                string tempString = s ?? ""; 
                ImGui.InputText(name, ref tempString, 256); 

                if (ImGui.BeginDragDropTarget())
                {
                    var payload = ImGui.AcceptDragDropPayload("CONTENT_FILE");
                    if (payload.NativePtr != null && !string.IsNullOrEmpty(EditorState.DraggedFilePath))
                    {
                        tempString = System.IO.Path.GetFileName(EditorState.DraggedFilePath);
                        EditorState.DraggedFilePath = ""; 
                    }
                    ImGui.EndDragDropTarget();
                }
                return tempString; 
            }
            
            if (value is bool b) { ImGui.Checkbox(name, ref b); return b; }
            if (value is Microsoft.Xna.Framework.Vector3 v3)
            {
                var sysV3 = new System.Numerics.Vector3(v3.X, v3.Y, v3.Z);
                if (ImGui.DragFloat3(name, ref sysV3, 0.1f)) return new Microsoft.Xna.Framework.Vector3(sysV3.X, sysV3.Y, sysV3.Z);
                return v3;
            }
            if (value is Microsoft.Xna.Framework.Color c)
            {
                var sysColor = new System.Numerics.Vector4(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);
                if (ImGui.ColorEdit4(name, ref sysColor)) return new Microsoft.Xna.Framework.Color(sysColor.X, sysColor.Y, sysColor.Z, sysColor.W);
                return c;
            }
            ImGui.TextDisabled($"{name}: {value}");
            return value;
        }
    }
}