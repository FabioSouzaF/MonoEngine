using System;
using System.IO;
using System.Numerics;
using Engine.Core.Modules;
using ImGuiNET;

namespace Engine.Editor.UI
{
    public class ContentBrowserWindow : EditorWindow
    {
        private string _baseDirectory = "";
        private string _currentDirectory = "";
        
        // --- Modais ---
        private string _itemToRename = null;
        private string _renameBuffer = "";
        private bool _openRenameModal = false;

        private string _itemToDelete = null;
        private bool _openDeleteModal = false;

        // --- Área de Transferência (Clipboard) ---
        private string _clipboardPath = "";
        private bool _isCutOperation = false;

        public ContentBrowserWindow()
        {
            Name = "Navegador de Assets";
        }

        public override unsafe void Draw()
        {
            string expectedAssetsPath = Path.Combine(EditorState.CurrentProjectPath);
            
            if (string.IsNullOrEmpty(_baseDirectory) || _baseDirectory != expectedAssetsPath)
            {
                _baseDirectory = expectedAssetsPath;
                if (!Directory.Exists(_baseDirectory)) Directory.CreateDirectory(_baseDirectory);
                _currentDirectory = _baseDirectory; 
            }

            ImGui.Begin(Name);

            // Mantivemos o botão voltar no topo por conveniência visual
            if (_currentDirectory != _baseDirectory)
            {
                if (ImGui.Button("<- Voltar"))
                {
                    var parentDir = Directory.GetParent(_currentDirectory);
                    if (parentDir != null) _currentDirectory = parentDir.FullName;
                }
                ImGui.SameLine();
            }

            string relativePath = _currentDirectory.Replace(_baseDirectory, "");
            ImGui.TextDisabled(string.IsNullOrEmpty(relativePath) ? "Raiz do Projeto" : relativePath);
            ImGui.Separator();

            // --- MENU DE CONTEXTO GERAL (Espaço Vazio) ---
            if (ImGui.BeginPopupContextWindow("ContentBrowserContext"))
            {
                if (ImGui.MenuItem("Criar Nova Pasta")) { CriarPastaSegura(); }
                if (ImGui.MenuItem("Criar Novo Script (C#)")) { CriarScriptSeguro(); }
                
                ImGui.Separator();
                
                if (ImGui.MenuItem("Criar Nova Cena")) { CriarCenaSegura(); }

                ImGui.Separator();
                
                // Opção de Colar só aparece se houver algo válido no Clipboard
                bool canPaste = !string.IsNullOrEmpty(_clipboardPath) && (File.Exists(_clipboardPath) || Directory.Exists(_clipboardPath));
                if (canPaste)
                {
                    if (ImGui.MenuItem($"Colar ({Path.GetFileName(_clipboardPath)})")) { ExecutarColar(); }
                }
                
                ImGui.EndPopup();
            }

            float padding = 16.0f;
            float thumbnailSize = 75.0f;
            float cellSize = thumbnailSize + padding;
            float panelWidth = ImGui.GetContentRegionAvail().X;
            int columnCount = (int)(panelWidth / cellSize);
            if (columnCount < 1) columnCount = 1;

            ImGui.Columns(columnCount, "ContentBrowserGrid", false);

            // --- 0. O BOTÃO ".." (DIRETÓRIO PAI PARA DRAG & DROP) ---
            if (_currentDirectory != _baseDirectory)
            {
                ImGui.PushID("PastaPai_DotDot");
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.8f, 0.7f, 0.2f, 0.5f)); // Um amarelo mais transparente
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.9f, 0.8f, 0.3f, 0.8f));

                if (ImGui.Button("..", new Vector2(thumbnailSize, thumbnailSize)))
                {
                    var parentDir = Directory.GetParent(_currentDirectory);
                    if (parentDir != null) _currentDirectory = parentDir.FullName;
                }

                // ALVO DO DRAG & DROP: Arrasta para o ".." para tirar da pasta atual!
                if (ImGui.BeginDragDropTarget())
                {
                    var payload = ImGui.AcceptDragDropPayload("CONTENT_FILE");
                    if (payload.NativePtr != null && !string.IsNullOrEmpty(EditorState.DraggedFilePath))
                    {
                        var parentDir = Directory.GetParent(_currentDirectory);
                        if (parentDir != null)
                        {
                            MoverItem(EditorState.DraggedFilePath, parentDir.FullName);
                        }
                    }
                    ImGui.EndDragDropTarget();
                }

                ImGui.PopStyleColor(2);
                ImGui.TextWrapped("Voltar");
                ImGui.NextColumn();
                ImGui.PopID();
            }

            // --- 1. DESENHAR AS PASTAS ---
            foreach (var dir in Directory.GetDirectories(_currentDirectory))
            {
                var dirName = new DirectoryInfo(dir).Name;
                ImGui.PushID(dirName);
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.8f, 0.7f, 0.2f, 1.0f)); 
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.9f, 0.8f, 0.3f, 1.0f));

                if (ImGui.Button(dirName, new Vector2(thumbnailSize, thumbnailSize)))
                {
                    _currentDirectory = dir;
                }

                // Drag & Drop Target
                if (ImGui.BeginDragDropTarget())
                {
                    var payload = ImGui.AcceptDragDropPayload("CONTENT_FILE");
                    if (payload.NativePtr != null && !string.IsNullOrEmpty(EditorState.DraggedFilePath))
                    {
                        MoverItem(EditorState.DraggedFilePath, dir);
                    }
                    ImGui.EndDragDropTarget();
                }

                DesenharMenuContextoItem(dir, dirName, true);

                if (ImGui.BeginDragDropSource())
                {
                    EditorState.DraggedFilePath = dir;
                    ImGui.SetDragDropPayload("CONTENT_FILE", IntPtr.Zero, 0);
                    ImGui.Text($"Movendo pasta {dirName}...");
                    ImGui.EndDragDropSource();
                }

                ImGui.PopStyleColor(2);
                ImGui.TextWrapped(dirName);
                ImGui.NextColumn();
                ImGui.PopID();
            }

            // --- 2. DESENHAR OS ARQUIVOS ---
            foreach (var file in Directory.GetFiles(_currentDirectory))
            {
                var fileName = Path.GetFileName(file);
                var extension = Path.GetExtension(file).ToLower();

                ImGui.PushID(fileName);
                
                Vector4 btnColor = new Vector4(0.2f, 0.2f, 0.2f, 1.0f);
                if (extension == ".scene") btnColor = new Vector4(0.2f, 0.5f, 0.2f, 1.0f);
                else if (extension == ".cs") btnColor = new Vector4(0.2f, 0.3f, 0.6f, 1.0f); 
                
                ImGui.PushStyleColor(ImGuiCol.Button, btnColor);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(btnColor.X + 0.1f, btnColor.Y + 0.1f, btnColor.Z + 0.1f, 1.0f));

                ImGui.Button(fileName, new Vector2(thumbnailSize, thumbnailSize));

                if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                {
                    if (extension == ".scene")
                    {
                        var loadedScene = Engine.Core.Serialization.SceneSerializer.LoadFromFile(file);
                        if (loadedScene != null)
                        {
                            SceneManager.LoadScene(loadedScene);
                            EditorState.SelectedObject = null;
                            EditorState.CurrentScenePath = file;
                        }
                    }
                }

                DesenharMenuContextoItem(file, fileName, false);

                if (ImGui.BeginDragDropSource())
                {
                    EditorState.DraggedFilePath = file;
                    ImGui.SetDragDropPayload("CONTENT_FILE", IntPtr.Zero, 0);
                    ImGui.Text($"Movendo {fileName}...");
                    ImGui.EndDragDropSource();
                }

                ImGui.PopStyleColor(2);
                ImGui.TextWrapped(fileName);
                ImGui.NextColumn();
                ImGui.PopID();
            }

            ImGui.Columns(1);
            
            DesenharModais();
            ImGui.End();
        }

        // ==========================================
        // MÉTODOS AUXILIARES DE INTERFACE E LÓGICA
        // ==========================================

        private void DesenharMenuContextoItem(string path, string name, bool isDirectory)
        {
            if (ImGui.BeginPopupContextItem($"Context_{name}"))
            {
                if (ImGui.MenuItem("Copiar")) { _clipboardPath = path; _isCutOperation = false; }
                if (ImGui.MenuItem("Recortar")) { _clipboardPath = path; _isCutOperation = true; }
                if (ImGui.MenuItem("Duplicar")) { DuplicarItem(path, isDirectory); }
                ImGui.Separator();
                if (ImGui.MenuItem("Renomear")) { _itemToRename = path; _renameBuffer = name; _openRenameModal = true; }
                ImGui.Separator();
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 0.2f, 0.2f, 1f));
                if (ImGui.MenuItem("Apagar")) { _itemToDelete = path; _openDeleteModal = true; }
                ImGui.PopStyleColor();
                ImGui.EndPopup();
            }
        }

        private void MoverItem(string origem, string destinoFolder)
        {
            string destPath = Path.Combine(destinoFolder, Path.GetFileName(origem));
            if (origem != destPath && !File.Exists(destPath) && !Directory.Exists(destPath))
            {
                try
                {
                    if (File.GetAttributes(origem).HasFlag(FileAttributes.Directory)) Directory.Move(origem, destPath);
                    else File.Move(origem, destPath);
                }
                catch (Exception e) { Console.WriteLine($"Erro ao mover: {e.Message}"); }
            }
            EditorState.DraggedFilePath = "";
        }

        private void ExecutarColar()
        {
            if (string.IsNullOrEmpty(_clipboardPath)) return;

            string fileName = Path.GetFileName(_clipboardPath);
            string destPath = Path.Combine(_currentDirectory, fileName);
            bool isDir = File.GetAttributes(_clipboardPath).HasFlag(FileAttributes.Directory);

            try
            {
                if (_isCutOperation)
                {
                    if (destPath != _clipboardPath && !File.Exists(destPath) && !Directory.Exists(destPath))
                    {
                        if (isDir) Directory.Move(_clipboardPath, destPath);
                        else File.Move(_clipboardPath, destPath);
                    }
                    _clipboardPath = ""; // Limpa a área de transferência ao recortar
                }
                else // Copiar
                {
                    destPath = GerarNomeUnico(destPath);
                    if (isDir) CopiarDiretorio(_clipboardPath, destPath);
                    else File.Copy(_clipboardPath, destPath);
                }
            }
            catch (Exception e) { Console.WriteLine($"Erro ao colar: {e.Message}"); }
        }

        private void DuplicarItem(string path, bool isDirectory)
        {
            try
            {
                string dir = Path.GetDirectoryName(path);
                string nameWithoutExt = Path.GetFileNameWithoutExtension(path);
                string ext = Path.GetExtension(path);
                
                string newPath = GerarNomeUnico(Path.Combine(dir, $"{nameWithoutExt}_Copia{ext}"));

                if (isDirectory) CopiarDiretorio(path, newPath);
                else File.Copy(path, newPath);
            }
            catch (Exception e) { Console.WriteLine($"Erro ao duplicar: {e.Message}"); }
        }

        private string GerarNomeUnico(string targetPath)
        {
            string dir = Path.GetDirectoryName(targetPath);
            string nameWithoutExt = Path.GetFileNameWithoutExtension(targetPath);
            string ext = Path.GetExtension(targetPath);
            string finalPath = targetPath;
            int counter = 1;

            while (File.Exists(finalPath) || Directory.Exists(finalPath))
            {
                finalPath = Path.Combine(dir, $"{nameWithoutExt}{counter}{ext}");
                counter++;
            }
            return finalPath;
        }

        private void CopiarDiretorio(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)));
            }
            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                CopiarDiretorio(dir, Path.Combine(destDir, Path.GetFileName(dir)));
            }
        }

        private void CriarPastaSegura() { Directory.CreateDirectory(GerarNomeUnico(Path.Combine(_currentDirectory, "Nova Pasta"))); }
        
        private void CriarScriptSeguro()
        {
            string newPath = GerarNomeUnico(Path.Combine(_currentDirectory, "NovoScript.cs"));
            string className = Path.GetFileNameWithoutExtension(newPath).Replace(" ", "");
            
            // O NOVO TEMPLATE: Com os Usings corretos e o GameTime!
            string scriptTemplate = 
                $@"using Engine.Core;
using Engine.Core.Components;
using Microsoft.Xna.Framework;

public class {className} : Component
{{
    public override void Start()
    {{
        base.Start();
        // Chamado uma vez quando o objeto nasce
    }}

    public override void Update(GameTime gameTime)
    {{
        // Sua lógica de frame-a-frame aqui
    }}
}}";
            File.WriteAllText(newPath, scriptTemplate);
        }

        private void CriarCenaSegura()
        {
            string newPath = GerarNomeUnico(Path.Combine(_currentDirectory, "NovaCena.scene"));
            var novaCena = new Engine.Core.Scene { Name = Path.GetFileNameWithoutExtension(newPath) };
            Engine.Core.Serialization.SceneSerializer.SaveToFile(novaCena, newPath);
            EditorState.CurrentScenePath = newPath;
        }

        private void DesenharModais()
        {
            if (_openRenameModal) { ImGui.OpenPopup("Renomear Item"); _openRenameModal = false; }
            bool dummyRename = true;
            if (ImGui.BeginPopupModal("Renomear Item", ref dummyRename, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.Text("Novo nome:");
                ImGui.InputText("##NovoNome", ref _renameBuffer, 256);
                ImGui.Spacing();
                if (ImGui.Button("Salvar", new Vector2(120, 0)))
                {
                    if (!string.IsNullOrWhiteSpace(_renameBuffer) && _itemToRename != null)
                    {
                        string parentDir = Path.GetDirectoryName(_itemToRename);
                        string newPath = Path.Combine(parentDir, _renameBuffer);
                        if (_itemToRename != newPath && !File.Exists(newPath) && !Directory.Exists(newPath))
                        {
                            if (File.GetAttributes(_itemToRename).HasFlag(FileAttributes.Directory)) Directory.Move(_itemToRename, newPath);
                            else File.Move(_itemToRename, newPath);
                        }
                    }
                    ImGui.CloseCurrentPopup(); _itemToRename = null;
                }
                ImGui.SameLine();
                if (ImGui.Button("Cancelar", new Vector2(120, 0))) { ImGui.CloseCurrentPopup(); _itemToRename = null; }
                ImGui.EndPopup();
            }

            if (_openDeleteModal) { ImGui.OpenPopup("Confirmar Exclusão"); _openDeleteModal = false; }
            bool dummyDelete = true;
            if (ImGui.BeginPopupModal("Confirmar Exclusão", ref dummyDelete, ImGuiWindowFlags.AlwaysAutoResize))
            {
                if (_itemToDelete != null)
                {
                    ImGui.Text($"Tem certeza que deseja apagar permanentemente:\n'{Path.GetFileName(_itemToDelete)}'?");
                    ImGui.Spacing();
                    
                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.8f, 0.2f, 0.2f, 1f));
                    if (ImGui.Button("SIM, APAGAR", new Vector2(120, 0)))
                    {
                        try
                        {
                            if (File.GetAttributes(_itemToDelete).HasFlag(FileAttributes.Directory)) Directory.Delete(_itemToDelete, true);
                            else File.Delete(_itemToDelete);
                        }
                        catch (Exception e) { Console.WriteLine($"Erro ao apagar: {e.Message}"); }
                        
                        ImGui.CloseCurrentPopup(); _itemToDelete = null;
                    }
                    ImGui.PopStyleColor();
                    
                    ImGui.SameLine();
                    if (ImGui.Button("Cancelar", new Vector2(120, 0))) { ImGui.CloseCurrentPopup(); _itemToDelete = null; }
                }
                ImGui.EndPopup();
            }
        }
    }
}