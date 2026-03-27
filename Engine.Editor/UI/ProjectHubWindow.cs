using System;
using System.IO;
using System.Numerics;
using Engine.Core.Assets;
using ImGuiNET;

namespace Engine.Editor.UI
{
    public class ProjectHubWindow : EditorWindow
    {
        private string _newProjectName = "MeuNovoJogo";
        private string _projectsFolder;
        private string _errorMessage = "";

        public ProjectHubWindow()
        {
            Name = "Hub de Projetos";
            
            // Define a pasta padrão para os projetos (ex: /home/fabio/Documentos/MonoProjects)
            _projectsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "MonoProjects");
            if (!Directory.Exists(_projectsFolder)) Directory.CreateDirectory(_projectsFolder);
        }

        public override void Draw()
        {
            // Força a janela a ocupar a tela inteira do Editor
            var viewport = ImGui.GetMainViewport();
            ImGui.SetNextWindowPos(viewport.WorkPos);
            ImGui.SetNextWindowSize(viewport.WorkSize);
            
            ImGui.Begin(Name, ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoSavedSettings);

            // Centraliza um "Painel" no meio da tela
            var windowSize = ImGui.GetWindowSize();
            ImGui.SetCursorPos(new Vector2(windowSize.X / 2 - 200, windowSize.Y / 2 - 150));

            ImGui.BeginChild("HubPanel", new Vector2(400, 300));
            
            ImGui.TextUnformatted("Bem-vindo à MonoEngine!");
            ImGui.Separator();
            ImGui.Spacing();

            // --- CRIAR NOVO PROJETO ---
            ImGui.Text("Criar Novo Projeto:");
            ImGui.Text("Local do Projeto:");
// Permite ao utilizador alterar o caminho base antes de criar
            ImGui.InputText("##Caminho", ref _projectsFolder, 500);
            ImGui.InputText("Nome", ref _newProjectName, 100);
            
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.6f, 0.8f, 1f));
            if (ImGui.Button("Criar Projeto", new Vector2(-1, 30)))
            {
                CriarNovoProjeto();
            }
            ImGui.PopStyleColor();

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // --- LISTAR PROJETOS EXISTENTES ---
            ImGui.Text("Projetos Recentes:");
            ImGui.BeginChild("ProjectList", new Vector2(-1, 100));
            foreach (var dir in Directory.GetDirectories(_projectsFolder))
            {
                var projName = new DirectoryInfo(dir).Name;
                if (ImGui.Selectable($"📁 {projName}"))
                {
                    CarregarProjeto(dir);
                }
            }
            ImGui.EndChild();

            // Mensagem de erro caso o utilizador tente criar um projeto duplicado
            if (!string.IsNullOrEmpty(_errorMessage))
            {
                ImGui.TextColored(new Vector4(1, 0.2f, 0.2f, 1f), _errorMessage);
            }

            ImGui.EndChild();
            ImGui.End();
        }

        private void CriarNovoProjeto()
        {
            if (string.IsNullOrWhiteSpace(_newProjectName)) return;

            string projPath = Path.Combine(_projectsFolder, _newProjectName);
            if (Directory.Exists(projPath))
            {
                _errorMessage = "Um projeto com este nome já existe!";
                return;
            }

            // 1. Cria a estrutura de pastas
            Directory.CreateDirectory(projPath);
            Directory.CreateDirectory(Path.Combine(projPath, "Assets"));
            Directory.CreateDirectory(Path.Combine(projPath, "Scripts"));
            string scenesPath = Path.Combine(projPath, "Scenes");
            Directory.CreateDirectory(scenesPath);

            // 2. Cria a Cena Inicial padrão do projeto
            var cenaInicial = new Engine.Core.Scene { Name = "Cena Inicial" };
            Engine.Core.Serialization.SceneSerializer.SaveToFile(cenaInicial, Path.Combine(scenesPath, "CenaInicial.scene"));

            // 3. Cria o ficheiro de configuração do projeto (.monoengine)
            var config = new Engine.Core.Serialization.ProjectConfig
            {
                ProjectName = _newProjectName,
                WindowWidth = 1280,
                WindowHeight = 720,
                InitialScene = "Scenes/CenaInicial.scene" // Aponta para a cena que acabámos de criar
            };
            config.Save(Path.Combine(projPath, "project.monoengine"));

            CarregarProjeto(projPath);
        }
        
        private void CarregarProjeto(string path)
        {
            if (!File.Exists(Path.Combine(path, "project.monoengine")))
            {
                _errorMessage = "Pasta inválida. Arquivo .monoengine não encontrado.";
                return;
            }

            // Define globalmente qual é o projeto atual
            EditorState.CurrentProjectPath = path;
            AssetManager.ProjectRootPath = EditorState.CurrentProjectPath;
            // --- A MÁGICA AUTOMÁTICA ---
            // Assim que o projeto é reconhecido, forçamos a compilação e injeção na memória RAM!
            Console.WriteLine("[HUB] Carregando projeto e injetando scripts...");
            ScriptCompiler.CompilarELocarScripts();
            
            // --- NOVIDADE: RESTAURAR O LAYOUT DAS JANELAS ---
            string layoutPath = Path.Combine(path, "layout.ini");
            if (File.Exists(layoutPath))
            {
                ImGuiNET.ImGui.LoadIniSettingsFromDisk(layoutPath);
                Console.WriteLine("[EDITOR] Layout do painel restaurado com sucesso!");
            }
            
        }
    }
}