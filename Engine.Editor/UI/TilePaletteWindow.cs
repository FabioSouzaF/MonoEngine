using ImGuiNET;
using Engine.Core.Components;
using Engine.Core.Entities;
using Engine.Editor;
using Engine.Core.Assets;
using Microsoft.Xna.Framework.Graphics; // Necessário para ler o tamanho da Textura
using System;

// Aliases para facilitar o código
using SNVector2 = System.Numerics.Vector2;
using SNVector4 = System.Numerics.Vector4;

namespace Engine.Editor.UI
{
    public class TilePaletteWindow : EditorWindow
    {
        private object _imGuiRenderer;

        // Agora recebemos o Renderer no construtor!
        public TilePaletteWindow(object imGuiRenderer) 
        { 
            Name = "Paleta de Tiles"; 
            _imGuiRenderer = imGuiRenderer;
        }

        public override void Draw()
        {
            ImGui.Begin("Paleta de Tiles");

            if (EditorState.SelectedObject != null)
            {
                var tilemap = EditorState.SelectedObject.GetComponent<Tilemap>();
                
                if (tilemap != null)
                {
                    ImGui.TextDisabled($"Pintando no objeto: {EditorState.SelectedObject.Name}");
                    ImGui.Separator();

                    ImGui.Text($"Tinta Atual: {(EditorState.SelectedTileBrush == -1 ? "Borracha" : $"Tile {EditorState.SelectedTileBrush}")}");
                    ImGui.Spacing();

                    // --- O BOTÃO DA BORRACHA ---
                    if (EditorState.SelectedTileBrush == -1) 
                        ImGui.PushStyleColor(ImGuiCol.Button, new SNVector4(0.8f, 0.2f, 0.2f, 1f));
                    
                    if (ImGui.Button("Borracha (-1)", new SNVector2(-1, 30)))
                    {
                        EditorState.SelectedTileBrush = -1;
                    }
                    
                    if (EditorState.SelectedTileBrush == -1) ImGui.PopStyleColor();

                    ImGui.Separator();
                    ImGui.Text("Tiles Disponíveis:");
                    ImGui.Spacing();

                    // --- CARREGA A TEXTURA ---
                    Texture2D texture = null;
                    if (!string.IsNullOrEmpty(tilemap.TexturePath))
                    {
                        texture = AssetManager.LoadTexture(tilemap.TexturePath);
                    }

                    if (texture != null)
                    {
                        // Converte a textura do MonoGame para um ID que o ImGui entenda
                        var textureId = ((MonoGame.ImGuiNet.ImGuiRenderer)_imGuiRenderer).BindTexture(texture);

                        float windowVisibleX2 = ImGui.GetWindowPos().X + ImGui.GetWindowContentRegionMax().X;
                        float buttonSize = 50f;
                        
                        // MÁGICA: A Paleta agora calcula quantos tiles existem na imagem real!
                        int cols = texture.Width / tilemap.TileWidth;
                        int rows = texture.Height / tilemap.TileHeight;
                        int totalTilesInImage = cols * rows; 

                        for (int i = 0; i < totalTilesInImage; i++)
                        {
                            ImGui.PushID(i); 

                            bool isSelected = (EditorState.SelectedTileBrush == i);
                            
                            // Se estiver selecionado, fica verde. Se não, tira a cor de fundo cinza do botão!
                            if (isSelected) 
                                ImGui.PushStyleColor(ImGuiCol.Button, new SNVector4(0.2f, 0.8f, 0.2f, 1f));
                            else
                                ImGui.PushStyleColor(ImGuiCol.Button, new SNVector4(0f, 0f, 0f, 0f));

                            // --- MATEMÁTICA DAS UVs (Corte da Imagem) ---
                            // Descobre X e Y em pixels
                            int tx = (i % cols) * tilemap.TileWidth;
                            int ty = (i / cols) * tilemap.TileHeight;

                            // Converte para Porcentagem (0.0 a 1.0) para a placa de vídeo
                            SNVector2 uv0 = new SNVector2((float)tx / texture.Width, (float)ty / texture.Height);
                            SNVector2 uv1 = new SNVector2((float)(tx + tilemap.TileWidth) / texture.Width, (float)(ty + tilemap.TileHeight) / texture.Height);

                            // O NOSSO NOVO BOTÃO COM IMAGEM!
                            if (ImGui.ImageButton($"btn_tile_{i}", textureId, new SNVector2(buttonSize, buttonSize), uv0, uv1))
                            {
                                EditorState.SelectedTileBrush = i;
                            }

                            ImGui.PopStyleColor(); // Limpa a cor que colocamos no PushStyleColor acima

                            // Quebra de linha automática
                            float lastButtonX2 = ImGui.GetItemRectMax().X;
                            float nextButtonX2 = lastButtonX2 + ImGui.GetStyle().ItemSpacing.X + buttonSize;
                            if (i + 1 < totalTilesInImage && nextButtonX2 < windowVisibleX2)
                            {
                                ImGui.SameLine();
                            }

                            ImGui.PopID();
                        }
                    }
                    else
                    {
                        ImGui.TextDisabled("Textura não carregada. Verifique o TexturePath no Inspetor.");
                    }
                }
                else
                {
                    ImGui.TextDisabled("Selecione um objeto com o\ncomponente 'Tilemap' para pintar.");
                }
            }
            else
            {
                ImGui.TextDisabled("Nenhum objeto selecionado.");
            }

            ImGui.End();
        }
    }
}