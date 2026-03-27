using Engine.Core.Assets;
using Engine.Core.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Engine.Core.Components
{
    public class Tilemap : Renderer
    {
        public string TexturePath = "";
        public int TileWidth = 16;
        public int TileHeight = 16;
        
        public int Columns = 20; 
        public int Rows = 10;    
        
        // Agora isso vai funcionar perfeitamente e o JSON vai salvar e carregar sem duplicar!
        public int[] MapData = new int[200]; 

        private int _lastCols = 20;
        private int _lastRows = 10;

        private Texture2D _texture;

        public override void Start()
        {
            if (!string.IsNullOrEmpty(TexturePath))
            {
                _texture = AssetManager.LoadTexture(TexturePath);
            }
            
            _lastCols = Columns;
            _lastRows = Rows;
            
            if (MapData == null || MapData.Length != Columns * Rows)
            {
                MapData = new int[Columns * Rows];
                Array.Fill(MapData, -1); 
            }
        }
        

        public override void Update(GameTime gameTime)
        {
            if (Columns != _lastCols || Rows != _lastRows)
            {
                ResizeMap(_lastCols, _lastRows);
                _lastCols = Columns;
                _lastRows = Rows;
            }
        }

        private void ResizeMap(int oldCols, int oldRows)
        {
            int[] newMap = new int[Columns * Rows];
            Array.Fill(newMap, -1);

            if (MapData != null)
            {
                for (int y = 0; y < Math.Min(Rows, oldRows); y++)
                {
                    for (int x = 0; x < Math.Min(Columns, oldCols); x++)
                    {
                        int oldIndex = x + y * oldCols;
                        int newIndex = x + y * Columns;
                        
                        if (oldIndex < MapData.Length && newIndex < newMap.Length)
                        {
                            newMap[newIndex] = MapData[oldIndex];
                        }
                    }
                }
            }

            MapData = newMap;
        }

        public void SetTile(int x, int y, int tileId)
        {
            if (x >= 0 && x < Columns && y >= 0 && y < Rows)
                MapData[x + y * Columns] = tileId;
        }

        public int GetTile(int x, int y)
        {
            if (x >= 0 && x < Columns && y >= 0 && y < Rows)
                return MapData[x + y * Columns];
            return -1; 
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (_texture == null || !Enabled || MapData == null) return;

            int tilesheetColumns = _texture.Width / TileWidth;
            if (tilesheetColumns == 0) return;

            Vector2 scale = new Vector2(Transform.LocalScale.X, Transform.LocalScale.Y);

            float totalMapWidth = Columns * TileWidth * scale.X;
            float totalMapHeight = Rows * TileHeight * scale.Y;

            Vector2 startPos = new Vector2(
                Transform.LocalPosition.X - (totalMapWidth / 2f),
                Transform.LocalPosition.Y - (totalMapHeight / 2f)
            );

            for (int y = 0; y < Rows; y++)
            {
                for (int x = 0; x < Columns; x++)
                {
                    int index = x + y * Columns;
                    if (index >= MapData.Length) continue;

                    int tileId = MapData[index];
                    if (tileId == -1) continue;

                    int tx = (tileId % tilesheetColumns) * TileWidth;
                    int ty = (tileId / tilesheetColumns) * TileHeight;
                    Rectangle sourceRect = new Rectangle(tx, ty, TileWidth, TileHeight);
                    
                    Vector2 drawPos = new Vector2(
                        startPos.X + (x * TileWidth * scale.X),
                        startPos.Y + (y * TileHeight * scale.Y)
                    );

                    spriteBatch.Draw(
                        _texture,
                        drawPos,
                        sourceRect,
                        Color.White,
                        0f,             
                        Vector2.Zero,   
                        scale,
                        SpriteEffects.None,
                        OrderInLayer
                    );
                }
            }
        }
    }
}