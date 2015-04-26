using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Farbase
{
    public class fbApplication : Game
    {
        public GraphicsDeviceManager Graphics;
        public SpriteBatch SpriteBatch;

        public fbEngine Engine;
        public fbGame Game;

        public fbApplication()
        {
            Graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            base.Initialize();

            Engine = new fbEngine(this);

            Engine.Textures = new Dictionary<string, Texture2D>();
            Engine.LoadTexture("grid", "gfx/@/grid.png");
            Engine.LoadTexture("scout", "gfx/@/scout.png");
            Engine.LoadTexture("background", "gfx/background-dark.jpg");
            Engine.LoadTexture("cross", "gfx/cross.png");
            Engine.LoadTexture("station", "gfx/@/station.png");
            Engine.LoadTexture("station-bg", "gfx/@/station-bg.png");
            Engine.LoadTexture("planet", "gfx/@/planet.png");
            Engine.LoadTexture("planet-bg", "gfx/@/planet-bg.png");
            
            Game = new fbGame(Engine);
        }

        protected override void LoadContent()
        {
            SpriteBatch = new SpriteBatch(GraphicsDevice);
        }

        protected override void UnloadContent()
        {
        }

        protected override void Update(GameTime gameTime)
        {
            Engine.Update();
            Game.Update();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            Game.Draw();
            Engine.Render();
            base.Draw(gameTime);
        }
    }
}
