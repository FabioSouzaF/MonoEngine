using ImGuiNET;
using Engine.Core;
using Engine.Core.Serialization;
using System.IO;
using System.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using Engine.Core.Modules;
using System.IO.Compression;

namespace Engine.Editor.UI
{
    public class MainMenuBar : EditorWindow
    {
        private bool _openBuildModal = false;
        private string _targetRuntime = "";
        private Dictionary<string, bool> _scenesToBuild = new Dictionary<string, bool>();

        public MainMenuBar() { Name = "Menu Principal"; }

        public override void Draw()
        {
            if (ImGui.BeginMainMenuBar())
            {
                if (ImGui.BeginMenu("Arquivo"))
                {
                    if (ImGui.MenuItem("Nova Cena"))
                    {
                        string scenesDir = Path.Combine(EditorState.CurrentProjectPath, "Scenes");
                        string baseName = "NovaCena";
                        string finalName = baseName;
                        int counter = 1;
                        while (File.Exists(Path.Combine(scenesDir, $"{finalName}.scene")))
                        {
                            finalName = $"{baseName}{counter}";
                            counter++;
                        }
                        SceneManager.LoadScene(new Scene { Name = finalName });
                        EditorState.SelectedObject = null;
                        EditorState.CurrentScenePath = ""; 
                        EditorState.IsDirty = false;
                        EditorState.CurrentScenePath = "";
                    }
                    
                    bool isPlaying = EditorState.IsPlaying;
                    if (isPlaying) ImGui.BeginDisabled();
                    
                    if (ImGui.MenuItem("Salvar Cena"))
                    {
                        if (EditorState.IsProjectLoaded && SceneManager.ActiveScene != null)
                        {
                            string savePath = EditorState.CurrentScenePath;
                            if (string.IsNullOrEmpty(savePath))
                            {
                                string scenesDir = Path.Combine(EditorState.CurrentProjectPath, "Scenes");
                                if (!Directory.Exists(scenesDir)) Directory.CreateDirectory(scenesDir);
                                savePath = Path.Combine(scenesDir, $"{SceneManager.ActiveScene.Name}.scene");
                                EditorState.CurrentScenePath = savePath; 
                            }
                            SceneSerializer.SaveToFile(SceneManager.ActiveScene, savePath);
                            EditorState.IsDirty = false;
                            Console.WriteLine($"[CENA SALVA] Sucesso: {savePath}");
                        }
                    }
                    
                    ImGui.Separator();
                    
                    if (ImGui.MenuItem("Salvar Layout das Janelas"))
                    {
                        if (EditorState.IsProjectLoaded)
                        {
                            string layoutPath = Path.Combine(EditorState.CurrentProjectPath, "layout.ini");
                            ImGui.SaveIniSettingsToDisk(layoutPath);
                            Console.WriteLine("[EDITOR] Layout salvo com sucesso!");
                        }
                    }

                    if (isPlaying) 
                    {
                        ImGui.EndDisabled();
                        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) ImGui.SetTooltip("Aviso: Não é possível guardar a cena no Modo Play!");
                    }
                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("Build"))
                {
                    if (EditorState.IsProjectLoaded)
                    {
                        ImGui.TextDisabled("Exportar para:");
                        ImGui.Separator();
                        if (ImGui.MenuItem("Windows (64-bit)")) { PrepararBuild("win-x64"); }
                        if (ImGui.MenuItem("Linux (64-bit)"))   { PrepararBuild("linux-x64"); }
                        if (ImGui.MenuItem("macOS (64-bit)"))   { PrepararBuild("osx-x64"); }
                    }
                    else ImGui.TextDisabled("Carregue um projeto primeiro");
                    ImGui.EndMenu();
                }
                
                if (ImGui.BeginMenu("Scripts"))
                {
                    if (ImGui.MenuItem("Compilar e Recarregar"))
                    {
                        ScriptCompiler.CompilarELocarScripts();
                    }
                    ImGui.EndMenu();
                }
                
                ImGui.EndMainMenuBar();
            }

            DesenharModalDeBuild();
        }

        private void PrepararBuild(string runtime)
        {
            _targetRuntime = runtime;
            _scenesToBuild.Clear();
            string scenesDir = Path.Combine(EditorState.CurrentProjectPath, "Scenes");
            
            if (Directory.Exists(scenesDir))
            {
                // NOVIDADE: Busca em todas as subpastas!
                foreach (var file in Directory.GetFiles(scenesDir, "*.scene", SearchOption.AllDirectories))
                {
                    _scenesToBuild[file] = true; 
                }
            }
            _openBuildModal = true;
        }

        private void DesenharModalDeBuild()
        {
            if (_openBuildModal) { ImGui.OpenPopup("Configurações de Build"); _openBuildModal = false; }

            bool dummyOpen = true;
            if (ImGui.BeginPopupModal("Configurações de Build", ref dummyOpen, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.Text($"Plataforma Alvo: {_targetRuntime}");
                ImGui.Separator(); ImGui.Spacing();
                ImGui.Text("Selecione as cenas para incluir no pacote final:");
                ImGui.BeginChild("ScenesList", new System.Numerics.Vector2(400, 200));

                if (_scenesToBuild.Count == 0) ImGui.TextDisabled("Nenhuma cena encontrada na pasta 'Scenes'.");
                else
                {
                    foreach (var kvp in _scenesToBuild.ToList())
                    {
                        bool isChecked = kvp.Value;
                        string sceneName = Path.GetFileName(kvp.Key);
                        if (ImGui.Checkbox(sceneName, ref isChecked)) _scenesToBuild[kvp.Key] = isChecked;
                    }
                }
                ImGui.EndChild();
                ImGui.Spacing();
                
                int selectedCount = _scenesToBuild.Values.Count(v => v);
                if (selectedCount == 0) ImGui.BeginDisabled();
                
                ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0.2f, 0.6f, 0.2f, 1f));
                if (ImGui.Button($"Exportar Jogo ({selectedCount} Cenas)", new System.Numerics.Vector2(200, 30)))
                {
                    ExportarJogo(_targetRuntime, _scenesToBuild.Where(k => k.Value).Select(k => k.Key).ToList());
                    ImGui.CloseCurrentPopup();
                }
                ImGui.PopStyleColor();

                if (selectedCount == 0) ImGui.EndDisabled();
                ImGui.SameLine();
                if (ImGui.Button("Cancelar", new System.Numerics.Vector2(120, 30))) ImGui.CloseCurrentPopup();

                ImGui.EndPopup();
            }
        }

        private void ExportarJogo(string targetRuntime, List<string> cenasParaExportar)
        {
            string projPath = EditorState.CurrentProjectPath;
            string buildDir = Path.Combine(projPath, "Build", targetRuntime);
            
            if (Directory.Exists(buildDir)) Directory.Delete(buildDir, true);
            Directory.CreateDirectory(buildDir);

            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string runtimeProjFolder = Path.GetFullPath(Path.Combine(basePath, "..", "..", "..", "..", "Engine.Runtime"));
            
            Console.WriteLine($"[BUILD] Iniciando compilação do Runtime para {targetRuntime}...");

            ProcessStartInfo psiRuntime = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"publish \"{runtimeProjFolder}\" -c Release -r {targetRuntime} --self-contained false -o \"{buildDir}\"",
                RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true
            };

            using (Process process = Process.Start(psiRuntime))
            {
                process.WaitForExit();
                if (process.ExitCode != 0) { Console.WriteLine($"[ERRO DE BUILD] {process.StandardError.ReadToEnd()}"); return; }
            }

            // --- NOVIDADE: COMPILAR OS SCRIPTS DO USUÁRIO ---
            if (!CompilarScriptsDoUsuario(projPath, buildDir, basePath))
            {
                Console.WriteLine("[BUILD] Exportação abortada devido a erros nos scripts do usuário.");
                return;
            }

            Console.WriteLine($"[BUILD] Compilações concluídas! Copiando Assets e Cenas...");

            // --- NOVIDADE: AJUSTE DINÂMICO DA CENA INICIAL ---
            // --- NOVIDADE: AJUSTE DINÂMICO DA CENA INICIAL (.DAT) ---
            string configSource = Path.Combine(projPath, "project.monoengine");
            if (File.Exists(configSource)) 
            {
                var config = ProjectConfig.Load(configSource);
                string currentInitialName = Path.GetFileNameWithoutExtension(config.InitialScene);
                bool hasInitial = cenasParaExportar.Any(c => Path.GetFileNameWithoutExtension(c) == currentInitialName);
                
                if (!hasInitial && cenasParaExportar.Count > 0)
                {
                    config.InitialScene = "Scenes/" + Path.GetFileNameWithoutExtension(cenasParaExportar[0]) + ".dat";
                    Console.WriteLine($"[BUILD] Auto-ajuste: Cena inicial definida para {config.InitialScene}");
                }
                else
                {
                    // Garante que a extensão fique .dat mesmo que a cena não mude
                    config.InitialScene = "Scenes/" + currentInitialName + ".dat";
                }
                
                config.Save(Path.Combine(buildDir, "project.monoengine"));
            }

            // --- NOVIDADE: EMPACOTAMENTO DOS ASSETS (.PAK) ---
            Console.WriteLine("[BUILD] Empacotando e blindando a pasta Assets...");
            string assetsDir = Path.Combine(projPath, "Assets");
            string pakPath = Path.Combine(buildDir, "Assets.pak");
            
            if (Directory.Exists(assetsDir))
            {
                // Transforma a pasta inteira num único arquivo comprimido e seguro
                ZipFile.CreateFromDirectory(assetsDir, pakPath, CompressionLevel.Optimal, false);
            }

            // (Apague a chamada antiga do CopiarPasta(..., ...) que estava aqui)

            string buildScenesDir = Path.Combine(buildDir, "Scenes");
            string originalScenesDir = Path.Combine(projPath, "Scenes");
            Directory.CreateDirectory(buildScenesDir);
            
            foreach (var cenaPath in cenasParaExportar)
            {
                string relativePath = Path.GetRelativePath(originalScenesDir, cenaPath);
                
                // TRUQUE EXTRA: Mudamos a extensão para .dat na pasta de Build para assustar hackers
                string destFile = Path.Combine(buildScenesDir, relativePath).Replace(".scene", ".dat");
                
                Directory.CreateDirectory(Path.GetDirectoryName(destFile));
                
                // Em vez de copiar o arquivo fisicamente, lemos o texto original e exportamos criptografado!
                string jsonOriginal = File.ReadAllText(cenaPath);
                Engine.Core.Serialization.SceneSerializer.ExportEncrypted(jsonOriginal, destFile);
            }

            Console.WriteLine($"[BUILD CONCLUÍDO] O seu jogo está pronto e criptografado em: {buildDir}");
        }
        
        // --- A MÁGICA DA COMPILAÇÃO DINÂMICA (ROSILYN/DOTNET CLI) ---
        private bool CompilarScriptsDoUsuario(string projPath, string buildDir, string engineBasePath)
        {
            string scriptsDir = Path.Combine(projPath, "Scripts");
            if (!Directory.Exists(scriptsDir) || Directory.GetFiles(scriptsDir, "*.cs", SearchOption.AllDirectories).Length == 0)
            {
                return true; 
            }

            Console.WriteLine("[BUILD] Compilando lógica C# do jogo (UserScripts.dll)...");
            
            string csprojPath = Path.Combine(projPath, "UserScripts.csproj");
            string coreDllPath = Path.Combine(engineBasePath, "Engine.Core.dll");

            string csprojContent = $@"<Project Sdk=""Microsoft.NET.Sdk"">
              <PropertyGroup>
                <TargetFramework>net9.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
              </PropertyGroup>
              <ItemGroup>
                <Compile Include=""Scripts/**/*.cs"" />
                <Reference Include=""Engine.Core"">
                  <HintPath>{coreDllPath}</HintPath>
                </Reference>
                <PackageReference Include=""MonoGame.Framework.DesktopGL"" Version=""3.8.1.303"" />
              </ItemGroup>
            </Project>";
            
            File.WriteAllText(csprojPath, csprojContent);

            ProcessStartInfo psiScripts = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"build \"{csprojPath}\" -c Release -o \"{buildDir}\"",
                RedirectStandardOutput = true, 
                RedirectStandardError = true, 
                UseShellExecute = false, 
                CreateNoWindow = true
            };

            using (Process process = Process.Start(psiScripts))
            {
                // A CORREÇÃO: Lemos o Output normal onde o dotnet esconde os erros!
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    Console.WriteLine($"[ERRO DE COMPILAÇÃO C#]:\n{output}\n{error}");
                    File.Delete(csprojPath);
                    return false;
                }
            }

            File.Delete(csprojPath);
            return true;
        }

        private void CopiarPasta(string sourceDir, string destinationDir)
        {
            if (!Directory.Exists(sourceDir)) return;
            Directory.CreateDirectory(destinationDir);
            foreach (var file in Directory.GetFiles(sourceDir)) File.Copy(file, Path.Combine(destinationDir, Path.GetFileName(file)), true);
            foreach (var dir in Directory.GetDirectories(sourceDir)) CopiarPasta(dir, Path.Combine(destinationDir, Path.GetFileName(dir)));
        }
    }
}