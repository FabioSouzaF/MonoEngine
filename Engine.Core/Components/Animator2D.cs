using System.Collections.Generic;
using Engine.Core.Entities;
using Microsoft.Xna.Framework;

namespace Engine.Core.Components
{
    // A classe precisa ser pública e ter um construtor vazio para o Inspetor ler!
    public class Animation
    {
        public string Name = "Nova Animação";
        
        // NOVIDADE: A imagem que essa animação usa! (Se ficar vazio, ele usa a imagem que já está no objeto)
        public string TexturePath = ""; 
        
        public int StartFrame = 0;
        public int EndFrame = 0;
        public int Row = 0;
        public int FramesPerSecond = 10;
        public bool Loop = true;

        public Animation() { } 
    }

    public class Animator2D : Component
    {
        public int FrameWidth = 16;
        public int FrameHeight = 16;

        // --- A VITRINE PARA O SEU INSPETOR ---
        // Agora você pode adicionar elementos a esta lista direto pelo ImGui ou JSON!
        public List<Animation> Animations = new List<Animation>();

        private Animation _currentAnimation;
        private int _currentFrame = 0;
        private float _timer = 0f;
        private SpriteRenderer _spriteRenderer;

        public override void Start()
        {
            _spriteRenderer = GameObject.GetComponent<SpriteRenderer>();

            // Se você configurou animações no Editor, ele dá Play na primeira automaticamente!
            if (Animations.Count > 0)
            {
                Play(Animations[0].Name);
            }
            else
            {
                UpdateSpriteRectangle();
            }
        }

        public void Play(string name)
        {
            if (_currentAnimation != null && _currentAnimation.Name == name) return; 
            
            var anim = Animations.Find(a => a.Name == name);

            if (anim != null)
            {
                _currentAnimation = anim;
                _currentFrame = anim.StartFrame; 
                _timer = 0f;
                
                // --- NOVIDADE: Troca a imagem se a animação pedir! ---
                if (!string.IsNullOrEmpty(anim.TexturePath) && _spriteRenderer != null)
                {
                    _spriteRenderer.SetTexture(anim.TexturePath);
                }

                UpdateSpriteRectangle();
            }
            else
            {
                System.Console.WriteLine($"[AVISO] Animator2D tentou tocar '{name}', mas não existe na lista!");
            }
        }

        public override void Update(GameTime gameTime)
        {
            if (_spriteRenderer == null || _currentAnimation == null) return;

            _timer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            float timePerFrame = 1f / _currentAnimation.FramesPerSecond;

            if (_timer >= timePerFrame)
            {
                _timer -= timePerFrame;
                _currentFrame++;

                if (_currentFrame > _currentAnimation.EndFrame)
                {
                    if (_currentAnimation.Loop) _currentFrame = _currentAnimation.StartFrame;
                    else _currentFrame = _currentAnimation.EndFrame; 
                }

                UpdateSpriteRectangle();
            }
        }

        private void UpdateSpriteRectangle()
        {
            if (_spriteRenderer != null && _currentAnimation != null)
            {
                int x = _currentFrame * FrameWidth;
                int y = _currentAnimation.Row * FrameHeight;
                _spriteRenderer.SourceRectangle = new Rectangle(x, y, FrameWidth, FrameHeight);
            }
        }
    }
}