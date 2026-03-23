using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Engine.Editor
{
    public static class ScriptCompiler
    {
        public static void CompilarELocarScripts()
        {
            if (!EditorState.IsProjectLoaded) return;

            string projPath = EditorState.CurrentProjectPath;
            string scriptsDir = Path.Combine(projPath, "Scripts");
            
            if (!Directory.Exists(scriptsDir) || Directory.GetFiles(scriptsDir, "*.cs", SearchOption.AllDirectories).Length == 0)
                return;

            Console.WriteLine("[EDITOR] Compilando scripts do projeto para o Inspetor...");

            string tempDir = Path.Combine(projPath, ".temp");
            Directory.CreateDirectory(tempDir);

            string csprojPath = Path.Combine(tempDir, "EditorScripts.csproj");
            string engineBasePath = AppDomain.CurrentDomain.BaseDirectory;
            string coreDllPath = Path.Combine(engineBasePath, "Engine.Core.dll");

            string csprojContent = $@"<Project Sdk=""Microsoft.NET.Sdk"">
              <PropertyGroup>
                <TargetFramework>net9.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
              </PropertyGroup>
              <ItemGroup>
                <Compile Include=""../Scripts/**/*.cs"" />
                <Reference Include=""Engine.Core"">
                  <HintPath>{coreDllPath}</HintPath>
                </Reference>
                <PackageReference Include=""MonoGame.Framework.DesktopGL"" Version=""3.8.1.303"" />
              </ItemGroup>
            </Project>";

            File.WriteAllText(csprojPath, csprojContent);

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"build \"{csprojPath}\" -c Debug -o \"{tempDir}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(psi))
            {
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    string dllPath = Path.Combine(tempDir, "EditorScripts.dll");
                    if (File.Exists(dllPath))
                    {
                        // O TRUQUE DE OURO: Lemos os bytes do arquivo para não o bloquear no HD!
                        byte[] assemblyBytes = File.ReadAllBytes(dllPath);
                        EditorState.UserAssembly = Assembly.Load(assemblyBytes);
                        Console.WriteLine("[EDITOR] Scripts injetados no Inspetor com sucesso!");
                    }
                }
                else
                {
                    Console.WriteLine($"[ERRO AO COMPILAR PARA O EDITOR]:\n{output}\n{error}");
                }
            }
        }
    }
}