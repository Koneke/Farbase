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

        public void Select(Vector2i position)
        {
            Tile t = fbGame.World.Map.At(position);
            if(t.Unit != null)
                selection = new UnitSelection(t.Unit);
            else
                selection = new TileSelection(t);
        }

        public fbCamera Cam;

        public fbInterface(fbApplication app)
        {
            this.app = app;
            Cam = new fbCamera(app);
        }

        public void DrawUI()
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
                    .Skip(Math.Max(0, game.Log.Count - 3))
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
