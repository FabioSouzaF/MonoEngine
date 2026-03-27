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
                if (ImGui.InputText("Nome", ref name, 100)) 
                {
                    obj.Name = name;
                    EditorState.IsDirty = true;
                }
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
                                EditorState.IsDirty = true;
                            }
                            ImGui.PopStyleColor();
                            ImGui.Separator();
                        }
                        
                        var fields = component.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
                        foreach (var field in fields)
                        {
                            // Ignora Quaternions brutos para não poluir o Inspetor
                            if (field.FieldType == typeof(Microsoft.Xna.Framework.Quaternion)) continue;
                            
                            var value = field.GetValue(component);
                            var newValue = DrawFieldUI(field.Name, value, field.FieldType);
                            if (newValue != null && !newValue.Equals(value)) 
                            {
                                field.SetValue(component, newValue);
                                EditorState.IsDirty = true;
                            }
                        }

                        var properties = component.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                        foreach (var prop in properties)
                        {
                            
                            if (!prop.CanWrite || !prop.CanRead || prop.Name == "GameObject" || prop.Name == "Transform" || prop.PropertyType == typeof(Microsoft.Xna.Framework.Quaternion)) continue;
                            var value = prop.GetValue(component);
                            var newValue = DrawFieldUI(prop.Name, value, prop.PropertyType);
                            if (newValue != null && !newValue.Equals(value)) 
                            {
                                prop.SetValue(component, newValue);
                                EditorState.IsDirty = true;
                            }
                        }
                    }
                }

                // Processa a remoção de forma segura fora do loop
                if (_componentToRemove != null)
                {
                    obj.Components.Remove(_componentToRemove);
                    EditorState.IsDirty = true;
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
                                EditorState.IsDirty = true;
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
        
        private static unsafe object DrawFieldUI(string name, object value, Type targetType = null)
        {
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
                        string ext = System.IO.Path.GetExtension(EditorState.DraggedFilePath).ToLower();
                        bool isValid = false;
                        string lowerName = name.ToLower();

                        if (lowerName.Contains("texture") || lowerName.Contains("sprite") || lowerName.Contains("image"))
                            isValid = (ext == ".png" || ext == ".jpg" || ext == ".tga");
                        else if (lowerName.Contains("sound") || lowerName.Contains("audio") || lowerName.Contains("music"))
                            isValid = (ext == ".wav" || ext == ".mp3" || lowerName.Contains(".ogg"));
                        else
                            isValid = (ext != ".scene" && ext != ".cs" && ext != ".monoengine");

                        if (isValid)
                        {
                            tempString = System.IO.Path.GetFileName(EditorState.DraggedFilePath);
                            EditorState.DraggedFilePath = ""; 
                            EditorState.IsDirty = true;
                        }
                        else
                        {
                            Console.WriteLine($"[AVISO] Bloqueado: O campo '{name}' não aceita o formato '{ext}'");
                        }
                    }
                    ImGui.EndDragDropTarget();
                }
                return tempString; 
            }
            
            if (value is bool b) { ImGui.Checkbox(name, ref b); return b; }
            
            if (value is Microsoft.Xna.Framework.Vector2 v2)
            {
                var sysV2 = new System.Numerics.Vector2(v2.X, v2.Y);
                if (ImGui.DragFloat2(name, ref sysV2, 0.1f)) return new Microsoft.Xna.Framework.Vector2(sysV2.X, sysV2.Y);
                return v2;
            }
            
            if (value is Microsoft.Xna.Framework.Vector3 v3)
            {
                var sysV3 = new System.Numerics.Vector3(v3.X, v3.Y, v3.Z);
                if (ImGui.DragFloat3(name, ref sysV3, 0.1f)) return new Microsoft.Xna.Framework.Vector3(sysV3.X, sysV3.Y, sysV3.Z);
                return v3;
            }
            
            if (value is Microsoft.Xna.Framework.Quaternion q)
            {
                var eulerRads = ToEulerAngles(q);
                var eulerDegrees = new System.Numerics.Vector3(
                    Microsoft.Xna.Framework.MathHelper.ToDegrees(eulerRads.X),
                    Microsoft.Xna.Framework.MathHelper.ToDegrees(eulerRads.Y),
                    Microsoft.Xna.Framework.MathHelper.ToDegrees(eulerRads.Z)
                );

                if (ImGui.DragFloat3($"{name} (Graus)", ref eulerDegrees, 1f)) 
                {
                    float radX = Microsoft.Xna.Framework.MathHelper.ToRadians(eulerDegrees.X);
                    float radY = Microsoft.Xna.Framework.MathHelper.ToRadians(eulerDegrees.Y);
                    float radZ = Microsoft.Xna.Framework.MathHelper.ToRadians(eulerDegrees.Z);
                    return Microsoft.Xna.Framework.Quaternion.CreateFromYawPitchRoll(radY, radX, radZ);
                }
                return q;
            }
            
            if (value is Microsoft.Xna.Framework.Color c)
            {
                var sysColor = new System.Numerics.Vector4(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);
                if (ImGui.ColorEdit4(name, ref sysColor)) return new Microsoft.Xna.Framework.Color(sysColor.X, sysColor.Y, sysColor.Z, sysColor.W);
                return c;
            }

            // --- NOVIDADE: O RENDERIZADOR UNIVERSAL DE LISTAS (CORRIGIDO) ---
            // CORREÇÃO 1: Usamos o caminho completo para pegar a interface não-genérica
            if (value is System.Collections.IList list)
            {
                Type elementType = targetType != null && targetType.IsGenericType ? targetType.GetGenericArguments()[0] : null;
                if (elementType == null) return value; 

                if (ImGui.TreeNode($"{name} ({list.Count} itens)"))
                {
                    int itemToRemove = -1;

                    // CORREÇÃO 2: Trocamos 'i' por 'listIndex' para evitar conflito de escopo
                    for (int listIndex = 0; listIndex < list.Count; listIndex++)
                    {
                        object item = list[listIndex];
                        if (item == null) continue;

                        string nodeName = $"[{listIndex}] {elementType.Name}";
                        
                        // CORREÇÃO 3: Separamos a busca de Field e Property para o compilador não reclamar dos tipos diferentes
                        var fieldInfo = item.GetType().GetField("Name");
                        var propInfo = item.GetType().GetProperty("Name");

                        if (fieldInfo != null)
                            nodeName = $"[{listIndex}] " + (fieldInfo.GetValue(item)?.ToString() ?? elementType.Name);
                        else if (propInfo != null)
                            nodeName = $"[{listIndex}] " + (propInfo.GetValue(item)?.ToString() ?? elementType.Name);
                        if (ImGui.TreeNode($"{nodeName}###FixedId_{list.GetHashCode()}_{listIndex}"))
                        {
                            var itemFields = item.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
                            foreach (var field in itemFields)
                            {
                                var fValue = field.GetValue(item);
                                var fNewValue = DrawFieldUI(field.Name, fValue, field.FieldType); 
                                if (fNewValue != null && !fNewValue.Equals(fValue))
                                {
                                    field.SetValue(item, fNewValue);
                                    EditorState.IsDirty = true;
                                }
                            }

                            ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0.6f, 0.2f, 0.2f, 1f));
                            if (ImGui.Button("Remover Item", new System.Numerics.Vector2(-1, 24))) itemToRemove = listIndex;
                            ImGui.PopStyleColor();

                            ImGui.TreePop();
                        }
                    }

                    if (itemToRemove >= 0)
                    {
                        list.RemoveAt(itemToRemove);
                        EditorState.IsDirty = true;
                    }

                    ImGui.Separator();
                    
                    if (ImGui.Button($"Adicionar {elementType.Name}", new System.Numerics.Vector2(-1, 24)))
                    {
                        list.Add(Activator.CreateInstance(elementType));
                        EditorState.IsDirty = true;
                    }

                    ImGui.TreePop();
                }
                return list;
            }
            
            ImGui.TextDisabled($"{name}: {value}");
            return value;
        }
        
        // --- FUNÇÃO AUXILIAR: Transforma a matemática de máquina em ângulos humanos ---
        private static Microsoft.Xna.Framework.Vector3 ToEulerAngles(Microsoft.Xna.Framework.Quaternion q)
        {
            Microsoft.Xna.Framework.Vector3 angles = new Microsoft.Xna.Framework.Vector3();

            // Rotação no eixo X (Pitch)
            double sinr_cosp = 2 * (q.W * q.X + q.Y * q.Z);
            double cosr_cosp = 1 - 2 * (q.X * q.X + q.Y * q.Y);
            angles.X = (float)Math.Atan2(sinr_cosp, cosr_cosp);

            // Rotação no eixo Y (Yaw)
            double sinp = 2 * (q.W * q.Y - q.Z * q.X);
            if (Math.Abs(sinp) >= 1)
                angles.Y = (float)Math.CopySign(Math.PI / 2, sinp); // 90 graus se sair do limite
            else
                angles.Y = (float)Math.Asin(sinp);

            // Rotação no eixo Z (Roll) - O mais usado em jogos 2D!
            double siny_cosp = 2 * (q.W * q.Z + q.X * q.Y);
            double cosy_cosp = 1 - 2 * (q.Y * q.Y + q.Z * q.Z);
            angles.Z = (float)Math.Atan2(siny_cosp, cosy_cosp);

            return angles;
        }
        
    }
}