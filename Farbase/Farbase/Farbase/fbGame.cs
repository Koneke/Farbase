using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Farbase
{
    public class Player
    {
        public string Name;
        public Color Color;
        public List<Unit> Units;
        public int Money;

        public Player(string name, Color color)
        {
            Name = name;
            Color = color;

            Units = new List<Unit>();
        }
    }

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
                map[x, y] = new Tile(x, y);
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
        public Planet Planet;
        public Vector2 Position;

        public Tile(int x, int y)
        {
            Position = new Vector2(x, y);
        }
    }

    public class UnitType
    {
        private static Dictionary<string, UnitType> types =
            new Dictionary<string, UnitType>();

        public static void RegisterType(string name, UnitType type)
        {
            name = name.ToLower();
            types.Add(name, type);
        }

        public static UnitType GetType(string name)
        {
            name = name.ToLower();
            return types[name];
        }

        public Texture2D Texture;
        public int Moves;
    }

    public class Unit
    {
        public static fbGame Game;

        public UnitType UnitType;
        public Player Owner;

        public Vector2 Position;
        public Tile Tile
            { get { return Game.Map.At(Position); } }

        public int Moves;

        public Unit(
            UnitType unitType,
            Player owner,
            Vector2 position
        ) {
            UnitType = unitType;
            Owner = owner;
            Position = position;
        }

        public void Replenish()
        {
            Moves = UnitType.Moves;

            //workers generate cash if they start the turn on a planet
            if(UnitType == UnitType.GetType("worker"))
                if (Tile.Planet != null)
                    Owner.Money += 10;
        }

        public bool CanMoveTo(Vector2 position)
        {
            //only bad condition atm is collision
            if (Game.Map.At(position).Unit == null)
                return true;
            return false;
        }

        public void MoveTo(Vector2 position)
        {
            Tile.Unit = null;
            Position = position;
            Game.Map.At(position).Unit = this;
        }
    }

    public class Station
    {
        public static fbGame Game;

        public Vector2 Position;
        public Tile Tile
            { get { return Game.Map.At(Position); } }
    }

    public class Planet
    {
        public static fbGame Game;

        public Vector2 Position;
        public Tile Tile
            { get { return Game.Map.At(Position); } }
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

        public void DrawUI()
        {
            new TextCall(
                "Current player: " + game.CurrentPlayer.Name,
                engine.DefaultFont,
                new Vector2(10)
            ).Draw(engine);

            new TextCall(
                "Money: " + game.CurrentPlayer.Money + "$",
                engine.DefaultFont,
                new Vector2(10, 20)
            ).Draw(engine);
        }
    }

    public class fbGame
    {
        private fbEngine engine;
        private fbInterface ui;

        public List<Player> Players;
        public int CurrentPlayerIndex;
        public Player CurrentPlayer
            { get { return Players[CurrentPlayerIndex]; } }

        public Map Map;
        private const int tileSize = 16;

        public Tile Selection;
        public Unit SelectedUnit
        {
            get
            {
                if (Selection == null) return null;
                return Selection.Unit;
            }
            set { Selection = value.Tile; }
        }
        public Station SelectedStation
        {
            get
            {
                if (Selection == null) return null;
                return Selection.Station;
            }
            set { Selection = value.Tile; }
        }

        public fbGame(fbEngine engine)
        {
            this.engine = engine;
            Unit.Game = this;
            Station.Game = this;
            Planet.Game = this;
            Initialize();
        }

        public void Initialize()
        {
            engine.SetSize(1280, 720);

            ui = new fbInterface(this, engine);

            Players = new List<Player>();
            Players.Add(new Player("Lukas", Color.CornflowerBlue));
            Players.Add(new Player("Barbarians", Color.Red));

            UnitType scout = new UnitType();
            scout.Texture = engine.GetTexture("scout");
            scout.Moves = 2;
            UnitType.RegisterType("scout", scout);

            UnitType worker = new UnitType();
            worker.Texture = engine.GetTexture("worker");
            worker.Moves = 1;
            UnitType.RegisterType("worker", worker);

            Map = new Map(80, 45);
            SpawnUnit(scout, Players[0], new Vector2(8, 12));

            Map.At(8, 12).Station = new Station();
            Map.At(9, 12).Station = new Station();

            Map.At(14, 14).Planet = new Planet();

            SpawnUnit(worker, Players[0], new Vector2(10, 12));

            SpawnUnit(worker, Players[1], new Vector2(16, 14));
        }

        public void SpawnUnit(
            UnitType type,
            Player owner,
            Vector2 position
        ) {
            Unit u = new Unit(
                type,
                owner,
                position
            );
            u.Replenish();
            Map.At(position).Unit = u;
            owner.Units.Add(u);
        }

        public void PassTurn()
        {
            CurrentPlayerIndex =
                (CurrentPlayerIndex + 1) % Players.Count;

            foreach (Unit u in CurrentPlayer.Units)
                u.Replenish();
        }

        public void Update()
        {
            if (engine.KeyPressed(Keys.Escape))
                engine.Exit();

            ui.UpdateCamera();

            if (engine.KeyPressed(Keys.Enter))
                PassTurn();

            if (SelectedUnit != null && SelectedUnit.Owner == CurrentPlayer)
            {
                Vector2 moveOrder = Vector2.Zero;

                if (engine.KeyPressed(Keys.NumPad2))
                    moveOrder = new Vector2(0, 1);
                if (engine.KeyPressed(Keys.NumPad4))
                    moveOrder = new Vector2(-1, 0);
                if (engine.KeyPressed(Keys.NumPad8))
                    moveOrder = new Vector2(0, -1);
                if (engine.KeyPressed(Keys.NumPad6))
                    moveOrder = new Vector2(1, 0);

                if (engine.KeyPressed(Keys.NumPad1))
                    moveOrder = new Vector2(-1, 1);
                if (engine.KeyPressed(Keys.NumPad7))
                    moveOrder = new Vector2(-1, -1);
                if (engine.KeyPressed(Keys.NumPad9))
                    moveOrder = new Vector2(1, -1);
                if (engine.KeyPressed(Keys.NumPad3))
                    moveOrder = new Vector2(1, 1);

                if (moveOrder != Vector2.Zero && SelectedUnit.Moves > 0)
                {
                    Unit u = SelectedUnit;

                    if(u.CanMoveTo(u.Position + moveOrder))
                    {
                        SelectedUnit.Moves -= 1;
                        SelectedUnit.MoveTo(SelectedUnit.Position + moveOrder);

                        //make sure to reselect the unit.
                        //since in reality, the UNIT isn't selected, the TILE is
                        //by moving the unit, it would no longer be selected,
                        //so we have to reselect it.
                        SelectedUnit = u;
                    }
                }
            }

            if (SelectedUnit != null)
            {
                if (engine.KeyPressed(Keys.OemPeriod))
                {
                    int index = CurrentPlayer.Units.IndexOf(SelectedUnit);
                    index = (index + 1) % CurrentPlayer.Units.Count;
                    SelectedUnit = CurrentPlayer.Units[index];
                }

                if (engine.KeyPressed(Keys.OemComma))
                {
                    int index = CurrentPlayer.Units.IndexOf(SelectedUnit);
                    index = (index + CurrentPlayer.Units.Count - 1)
                        % CurrentPlayer.Units.Count;
                    SelectedUnit = CurrentPlayer.Units[index];
                }
            }

            if (engine.KeyPressed(Keys.W))
                if (
                    SelectedStation != null &&
                    SelectedUnit == null &&
                    CurrentPlayer.Money >= 25
                ) {
                    SpawnUnit(
                        UnitType.GetType("worker"),
                        CurrentPlayer,
                        Selection.Position
                    );
                    CurrentPlayer.Money -= 25;
                }

            if (engine.KeyPressed(Keys.B))
                if (
                    SelectedStation != null &&
                    SelectedUnit == null &&
                    CurrentPlayer.Money >= 45
                ) {
                    SpawnUnit(
                        UnitType.GetType("scout"),
                        CurrentPlayer,
                        Selection.Position
                    );
                    CurrentPlayer.Money -= 45;
                }

            if (engine.ButtonPressed(0))
            {
                if (engine.Active && engine.MouseInside)
                {
                    Vector2 square = ScreenToGrid(engine.MousePosition);
                    Selection = Map.At(square);
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
                new Color(0.3f, 0.3f, 0.3f),
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

            //if we have anything fun selected, show it.
            //tiles themselves might be interesting later, but not for now.
            if(
                Selection == t &&
                (
                    t.Unit != null ||
                    t.Station != null ||
                    t.Planet != null
                )
            )
                engine.Draw(
                    engine.GetTexture("selection"),
                    destination
                );
        }

        private void DrawUnit(Unit u)
        {
            fbRectangle destination =
                ui.WorldToScreen(
                    new fbRectangle(
                        u.Position * tileSize,
                        new Vector2(tileSize)
                    )
                );

            engine.Draw(
                u.UnitType.Texture,
                destination,
                u.Owner.Color
            );

            for (int i = 0; i < u.Moves; i++)
            {
                Vector2 dingySize = engine.GetTextureSize("move-dingy");

                engine.Draw(
                    engine.GetTexture("move-dingy"),
                    ui.WorldToScreen(
                        new fbRectangle(
                            u.Position * tileSize
                                + new Vector2(dingySize.X * i, 0),
                            dingySize
                        )
                    )
                );
            }
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
            ui.DrawUI();
        }
    }

}
