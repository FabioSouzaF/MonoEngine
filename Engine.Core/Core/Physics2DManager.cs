using System;
using System.Collections.Generic;
using Engine.Core.Components;
using Engine.Core.Entities;
using Microsoft.Xna.Framework;

namespace Engine.Core.Modules
{
    public static class Physics2DManager
    {
        public static bool Enabled = true; 
        public static Vector2 Gravity = new Vector2(0, 980f);

        private static HashSet<(BoxCollider2D, BoxCollider2D)> _previousCollisions = new HashSet<(BoxCollider2D, BoxCollider2D)>();
        // NOVIDADE: Memória de quem bateu no cenário!
        private static HashSet<(BoxCollider2D, TilemapCollider2D)> _previousTileCollisions = new HashSet<(BoxCollider2D, TilemapCollider2D)>();

        public static void Update(GameTime gameTime, Scene activeScene)
        {
            if (!Enabled || activeScene == null) return;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            var bodies = new List<RigidBody2D>();
            var colliders = new List<BoxCollider2D>();
            var tileColliders = new List<TilemapCollider2D>(); 

            foreach (var root in activeScene.RootObjects) GatherComponents(root, bodies, colliders, tileColliders);

            var currentCollisions = new HashSet<(BoxCollider2D, BoxCollider2D)>();
            var currentTileCollisions = new HashSet<(BoxCollider2D, TilemapCollider2D)>();

            // --- 1. EVENTOS: BOX VS BOX ---
            for (int i = 0; i < colliders.Count; i++)
            {
                var colA = colliders[i];
                if (!colA.Enabled || !colA.GameObject.IsActive) continue;

                for (int j = i + 1; j < colliders.Count; j++) 
                {
                    var colB = colliders[j];
                    if (!colB.Enabled || !colB.GameObject.IsActive) continue;

                    if (colA.Bounds.Intersects(colB.Bounds))
                    {
                        var pair = colA.GetHashCode() < colB.GetHashCode() ? (colA, colB) : (colB, colA);
                        currentCollisions.Add(pair);

                        if (!_previousCollisions.Contains(pair))
                        {
                            bool isTriggerEvent = colA.IsTrigger || colB.IsTrigger;
                            foreach (var compA in colA.GameObject.Components)
                            {
                                if (isTriggerEvent) compA.OnTriggerEnter(colB);
                                else compA.OnCollisionEnter(colB);
                            }
                            foreach (var compB in colB.GameObject.Components)
                            {
                                if (isTriggerEvent) compB.OnTriggerEnter(colA);
                                else compB.OnCollisionEnter(colA);
                            }
                        }
                    }
                }   
            }

            // --- 2.5 EVENTOS: BOX VS TILEMAP (O NOVO RADAR!) ---
            foreach (var colA in colliders)
            {
                if (!colA.Enabled || !colA.GameObject.IsActive) continue;
                BoundingBox rectA = colA.Bounds; // Usa a caixa bruta para disparar o gatilho

                foreach (var tCol in tileColliders)
                {
                    if (!tCol.Enabled || !tCol.GameObject.IsActive) continue;

                    bool isIntersecting = false;
                    foreach (var tileRect in tCol.CollisionRects)
                    {
                        if (rectA.Intersects(tileRect))
                        {
                            isIntersecting = true;
                            break;
                        }
                    }

                    if (isIntersecting)
                    {
                        var pair = (colA, tCol);
                        currentTileCollisions.Add(pair);

                        if (!_previousTileCollisions.Contains(pair))
                        {
                            bool isTriggerEvent = colA.IsTrigger;
                            foreach (var compA in colA.GameObject.Components)
                            {
                                if (isTriggerEvent) compA.OnTriggerEnter(tCol);
                                else compA.OnCollisionEnter(tCol);
                            }
                        }
                    }
                }
            }

            // --- 3. MOVIMENTO E RESOLUÇÃO DE FÍSICA ---
            foreach (var rb in bodies)
            {
                if (rb.IsKinematic) continue;

                var col = rb.GameObject.GetComponent<BoxCollider2D>();
                rb.Velocity += Gravity * rb.GravityScale * dt;

                if (col == null || !col.Enabled || col.IsTrigger)
                {
                    var p = rb.Transform.LocalPosition;
                    p.X += rb.Velocity.X * dt;
                    p.Y += rb.Velocity.Y * dt;
                    rb.Transform.LocalPosition = p;
                    continue;
                }

                // EIXO X
                var pos = rb.Transform.LocalPosition;
                pos.X += rb.Velocity.X * dt;
                rb.Transform.LocalPosition = pos;
                ResolveAxis(col, rb, colliders, tileColliders, true); 

                // EIXO Y
                pos = rb.Transform.LocalPosition;
                pos.Y += rb.Velocity.Y * dt;
                rb.Transform.LocalPosition = pos;
                ResolveAxis(col, rb, colliders, tileColliders, false); 
            }

            _previousCollisions = currentCollisions;
            _previousTileCollisions = currentTileCollisions;
        }

        private static void ResolveAxis(BoxCollider2D colA, RigidBody2D rbA, List<BoxCollider2D> colliders, List<TilemapCollider2D> tileColliders, bool isXAxis)
        {
            BoundingBox rectA = GetLiveBounds(colA, rbA);

            foreach (var tCol in tileColliders)
            {
                if (!tCol.Enabled || !tCol.GameObject.IsActive) continue;
                foreach (var tileRect in tCol.CollisionRects)
                {
                    if (rectA.Intersects(tileRect))
                    {
                        ApplyAxisPush(rectA, tileRect, rbA, isXAxis);
                        rectA = GetLiveBounds(colA, rbA); 
                    }
                }
            }

            foreach (var colB in colliders)
            {
                if (colA == colB || !colB.Enabled || !colB.GameObject.IsActive || colB.IsTrigger) continue;
                BoundingBox rectB = colB.Bounds;

                if (rectA.Intersects(rectB))
                {
                    ApplyAxisPush(rectA, rectB, rbA, isXAxis);
                    rectA = GetLiveBounds(colA, rbA);
                }
            }
        }

        private static void ApplyAxisPush(BoundingBox rectA, BoundingBox other, RigidBody2D rb, bool isXAxis)
        {
            float overlapLeft = rectA.Max.X - other.Min.X;
            float overlapRight = other.Max.X - rectA.Min.X;
            float overlapTop = rectA.Max.Y - other.Min.Y;
            float overlapBottom = other.Max.Y - rectA.Min.Y;

            if (overlapLeft <= 0.001f || overlapRight <= 0.001f || overlapTop <= 0.001f || overlapBottom <= 0.001f) return;

            float minOverlapX = Math.Min(overlapLeft, overlapRight);
            float minOverlapY = Math.Min(overlapTop, overlapBottom);

            var pos = rb.Transform.LocalPosition;

            if (isXAxis)
            {
                if (minOverlapY < minOverlapX) return; 

                if (overlapLeft < overlapRight) { pos.X -= overlapLeft; rb.Velocity.X = 0; }
                else { pos.X += overlapRight; rb.Velocity.X = 0; }
            }
            else
            {
                if (minOverlapX < minOverlapY) return;

                if (overlapTop < overlapBottom) { pos.Y -= overlapTop; rb.Velocity.Y = 0; }
                else 
                { 
                    pos.Y += overlapBottom; 
                    if (rb.Velocity.Y < 0) rb.Velocity.Y = 0; 
                }
            }

            rb.Transform.LocalPosition = pos;
        }

        private static BoundingBox GetLiveBounds(BoxCollider2D col, RigidBody2D rb)
        {
            float w = col.Size.X * rb.Transform.LocalScale.X;
            float h = col.Size.Y * rb.Transform.LocalScale.Y;
            float x = rb.Transform.LocalPosition.X + col.Offset.X - (w / 2f);
            float y = rb.Transform.LocalPosition.Y + col.Offset.Y - (h / 2f);
            
            return new BoundingBox(new Vector3(x, y, 0), new Vector3(x + w, y + h, 1));
        }

        private static void GatherComponents(GameObject obj, List<RigidBody2D> bodies, List<BoxCollider2D> colliders, List<TilemapCollider2D> tileColliders)
        {
            if (!obj.IsActive) return;
            var rb = obj.GetComponent<RigidBody2D>();
            if (rb != null && rb.Enabled) bodies.Add(rb);
            var col = obj.GetComponent<BoxCollider2D>();
            if (col != null && col.Enabled) colliders.Add(col);
            var tCol = obj.GetComponent<TilemapCollider2D>();
            if (tCol != null && tCol.Enabled) tileColliders.Add(tCol);
            
            foreach (var child in obj.Children) GatherComponents(child, bodies, colliders, tileColliders);
        }
    }
}