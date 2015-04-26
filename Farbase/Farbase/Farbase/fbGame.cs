using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Farbase
{
    public class Map
    {
        private Tile[,] map;
        public Vector2 Size;

        public Map(int w, int h)
        {
            Size = new Vector2(w, h);

            map = new Tile[w, h];
            for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                map[x, y] = new Tile();
        }

        public Tile At(int x, int y)
        {
            return map[x, y];
        }

        public Tile At(Vector2 position)
        {
            return map[
                (int)position.X,
                (int)position.Y
            ];
        }
    }

    public class Tile
    {
        public bool Tick;
        public Unit Unit;
        public Station Station;
    }

    //what we spawn units from
    //the unit class itself represents an instance of a unit
    /*public class UnitTemplate
    {
    }*/

    public class Unit
    {
        public Texture2D Texture;
    }

    public class Station
    {
        public Texture2D Texture;
    }

    public class fbInterface
    {
        private fbGame game;
        private fbEngine engine;

        public fbInterface(fbGame game, fbEngine engine)
        {
            this.game = game;
            this.engine = engine;
            Camera = new fbRectangle(Vector2.Zero, engine.GetSize());
        }

        public fbRectangle Camera;
        private float cameraScaling
            { get { return Camera.Size.X / engine.GetSize().X; } }

        private const float keyboardScrollSpeed = 10f;
        private const float mouseScrollSpeed = 8f;
        private const float edgeSize = 10f;

        //position ON SCREEN
        private void ZoomAt(Vector2 position, float amount)
        {
            amount *= -1;

            if (
                position.X < 0 || position.X > engine.GetSize().X ||
                position.Y < 0 || position.Y > engine.GetSize().Y
            )
                throw new ArgumentException("Bad zoom point.");

            Vector2 deltaSize = Camera.Size - Camera.Size + new Vector2(amount);
            deltaSize.Y = deltaSize.X / engine.GetAspectRatio();

            Vector2 bias = position / engine.GetSize();
            Camera.Position -= deltaSize * bias;
            Camera.Size += deltaSize;
        }

        public void UpdateCamera()
        {
            if (engine.KeyPressed(Keys.OemPlus))
                ZoomAt(engine.GetSize() / 2f, 100f);

            if (engine.KeyPressed(Keys.OemMinus))
                ZoomAt(engine.GetSize() / 2f, -100f);

            Vector2 keyboardScroll = Vector2.Zero;
            if (engine.KeyDown(Keys.Right)) keyboardScroll.X += 1;
            if (engine.KeyDown(Keys.Left)) keyboardScroll.X -= 1;
            if (engine.KeyDown(Keys.Up)) keyboardScroll.Y -= 1;
            if (engine.KeyDown(Keys.Down)) keyboardScroll.Y += 1;

            Camera.Position +=
                keyboardScroll * keyboardScrollSpeed * cameraScaling;

            ZoomAt(engine.MousePosition, engine.MouseWheelDelta * 1f);

            Vector2 mouseScroll = Vector2.Zero;
            if (engine.Active)
            {
                if (engine.MousePosition.X < edgeSize) mouseScroll.X -= 1;
                if (engine.MousePosition.X > engine.GetSize().X - edgeSize)
                    mouseScroll.X += 1;
                if (engine.MousePosition.Y < edgeSize) mouseScroll.Y -= 1;
                if (engine.MousePosition.Y > engine.GetSize().Y - edgeSize)
                    mouseScroll.Y += 1;
            }

            Camera.Position +=
                mouseScroll * mouseScrollSpeed * cameraScaling;
        }

        private Rectangle WorldToScreen(Rectangle rectangle)
        {
            rectangle.X -= (int)Camera.Position.X;
            rectangle.Y -= (int)Camera.Position.Y;

            Vector2 scaleFactor = engine.GetSize() / Camera.Size;
            rectangle.X = (int)(rectangle.X * scaleFactor.X);
            rectangle.Y = (int)(rectangle.Y * scaleFactor.Y);
            rectangle.Width = (int)(rectangle.Width * scaleFactor.X);
            rectangle.Height = (int)(rectangle.Height * scaleFactor.Y);

            return rectangle;
        }

        public fbRectangle WorldToScreen(fbRectangle rectangle)
        {
            rectangle.Position -= Camera.Position;

            Vector2 scaleFactor = engine.GetSize() / Camera.Size;
            rectangle.Position *= scaleFactor;
            rectangle.Size *= scaleFactor;

            return rectangle;
        }

        public Vector2 ScreenToWorld(
            Vector2 position
        ) {
            Vector2 scaleFactor = engine.GetSize() / Camera.Size;
            return position / scaleFactor + Camera.Position;
        }

        public Rectangle ScreenToWorld(Rectangle rectangle)
        {
            Vector2 scaleFactor = engine.GetSize() / Camera.Size;
            rectangle.X = (int)(rectangle.X / scaleFactor.X);
            rectangle.Y = (int)(rectangle.Y / scaleFactor.Y);
            rectangle.Width = (int)(rectangle.Width / scaleFactor.X);
            rectangle.Height = (int)(rectangle.Height / scaleFactor.Y);

            rectangle.X += (int)Camera.Position.X;
            rectangle.Y += (int)Camera.Position.Y;

            return rectangle;
        }
    }

    public class fbGame
    {
        private fbEngine engine;
        private fbInterface ui;

        private Map Map;
        private const int tileSize = 48;

        public fbGame(fbEngine engine)
        {
            this.engine = engine;
            Initialize();
        }

        public void Initialize()
        {
            engine.SetSize(1280, 720);

            ui = new fbInterface(this, engine);

            Map = new Map(80, 45);
            Map.At(8, 12).Unit = new Unit();
            Map.At(8, 12).Unit.Texture = engine.GetTexture("scout");

            Map.At(8, 12).Station = new Station();
            Map.At(9, 12).Station = new Station();

            Map.At(10, 12).Unit = new Unit();
            Map.At(10, 12).Unit.Texture = engine.GetTexture("scout");
        }

        public void Update()
        {
            if (engine.KeyPressed(Keys.Escape))
                engine.Exit();

            ui.UpdateCamera();

            if (engine.ButtonPressed(0))
            {
                if (engine.Active && engine.MouseInside)
                {
                    Vector2 square = ScreenToGrid(engine.MousePosition);
                }
            }
        }

        private Vector2 ScreenToGrid(Vector2 position)
        {
            Vector2 worldPoint = ui.ScreenToWorld(position);
            Vector2 square =
                new Vector2(
                    worldPoint.X - (worldPoint.X % tileSize),
                    worldPoint.Y - (worldPoint.Y % tileSize)
                ) / tileSize;

            if(
                square.X >= 0 && square.X < Map.Size.X &&
                square.Y >= 0 && square.Y < Map.Size.Y
            )
                return square;

            throw new ArgumentException("Not inside screen.");
        }

        private void DrawBackground()
        {
            Vector2 position =
                -engine.GetTextureSize("background") / 2f +
                engine.GetSize() / 2f;
            position -= ui.Camera.Position / 10f;

            engine.Draw(
                engine.GetTexture("background"),
                new fbRectangle(position, new Vector2(-1)),
                Color.White,
                1000
            );
        }

        private void DrawGrid()
        {
            for (int x = 0; x < Map.Size.X; x++)
            for (int y = 0; y < Map.Size.Y; y++)
            {
                engine.Draw(
                    engine.GetTexture("grid"),
                    ui.WorldToScreen(
                        new fbRectangle(
                            new Vector2(x, y) * tileSize,
                            tileSize,
                            tileSize
                            )
                        )
                    );
            }
        }

        private void DrawTile(int x, int y)
        {
            Tile t = Map.At(x, y);
            fbRectangle destination =
                ui.WorldToScreen(
                    new fbRectangle(
                        new Vector2(x, y) * tileSize,
                        tileSize,
                        tileSize
                    )
                );

            if (t.Station != null)
            {
                engine.Draw(
                    t.Unit == null
                    ? engine.GetTexture("station")
                    : engine.GetTexture("station-bg"),
                    destination
                );
            }

            if (t.Unit != null)
                engine.Draw(
                    t.Unit.Texture,
                    destination,
                    Color.Red
                );
        }

        private void DrawMap()
        {
            DrawGrid();
            for (int x = 0; x < Map.Size.X; x++)
            for (int y = 0; y < Map.Size.Y; y++)
            {
                DrawTile(x, y);
            }
        }

        public void Draw()
        {
            DrawBackground();
            DrawMap();
        }
    }

}
