using Engine.Core.Modules;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.Core;

public abstract class EngineApp : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    protected EngineApp()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        Screen.Initialize(_graphics);
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        // Aqui futuramente injetaremos o AssetManager nativo
    }

    protected override void Update(GameTime gameTime)
    {
        Time.Update(gameTime);
        Input.Update();
        
        SceneManager.Update(gameTime);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        // Agora o RenderManager assume o controle absoluto da renderização da Cena
        RenderManager.Draw(_spriteBatch);

        base.Draw(gameTime);
    }
}