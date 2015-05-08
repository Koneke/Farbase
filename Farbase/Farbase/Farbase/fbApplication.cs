using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            //IsMouseVisible = true;
            IsFixedTimeStep = false;
        }

        protected override void Initialize()
        {
            base.Initialize();
            SpriteBatch = new SpriteBatch(GraphicsDevice);

            Engine = new fbEngine(this);
            Engine.SetSize(1280, 720);
            LoadTexturesFromFile("cfg/textures.cfg");

            Engine.DefaultFont = new Font();
            Engine.DefaultFont.FontSheet = Engine.GetTexture("font");
            Engine.DefaultFont.CharSize = new Vector2(8);

            NetMessage3.Setup();

            Game = new fbGame();
            Game.Ready = false;
            Game.SetupClientSideEventHandler(Engine);

            //eugh
            fbNetClient.Game = Game;

            UI = new fbInterface(Game, Engine);

            Engine.StartNetClient();
        }

        private void LoadTexturesFromFile(string path)
        {
            Engine.Textures = new Dictionary<string, Texture2D>();

            foreach (string ln in File.ReadLines(path))
            {
                if (ln == "") continue;

                string line = ln.ToLower();

                if (!line.Contains(',')) //we always need a name
                    throw new FormatException();

                List<string> split = line.Split(',').ToList();
                if (split.Count != 2)
                    throw new FormatException();

                string name = split[0]
                    .Trim(new[] {'\t', ' '});

                string trimmedpath = split[1]
                    .Trim(new[] {'\t', ' '});

                Engine.LoadTexture(name, trimmedpath);
            }
        }

        protected override void OnExiting(object sender, EventArgs args)
        {
            Engine.NetClient.Send(
                new NetMessage3(
                    NM3MessageType.client_disconnect,
                    Game.We
                )
            );

            if (Engine.NetClient != null)
                Engine.NetClient.ShouldDie = true;
        }

        protected override void Update(GameTime gameTime)
        {
            Engine.Update(gameTime);
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
