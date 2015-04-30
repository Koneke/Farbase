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
        public fbInterface UI;

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

            //this shit needs to be moved...
            Engine.Textures = new Dictionary<string, Texture2D>();
            Engine.LoadTexture("grid", "gfx/@/grid.png");
            Engine.LoadTexture("background", "gfx/background-dark.jpg");
            Engine.LoadTexture("cross", "gfx/cross.png");

            Engine.LoadTexture("scout", "gfx/@/scout.png");
            Engine.LoadTexture("worker", "gfx/@/worker.png");

            Engine.LoadTexture("station", "gfx/@/station.png");
            Engine.LoadTexture("station-bg", "gfx/@/station-bg.png");
            Engine.LoadTexture("planet", "gfx/@/planet.png");
            Engine.LoadTexture("planet-bg", "gfx/@/planet-bg.png");

            Engine.LoadTexture(
                "move-dingy",
                "gfx/@/ui-move-dingy.png"
            );
            Engine.LoadTexture(
                "strength-dingy",
                "gfx/@/ui-strength-dingy.png"
            );

            Engine.LoadTexture("selection", "gfx/@/ui-selection.png");

            Engine.LoadTexture("blank", "gfx/@/blank.png");

            Engine.LoadTexture("check", "gfx/@/ui-check.png");

            Engine.DefaultFont = new Font();
            Engine.DefaultFont.FontSheet = Engine.LoadTexture("font", "gfx/font.png");
            Engine.DefaultFont.CharSize = new Vector2(8);
            
            Game = new fbGame(Engine);
            UI = new fbInterface(Game, Engine);

            Engine.StartNetClient();
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
            UI.Update();
            Game.Update();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            UI.Draw();
            Engine.Render();
            base.Draw(gameTime);
        }
    }
}
