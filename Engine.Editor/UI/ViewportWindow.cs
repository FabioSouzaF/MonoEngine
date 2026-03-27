using ImGuiNET;
using Microsoft.Xna.Framework.Graphics;
using System;
using Engine.Core;
using Engine.Core.Modules;
using Engine.Core.Components;
using Engine.Core.Entities;

// Aliases para evitar conflitos de Matemática
using SNVector2 = System.Numerics.Vector2;
using SNVector4 = System.Numerics.Vector4;
using MGVector2 = Microsoft.Xna.Framework.Vector2;
using MGVector3 = Microsoft.Xna.Framework.Vector3;

namespace Engine.Editor.UI
{
    public class ViewportWindow : EditorWindow
    {
        private object _imGuiRenderer; 
        private RenderTarget2D _renderTarget;
        
        // Estado do Gizmo
        private int _draggingAxis = -1; 
        private MGVector2 _dragOffset;

        public ViewportWindow(object imGuiRenderer, RenderTarget2D renderTarget)
        {
            Name = "Viewport";
            _imGuiRenderer = imGuiRenderer;
            _renderTarget = renderTarget;
        }

        public override void Draw()
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new SNVector2(0, 0));
            ImGui.Begin(Name);

            var viewportMinRegion = ImGui.GetWindowContentRegionMin();
            var viewportMaxRegion = ImGui.GetWindowContentRegionMax();
            var viewportOffset = ImGui.GetWindowPos();

            SNVector2 screenPos = new SNVector2(viewportMinRegion.X + viewportOffset.X, viewportMinRegion.Y + viewportOffset.Y);
            SNVector2 viewportSize = new SNVector2(viewportMaxRegion.X - viewportMinRegion.X, viewportMaxRegion.Y - viewportMinRegion.Y);

            if (viewportSize.X > 0 && viewportSize.Y > 0)
            {
                // 1. DESENHA A IMAGEM DO JOGO
                DesenharJogo(viewportSize);

                // 2. LÓGICA DO EDITOR (Apenas se o jogo estiver parado)
                if (!EditorState.IsPlaying) 
                {
                    var camera = SceneManager.ActiveScene?.ActiveCamera;
                    var obj = EditorState.SelectedObject;

                    if (camera != null && obj != null)
                    {
                        // Calcula o rato apenas 1 vez por frame para todos os scripts usarem!
                        MGVector2 worldMousePos = ScreenToWorld(screenPos, viewportSize, camera);

                        var tilemap = obj.GetComponent<Tilemap>();
                        
                        // MÁQUINA DE ESTADOS DO VIEWPORT:
                        // Se é um Tilemap, liga o modo Pintura. Se não, liga o modo Gizmo!
                        if (tilemap != null)
                        {
                            HandleTilemapPainting(worldMousePos, screenPos, viewportSize, camera, tilemap);
                        }
                        else
                        {
                            DesenharGizmos(screenPos, viewportSize, worldMousePos, camera, obj);
                        }
                    }
                }
            }

            ImGui.End();
            ImGui.PopStyleVar();
        }

        // ==========================================
        // MÓDULO 1: RENDERIZAÇÃO BASE
        // ==========================================
        private void DesenharJogo(SNVector2 viewportSize)
        {
            var textureId = ((MonoGame.ImGuiNet.ImGuiRenderer)_imGuiRenderer).BindTexture(_renderTarget); 
            ImGui.Image(textureId, viewportSize);
        }

        // ==========================================
        // MÓDULO 2: PINTURA DO TILEMAP
        // ==========================================
        private void HandleTilemapPainting(MGVector2 worldMousePos, SNVector2 screenPos, SNVector2 viewportSize, Camera camera, Tilemap tilemap)
        {
            var scale = tilemap.Transform.LocalScale;
            float totalMapWidth = tilemap.Columns * tilemap.TileWidth * scale.X;
            float totalMapHeight = tilemap.Rows * tilemap.TileHeight * scale.Y;

            // Recria exatamente o ponto de origem que o Tilemap usa para se desenhar
            MGVector2 startPos = new MGVector2(
                tilemap.Transform.LocalPosition.X - (totalMapWidth / 2f),
                tilemap.Transform.LocalPosition.Y - (totalMapHeight / 2f)
            );

            // Descobre as coordenadas do rato relativas à grade
            float localMouseX = worldMousePos.X - startPos.X;
            float localMouseY = worldMousePos.Y - startPos.Y;

            int gridX = (int)Math.Floor(localMouseX / (tilemap.TileWidth * scale.X));
            int gridY = (int)Math.Floor(localMouseY / (tilemap.TileHeight * scale.Y));

            // Só interage se o rato estiver dentro da área do mapa
            if (gridX >= 0 && gridX < tilemap.Columns && gridY >= 0 && gridY < tilemap.Rows)
            {
                // --- A) DESENHA O PREVIEW (O QUADRADO AMARELO) ---
                MGVector2 tileWorldPos = new MGVector2(
                    startPos.X + (gridX * tilemap.TileWidth * scale.X),
                    startPos.Y + (gridY * tilemap.TileHeight * scale.Y)
                );
                
                MGVector2 tileWorldEnd = new MGVector2(
                    tileWorldPos.X + (tilemap.TileWidth * scale.X), 
                    tileWorldPos.Y + (tilemap.TileHeight * scale.Y)
                );

                SNVector2 tileScreenPos = WorldToScreen(tileWorldPos, screenPos, viewportSize, camera);
                SNVector2 tileScreenEnd = WorldToScreen(tileWorldEnd, screenPos, viewportSize, camera);

                var drawList = ImGui.GetWindowDrawList();
                drawList.AddRect(tileScreenPos, tileScreenEnd, ImGui.GetColorU32(new SNVector4(1, 1, 0, 1)), 0f, 0, 2f);

                // --- B) PINTA O TILE AO CLICAR/ARRASTAR ---
                if (ImGui.IsMouseDown(ImGuiMouseButton.Left))
                {
                    // Evita pintar por cima da janela da Paleta se o mouse escorregar
                    if (ImGui.IsWindowHovered()) 
                    {
                        tilemap.SetTile(gridX, gridY, EditorState.SelectedTileBrush);
                        EditorState.IsDirty = true;
                    }
                }
                // ATALHO EXTRA: Botão direito age como Borracha instantânea!
                else if (ImGui.IsMouseDown(ImGuiMouseButton.Right))
                {
                    if (ImGui.IsWindowHovered())
                    {
                        tilemap.SetTile(gridX, gridY, -1);
                        EditorState.IsDirty = true;
                    }
                }
            }
        }

        // ==========================================
        // MÓDULO 3: GIZMOS DE TRANSFORMAÇÃO
        // ==========================================
        private void DesenharGizmos(SNVector2 screenPos, SNVector2 viewportSize, MGVector2 worldMousePos, Camera camera, GameObject obj)
        {
            SNVector2 relativeMousePos = ImGui.GetMousePos() - screenPos;
            
            MGVector2 objWorldPos = new MGVector2(obj.Transform.Position.X, obj.Transform.Position.Y);
            SNVector2 objScreenPos = WorldToScreen(objWorldPos, screenPos, viewportSize, camera);

            var drawList = ImGui.GetWindowDrawList();
            float gizmoLength = 90f;
            float thickness = 3f;
            float arrowSize = 12f;

            bool hoverX = relativeMousePos.X > (objScreenPos.X - screenPos.X) && relativeMousePos.X < (objScreenPos.X - screenPos.X) + gizmoLength && Math.Abs(relativeMousePos.Y - (objScreenPos.Y - screenPos.Y)) < 15f;
            bool hoverY = relativeMousePos.Y < (objScreenPos.Y - screenPos.Y) && relativeMousePos.Y > (objScreenPos.Y - screenPos.Y) - gizmoLength && Math.Abs(relativeMousePos.X - (objScreenPos.X - screenPos.X)) < 15f;

            uint colorX = (_draggingAxis == 0 || hoverX) ? ImGui.GetColorU32(new SNVector4(1, 1, 0, 1)) : ImGui.GetColorU32(new SNVector4(1, 0.2f, 0.2f, 1));
            uint colorY = (_draggingAxis == 1 || hoverY) ? ImGui.GetColorU32(new SNVector4(1, 1, 0, 1)) : ImGui.GetColorU32(new SNVector4(0.2f, 1, 0.2f, 1));

            SNVector2 xAxisEnd = objScreenPos + new SNVector2(gizmoLength, 0);
            drawList.AddLine(objScreenPos, xAxisEnd, colorX, thickness);
            drawList.AddTriangleFilled(xAxisEnd + new SNVector2(arrowSize, 0), xAxisEnd + new SNVector2(0, -arrowSize / 2), xAxisEnd + new SNVector2(0, arrowSize / 2), colorX);

            SNVector2 yAxisEnd = objScreenPos + new SNVector2(0, -gizmoLength);
            drawList.AddLine(objScreenPos, yAxisEnd, colorY, thickness);
            drawList.AddTriangleFilled(yAxisEnd + new SNVector2(0, -arrowSize), yAxisEnd + new SNVector2(-arrowSize / 2, 0), yAxisEnd + new SNVector2(arrowSize / 2, 0), colorY);

            // Dragging Logic
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                if (hoverX) { _draggingAxis = 0; _dragOffset = new MGVector2(obj.Transform.LocalPosition.X - worldMousePos.X, 0); }
                else if (hoverY) { _draggingAxis = 1; _dragOffset = new MGVector2(0, obj.Transform.LocalPosition.Y - worldMousePos.Y); }
            }

            if (ImGui.IsMouseReleased(ImGuiMouseButton.Left)) _draggingAxis = -1;

            if (_draggingAxis != -1 && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
            {
                if (_draggingAxis == 0) obj.Transform.LocalPosition = new MGVector3(worldMousePos.X + _dragOffset.X, obj.Transform.LocalPosition.Y, obj.Transform.LocalPosition.Z);
                else if (_draggingAxis == 1) obj.Transform.LocalPosition = new MGVector3(obj.Transform.LocalPosition.X, worldMousePos.Y + _dragOffset.Y, obj.Transform.LocalPosition.Z);
                EditorState.IsDirty = true;
            }
        }

        // ==========================================
        // MÓDULO 4: FUNÇÕES MATEMÁTICAS AUXILIARES
        // ==========================================
        private MGVector2 ScreenToWorld(SNVector2 screenPos, SNVector2 viewportSize, Camera camera)
        {
            SNVector2 relativeMousePos = ImGui.GetMousePos() - screenPos;
            MGVector2 rtMousePos = new MGVector2((relativeMousePos.X / viewportSize.X) * 1280f, (relativeMousePos.Y / viewportSize.Y) * 720f);
            return camera.ScreenToWorld(rtMousePos);
        }

        private SNVector2 WorldToScreen(MGVector2 worldPos, SNVector2 screenPos, SNVector2 viewportSize, Camera camera)
        {
            MGVector2 rtPos = MGVector2.Transform(worldPos, camera.GetViewMatrix());
            SNVector2 imgPos = new SNVector2((rtPos.X / 1280f) * viewportSize.X, (rtPos.Y / 720f) * viewportSize.Y);
            return screenPos + imgPos;
        }
    }
}