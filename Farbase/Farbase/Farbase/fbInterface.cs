using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Farbase
{
    public interface ISelection
    {
        Vector2i GetSelection();
    }

    public class TileSelection : ISelection
    {
        private Tile selected;

        public TileSelection(Tile t)
        {
            selected = t;
        }

        public Vector2i GetSelection()
        {
            return new Vector2i(
                (int)selected.Position.X,
                (int)selected.Position.Y
            );
        }
    }

    public class UnitSelection : ISelection
    {
        private Unit selected;

        public UnitSelection(Unit u)
        {
            selected = u;
        }

        public Vector2i GetSelection()
        {
            return new Vector2i(
                (int)selected.Position.X,
                (int)selected.Position.Y
            );
        }
    }

    public class fbInterface
    {
        private fbApplication app;

        private fbGame game { get { return app.Game; } }
        private fbEngine engine { get { return app.Engine; } }

        private const int tileSize = 16;

        private ISelection selection;
        public Tile SelectedTile
        {
            get
            {
                if (selection == null) return null;
                return fbGame.World.Map.At(selection.GetSelection());
            }
        }
        public Unit SelectedUnit
        {
            get
            {
                if (selection == null) return null;
                return SelectedTile.Unit;
            }
        }

        public fbCamera Cam;

        public fbInterface(fbApplication app)
        {
            this.app = app;
            Cam = new fbCamera(app);
        }

        public void Select(Vector2i position)
        {
            Tile t = fbGame.World.Map.At(position);
            if(t.Unit != null)
                selection = new UnitSelection(t.Unit);
            else
                selection = new TileSelection(t);
        }

        public void Draw()
        {
            DrawBackground();

            if (fbGame.World == null) return;

            DrawMap();
            DrawUI();
        }

        private void DrawBackground()
        {
            Vector2 position =
                -engine.GetTextureSize("background") / 2f +
                engine.GetSize() / 2f;
            position -= Cam.Camera.Position / 10f;

            engine.Draw(
                engine.GetTexture("background"),
                new fbRectangle(position, new Vector2(-1)),
                new Color(0.3f, 0.3f, 0.3f),
                1000
            );
        }

        private void DrawGrid()
        {
            for (int x = 0; x < fbGame.World.Map.Width; x++)
            for (int y = 0; y < fbGame.World.Map.Height; y++)
            {
                engine.Draw(
                    engine.GetTexture("grid"),
                    Cam.WorldToScreen(
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
            Tile t = fbGame.World.Map.At(x, y);
            fbRectangle destination =
                Cam.WorldToScreen(
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

            if (t.Planet != null)
            {
                engine.Draw(
                    t.Unit == null
                    ? engine.GetTexture("planet")
                    : engine.GetTexture("planet-bg"),
                    destination
                );
            }

            if (t.Unit != null)
                DrawUnit(t.Unit);

            if(
                (SelectedUnit != null && t.Unit == SelectedUnit) ||
                (SelectedTile == t && t.Station != null)
            )
                engine.Draw(
                    engine.GetTexture("selection"),
                    destination
                );
        }

        private void DrawUnit(Unit u)
        {
            fbRectangle destination =
                Cam.WorldToScreen(
                    new fbRectangle(
                        u.Position * tileSize,
                        new Vector2(tileSize)
                    )
                );

            engine.Draw(
                u.UnitType.Texture,
                destination,
                fbGame.World.Players[u.Owner].Color
            );

            for (int i = 0; i < u.Moves; i++)
            {
                Vector2 dingySize = engine.GetTextureSize("move-dingy");

                engine.Draw(
                    engine.GetTexture("move-dingy"),
                    Cam.WorldToScreen(
                        new fbRectangle(
                            u.Position * tileSize
                                + new Vector2(dingySize.X * i, 0),
                            dingySize
                        )
                    )
                );
            }

            for (int i = 0; i < u.Strength; i++)
            {
                Vector2 dingySize = engine.GetTextureSize("strength-dingy");

                engine.Draw(
                    engine.GetTexture("strength-dingy"),
                    Cam.WorldToScreen(
                        new fbRectangle(
                            u.Position * tileSize + new Vector2(0, tileSize)
                                - new Vector2(0, dingySize.Y * (i + 1)),
                            dingySize
                        )
                    )
                );
            }
        }

        private void DrawMap()
        {
            DrawGrid();
            for (int x = 0; x < fbGame.World.Map.Width; x++)
            for (int y = 0; y < fbGame.World.Map.Height; y++)
                DrawTile(x, y);
        }

        private void DrawUI()
        {
            new TextCall(
                string.Format(
                    "Hi, I am {0}<{1}>",
                    fbGame.World.Players[game.We].Name,
                    game.We
                    ),
                engine.DefaultFont,
                new Vector2(10)
            ).Draw(engine);

            if (fbGame.World.PlayerIDs.Count > 0)
            {
                Player current = fbGame.World.Players
                    [fbGame.World.PlayerIDs[fbGame.World.CurrentPlayerIndex]];

                new TextCall(
                    string.Format(
                        "Current player: {0}<{1}>",
                        current.Name,
                        current.ID
                    ),
                    engine.DefaultFont,
                    new Vector2(10, 20)
                ).Draw(engine);
            }

            List<string> logTail;
            lock (game.Log)
            {
                logTail = game.Log
                    .Skip(Math.Max(0, game.Log.Count - 10))
                    .ToList();
            }
            logTail.Reverse();

            Vector2 position = new Vector2(10, engine.GetSize().Y - 10);
            foreach (string message in logTail)
            {
                position -= new Vector2(0, engine.DefaultFont.CharSize.Y + 1);
                new TextCall(
                    message,
                    engine.DefaultFont,
                    position
                ).Draw(engine);
            }
        }
    }

    public class fbCamera
    {
        public fbRectangle Camera;
        private fbApplication app;
        private fbEngine engine { get { return app.Engine; } }

        public fbCamera(fbApplication app)
        {
            this.app = app;
            Camera = new fbRectangle(
                Vector2.Zero,
                engine.GetSize()
            );
        }

        private float cameraScaling
        {
            get { return Camera.Size.X / engine.GetSize().X; }
        }

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
}
