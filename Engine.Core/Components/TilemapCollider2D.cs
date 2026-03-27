using Engine.Core.Entities;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Engine.Core.Components
{
    public class TilemapCollider2D : Component
    {
        private Tilemap _tilemap;
        // AGORA É UMA LISTA DE FLOATS!
        public List<BoundingBox> CollisionRects = new List<BoundingBox>();

        public override void Start()
        {
            _tilemap = GameObject.GetComponent<Tilemap>();
            BuildColliders();
        }

        public void BuildColliders()
        {
            CollisionRects.Clear();
            if (_tilemap == null || _tilemap.MapData == null) return;

            Vector2 scale = new Vector2(Transform.LocalScale.X, Transform.LocalScale.Y);
            
            float totalMapWidth = _tilemap.Columns * _tilemap.TileWidth * scale.X;
            float totalMapHeight = _tilemap.Rows * _tilemap.TileHeight * scale.Y;

            Vector2 startPos = new Vector2(
                Transform.LocalPosition.X - (totalMapWidth / 2f),
                Transform.LocalPosition.Y - (totalMapHeight / 2f)
            );

            bool[,] visited = new bool[_tilemap.Columns, _tilemap.Rows];

            for (int y = 0; y < _tilemap.Rows; y++)
            {
                for (int x = 0; x < _tilemap.Columns; x++)
                {
                    if (_tilemap.GetTile(x, y) != -1 && !visited[x, y])
                    {
                        int width = 1;
                        while (x + width < _tilemap.Columns && _tilemap.GetTile(x + width, y) != -1 && !visited[x + width, y])
                        {
                            width++;
                        }

                        int height = 1;
                        bool canExpandDown = true;
                        while (y + height < _tilemap.Rows && canExpandDown)
                        {
                            for (int ix = 0; ix < width; ix++)
                            {
                                if (_tilemap.GetTile(x + ix, y + height) == -1 || visited[x + ix, y + height])
                                {
                                    canExpandDown = false;
                                    break;
                                }
                            }
                            if (canExpandDown) height++;
                        }

                        for (int iy = 0; iy < height; iy++)
                        {
                            for (int ix = 0; ix < width; ix++)
                            {
                                visited[x + ix, y + iy] = true;
                            }
                        }

                        // MATEMÁTICA FLOAT PURA
                        float rectX = startPos.X + (x * _tilemap.TileWidth * scale.X);
                        float rectY = startPos.Y + (y * _tilemap.TileHeight * scale.Y);
                        float rectWidth = width * _tilemap.TileWidth * scale.X;
                        float rectHeight = height * _tilemap.TileHeight * scale.Y;

                        CollisionRects.Add(new BoundingBox(
                            new Vector3(rectX, rectY, 0),
                            new Vector3(rectX + rectWidth, rectY + rectHeight, 1)
                        ));
                    }
                }
            }
        }
    }
}