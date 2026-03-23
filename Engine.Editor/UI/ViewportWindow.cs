using ImGuiNET;
using Microsoft.Xna.Framework.Graphics;
using System;
using Engine.Core;
using Engine.Core.Modules;

// Aliases para evitar conflitos de Matemática
using SNVector2 = System.Numerics.Vector2;
using SNVector4 = System.Numerics.Vector4;
using MGVector2 = Microsoft.Xna.Framework.Vector2;
using MGVector3 = Microsoft.Xna.Framework.Vector3;

namespace Engine.Editor.UI
{
    public class ViewportWindow : EditorWindow
    {
        // NOTA: Ajuste o tipo do _imGuiRenderer se a sua classe de renderização tiver um nome diferente
        private object _imGuiRenderer; 
        private RenderTarget2D _renderTarget;
        
        // --- Estado do Gizmo ---
        private int _draggingAxis = -1; // -1 = Nenhum, 0 = Eixo X (Vermelho), 1 = Eixo Y (Verde)
        private MGVector2 _dragOffset;

        // Se o seu construtor for diferente, mantenha o seu, mas passe o ImGuiRenderer e o RenderTarget!
        public ViewportWindow(object imGuiRenderer, RenderTarget2D renderTarget)
        {
            Name = "Viewport";
            _imGuiRenderer = imGuiRenderer;
            _renderTarget = renderTarget;
        }

        public override void Draw()
        {
            // Remove as bordas internas para a imagem do jogo colar nos cantos da janela
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new SNVector2(0, 0));
            ImGui.Begin(Name);

            var viewportMinRegion = ImGui.GetWindowContentRegionMin();
            var viewportMaxRegion = ImGui.GetWindowContentRegionMax();
            var viewportOffset = ImGui.GetWindowPos();

            // Onde a imagem do jogo começa e termina na tela do seu computador
            SNVector2 screenPos = new SNVector2(viewportMinRegion.X + viewportOffset.X, viewportMinRegion.Y + viewportOffset.Y);
            SNVector2 viewportSize = new SNVector2(viewportMaxRegion.X - viewportMinRegion.X, viewportMaxRegion.Y - viewportMinRegion.Y);

            if (viewportSize.X > 0 && viewportSize.Y > 0)
            {
                // DESENHA O JOGO 
                // (Substitua esta linha pela sua chamada original de BindTexture se der erro)
                var textureId = ((MonoGame.ImGuiNet.ImGuiRenderer)_imGuiRenderer).BindTexture(_renderTarget); 
                ImGui.Image(textureId, viewportSize);

                // DESENHA OS GIZMOS POR CIMA DO JOGO
                if (!EditorState.IsPlaying) // Só desenha Gizmos se o jogo estiver parado!
                {
                    DesenharGizmos(screenPos, viewportSize);
                }
            }

            ImGui.End();
            ImGui.PopStyleVar();
        }

        private void DesenharGizmos(SNVector2 screenPos, SNVector2 viewportSize)
        {
            if (EditorState.SelectedObject == null || SceneManager.ActiveScene?.ActiveCamera == null) return;

            var obj = EditorState.SelectedObject;
            var camera = SceneManager.ActiveScene.ActiveCamera;

            // ==========================================
            // 1. MATEMÁTICA: MOUSE -> MUNDO
            // ==========================================
            SNVector2 mousePos = ImGui.GetMousePos();
            SNVector2 relativeMousePos = mousePos - screenPos;
            
            // Converte a posição do rato na UI para a escala real do RenderTarget (1280x720)
            MGVector2 rtMousePos = new MGVector2(
                (relativeMousePos.X / viewportSize.X) * 1280f,
                (relativeMousePos.Y / viewportSize.Y) * 720f
            );
            
            // Usa a Matriz da Câmera para descobrir exatamente onde o rato está no universo 2D
            MGVector2 worldMousePos = camera.ScreenToWorld(rtMousePos);

            // ==========================================
            // 2. MATEMÁTICA: MUNDO -> TELA
            // ==========================================
            MGVector2 objWorldPos = new MGVector2(obj.Transform.Position.X, obj.Transform.Position.Y);
            
            // Projeta o objeto do mundo para a tela virtual (1280x720)
            MGVector2 objRtPos = MGVector2.Transform(objWorldPos, camera.GetViewMatrix());
            
            // Projeta da tela virtual para o tamanho atual da janela flutuante do Viewport
            SNVector2 objImgPos = new SNVector2(
                (objRtPos.X / 1280f) * viewportSize.X,
                (objRtPos.Y / 720f) * viewportSize.Y
            );
            SNVector2 objScreenPos = screenPos + objImgPos; // Posição Final na tela do Monitor

            // ==========================================
            // 3. DESENHO DOS GIZMOS (ImGui DrawList)
            // ==========================================
            var drawList = ImGui.GetWindowDrawList();
            float gizmoLength = 90f;
            float thickness = 3f;
            float arrowSize = 12f;

            // Hitboxes (Zonas de colisão invisíveis do rato)
            bool hoverX = relativeMousePos.X > objImgPos.X && relativeMousePos.X < objImgPos.X + gizmoLength && Math.Abs(relativeMousePos.Y - objImgPos.Y) < 15f;
            bool hoverY = relativeMousePos.Y < objImgPos.Y && relativeMousePos.Y > objImgPos.Y - gizmoLength && Math.Abs(relativeMousePos.X - objImgPos.X) < 15f;

            // Cores Ativas (Ficam Amarelas quando o rato passa por cima ou clica)
            uint colorX = (_draggingAxis == 0 || hoverX) ? ImGui.GetColorU32(new SNVector4(1, 1, 0, 1)) : ImGui.GetColorU32(new SNVector4(1, 0.2f, 0.2f, 1));
            uint colorY = (_draggingAxis == 1 || hoverY) ? ImGui.GetColorU32(new SNVector4(1, 1, 0, 1)) : ImGui.GetColorU32(new SNVector4(0.2f, 1, 0.2f, 1));

            // Eixo X (Seta para a Direita)
            SNVector2 xAxisEnd = objScreenPos + new SNVector2(gizmoLength, 0);
            drawList.AddLine(objScreenPos, xAxisEnd, colorX, thickness);
            drawList.AddTriangleFilled(xAxisEnd + new SNVector2(arrowSize, 0), xAxisEnd + new SNVector2(0, -arrowSize / 2), xAxisEnd + new SNVector2(0, arrowSize / 2), colorX);

            // Eixo Y (Seta para Cima - Nota: Y é negativo para subir no ecrã)
            SNVector2 yAxisEnd = objScreenPos + new SNVector2(0, -gizmoLength);
            drawList.AddLine(objScreenPos, yAxisEnd, colorY, thickness);
            drawList.AddTriangleFilled(yAxisEnd + new SNVector2(0, -arrowSize), yAxisEnd + new SNVector2(-arrowSize / 2, 0), yAxisEnd + new SNVector2(arrowSize / 2, 0), colorY);

            // ==========================================
            // 4. LÓGICA DE ARRASTO (DRAG & DROP)
            // ==========================================
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                if (hoverX) 
                { 
                    _draggingAxis = 0; 
                    // Guarda a diferença exata entre o centro do objeto e o ponto do rato onde clicámos
                    _dragOffset = new MGVector2(obj.Transform.LocalPosition.X - worldMousePos.X, 0); 
                }
                else if (hoverY) 
                { 
                    _draggingAxis = 1; 
                    _dragOffset = new MGVector2(0, obj.Transform.LocalPosition.Y - worldMousePos.Y); 
                }
            }

            // Larga a seta
            if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
            {
                _draggingAxis = -1;
            }

            // Move o Objeto! (Alteramos o LocalPosition para garantir que funciona bem na Hierarquia)
            if (_draggingAxis != -1 && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
            {
                if (_draggingAxis == 0)
                {
                    obj.Transform.LocalPosition = new MGVector3(worldMousePos.X + _dragOffset.X, obj.Transform.LocalPosition.Y, obj.Transform.LocalPosition.Z);
                }
                else if (_draggingAxis == 1)
                {
                    obj.Transform.LocalPosition = new MGVector3(obj.Transform.LocalPosition.X, worldMousePos.Y + _dragOffset.Y, obj.Transform.LocalPosition.Z);
                }
            }
        }
    }
}