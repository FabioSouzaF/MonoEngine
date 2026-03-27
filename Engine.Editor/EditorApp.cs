using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.ImGuiNet;
using Engine.Core;
using Engine.Core.Assets;
using Engine.Core.Modules;
using Engine.Editor.UI; // Importante para as janelas!

namespace Engine.Editor
{
    public class EditorApp : Game
    {
        private GraphicsDeviceManager _graphics;
        private ImGuiRenderer _imGuiRenderer;
        private SpriteBatch _spriteBatch;
        private RenderTarget2D _sceneRenderTarget;
        
        
        // O nosso novo gerenciador de interface!
        private EditorUIManager _uiManager;
        private EditorUIManager _hubUIManager;

        public EditorApp()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            _graphics.PreferredBackBufferWidth = 1280;
            _graphics.PreferredBackBufferHeight = 720;
            Window.AllowUserResizing = true;
        }

        protected override void Initialize()
        {
            Screen.Initialize(_graphics);
            _imGuiRenderer = new ImGuiRenderer(this);
            _imGuiRenderer.RebuildFontAtlas();
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _sceneRenderTarget = new RenderTarget2D(GraphicsDevice, 1280, 720, false, SurfaceFormat.Color, DepthFormat.None);
            AssetManager.Initialize(GraphicsDevice);
            GizmoRenderer.Initialize(GraphicsDevice);
            SceneManager.LoadScene(new Scene { Name = "Cena Vazia" });

            // 1. Interface do Hub (Apenas a tela inicial)
            _hubUIManager = new EditorUIManager();
            _hubUIManager.AddWindow(new ProjectHubWindow());

            // 2. Interface do Editor Completo
            _uiManager = new EditorUIManager();
            _uiManager.AddWindow(new MainMenuBar()); // <-- Se a sua MainMenuBar estiver encapsulada numa EditorWindow
            _uiManager.AddWindow(new ToolbarWindow());
            _uiManager.AddWindow(new ViewportWindow(_imGuiRenderer, _sceneRenderTarget));
            _uiManager.AddWindow(new HierarchyWindow());
            _uiManager.AddWindow(new InspectorWindow());
            _uiManager.AddWindow(new ContentBrowserWindow());
            
            _uiManager.AddWindow(new TilePaletteWindow(_imGuiRenderer));
        }
        
        protected override void Update(GameTime gameTime)
        {
            // --- ATUALIZAÇÃO DO TÍTULO (DIRTY FLAG) ---
            string title = "MonoEngine";
            if (EditorState.IsProjectLoaded) title += $" - {System.IO.Path.GetFileName(EditorState.CurrentProjectPath)}";
            if (EditorState.IsDirty) title += " *";
            Window.Title = title;

            Time.Update(gameTime);
            Input.Update();
            SceneManager.Update(gameTime);
            base.Update(gameTime);
        }

        // protected override void Draw(GameTime gameTime)
        // {
        //     // 1. Jogo
        //     GraphicsDevice.SetRenderTarget(_sceneRenderTarget);
        //     GraphicsDevice.Clear(Color.CornflowerBlue); 
        //     RenderManager.Draw(_spriteBatch);
        //
        //     // 2. Editor
        //     GraphicsDevice.SetRenderTarget(null); 
        //     GraphicsDevice.Clear(new Color(40, 44, 52));
        //
        //     _imGuiRenderer.BeginLayout(gameTime);
        //     
        //     // Chama todas as janelas registadas
        //     _uiManager.Draw();
        //
        //     _imGuiRenderer.EndLayout();
        //     base.Draw(gameTime);
        // }
        
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.SetRenderTarget(_sceneRenderTarget);
            GraphicsDevice.Clear(Color.CornflowerBlue); 
            RenderManager.Draw(_spriteBatch);

            GraphicsDevice.SetRenderTarget(null); 
            GraphicsDevice.Clear(new Color(40, 44, 52));

            _imGuiRenderer.BeginLayout(gameTime);
            
            // --- AS DUAS REGRAS DE OURO DO EDITOR ---
            var io = ImGuiNET.ImGui.GetIO();
            io.ConfigFlags |= ImGuiNET.ImGuiConfigFlags.DockingEnable; // 1. Ativa o sistema de encaixe (Unity Style)
            io.ConfigWindowsMoveFromTitleBarOnly = true;               // 2. Impede que clicar no Viewport arraste a janela!

            if (!EditorState.IsProjectLoaded)
            {
                // Se não há projeto, desenha APENAS o Hub
                _hubUIManager.Draw();
            }
            else
            {
                // --- O IMÃ GERAL (DOCKSPACE BASE) ---
                var viewport = ImGuiNET.ImGui.GetMainViewport();
                ImGuiNET.ImGui.SetNextWindowPos(viewport.WorkPos);
                ImGuiNET.ImGui.SetNextWindowSize(viewport.WorkSize);
                ImGuiNET.ImGui.SetNextWindowViewport(viewport.ID);

                // Configurações para esta janela base ser completamente invisível e imóvel
                ImGuiNET.ImGuiWindowFlags windowFlags = ImGuiNET.ImGuiWindowFlags.NoDocking | 
                                                        ImGuiNET.ImGuiWindowFlags.NoTitleBar | 
                                                        ImGuiNET.ImGuiWindowFlags.NoCollapse | 
                                                        ImGuiNET.ImGuiWindowFlags.NoResize | 
                                                        ImGuiNET.ImGuiWindowFlags.NoMove | 
                                                        ImGuiNET.ImGuiWindowFlags.NoBringToFrontOnFocus | 
                                                        ImGuiNET.ImGuiWindowFlags.NoNavFocus | 
                                                        ImGuiNET.ImGuiWindowFlags.NoBackground;

                ImGuiNET.ImGui.PushStyleVar(ImGuiNET.ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(0.0f, 0.0f));
                ImGuiNET.ImGui.PushStyleVar(ImGuiNET.ImGuiStyleVar.WindowBorderSize, 0.0f);
                
                ImGuiNET.ImGui.Begin("MainDockSpace", windowFlags);
                
                ImGuiNET.ImGui.PopStyleVar(2);

                // Cria o espaço de encaixe real
                ImGuiNET.ImGui.DockSpace(ImGuiNET.ImGui.GetID("MyDockSpace"), new System.Numerics.Vector2(0.0f, 0.0f), ImGuiNET.ImGuiDockNodeFlags.None);

                // --- FEEDBACK VISUAL DE PLAY MODE ---
                bool isPlayingNow = EditorState.IsPlaying;
                if (isPlayingNow)
                {
                    // Um tom avermelhado sutil nos cabeçalhos e fundos de janela para avisar que é "Play Mode"
                    ImGuiNET.ImGui.PushStyleColor(ImGuiNET.ImGuiCol.Header, new System.Numerics.Vector4(0.4f, 0.1f, 0.1f, 1.0f));
                    ImGuiNET.ImGui.PushStyleColor(ImGuiNET.ImGuiCol.HeaderHovered, new System.Numerics.Vector4(0.5f, 0.2f, 0.2f, 1.0f));
                    ImGuiNET.ImGui.PushStyleColor(ImGuiNET.ImGuiCol.HeaderActive, new System.Numerics.Vector4(0.6f, 0.3f, 0.3f, 1.0f));
                    ImGuiNET.ImGui.PushStyleColor(ImGuiNET.ImGuiCol.TitleBgActive, new System.Numerics.Vector4(0.3f, 0.1f, 0.1f, 1.0f));
                }

                // Agora desenhamos o Editor! Todas as janelas vão "nascer" e colar neste DockSpace
                _uiManager.Draw();

                if (isPlayingNow)
                {
                    ImGuiNET.ImGui.PopStyleColor(4);
                }

                ImGuiNET.ImGui.End();
            }

            _imGuiRenderer.EndLayout();
            base.Draw(gameTime);
        }
        
        protected override void UnloadContent()
        {
            // Salva o layout das janelas automaticamente quando o utilizador fecha o Editor!
            if (EditorState.IsProjectLoaded)
            {
                string layoutPath = Path.Combine(EditorState.CurrentProjectPath, "layout.ini");
                ImGuiNET.ImGui.SaveIniSettingsToDisk(layoutPath);
            }
            
            base.UnloadContent();
        }
        
    }
}