using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Farbase
{
    public class Player
    {
        public int ID;
        public string Name;
        public Color Color;
        public int Money;

        public Player(string name, int id, Color color)
        {
            Name = name;
            ID = id;
            Color = color;
        }
    }

    public class Map
    {
        private Tile[,] map;

        public Map(int w, int h)
        {
            Width = w;
            Height = h;

            map = new Tile[w, h];
            for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                map[x, y] = new Tile(x, y);
        }

        public int Width;
        public int Height;

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
            type.Name = name;
        }

        public static UnitType GetType(string name)
        {
            name = name.ToLower();
            return types[name];
        }

        public string Name;
        public Texture2D Texture;
        public int Moves;
        public int Attacks;
        public int Strength;
    }

    public class Unit
    {
        public static fbGame Game;

        public UnitType UnitType;
        public int Owner;

        public Vector2 Position;
        public Tile Tile
            { get { return Game.World.Map.At(Position); } }

        public int Moves;
        public int Attacks;
        public int Strength;

        public Unit(
            UnitType unitType,
            int owner,
            int x,
            int y
        ) : this(unitType, owner, new Vector2(x, y)) {
        }

        public Unit(
            UnitType unitType,
            int owner,
            Vector2 position
        ) {
            UnitType = unitType;
            Owner = owner;
            Position = position;
            Moves = UnitType.Moves;
            Attacks = UnitType.Attacks;
            Strength = UnitType.Strength;
        }

        public void Replenish()
        {
            Moves = UnitType.Moves;

            //workers generate cash if they start the turn on a planet
            if(UnitType == UnitType.GetType("worker"))
                if (Tile.Planet != null)
                    Game.World.Players[Owner].Money += 10;

            if (Tile.Station != null)
            {
                if (Strength < UnitType.Strength)
                    Strength++;
            }
        }

        public bool CanMoveTo(Vector2 position)
        {
            //only bad condition atm is collision
            if (Game.World.Map.At(position).Unit == null)
                return true;
            return false;
        }

        public bool CanAttack(Vector2 position)
        {
            Vector2 delta = Position - position;
            //only neighbours
            if (Math.Abs(delta.X) > 1 || Math.Abs(delta.Y) > 1)
                return false;

            if (Game.World.Map.At(position).Unit != null)
                if (Game.World.Map.At(position).Unit.Owner != Owner)
                    return true;

            return false;
        }

        public void MoveTo(Vector2 position)
        {
            bool selected = Game.SelectedUnit == this;

            Tile.Unit = null;
            Position = position;
            Game.World.Map.At(position).Unit = this;

            //automaintain selection
            if(selected)
                Game.SelectedUnit = this;
        }

        public Vector2 StepTowards(Vector2 goal)
        {
            //return the next move order for the path towards the given point.

            //todo: real pathfinding.
            //      at the moment, I think the fast naive approach is enough,
            //      since overall, there's only very, very few possible
            //      obstacles in the game (namely, only units).
            //      there's apparently a load of space in space, whoda thunk.

            Vector2 delta = goal - Position;
            delta.X = delta.X.Clamp(-1, 1);
            delta.Y = delta.Y.Clamp(-1, 1);

            Vector2 moveOrder = Vector2.Zero;

            if (delta.X == 0 && delta.Y == 0)
                return moveOrder;

            if (CanMoveTo(Position + delta))
                return delta;

            if (
                delta.X != 0 &&
                CanMoveTo(Position + new Vector2(delta.X, 0))
            )
                return new Vector2(delta.X, 0);

            if (CanMoveTo(Position + new Vector2(0, delta.Y)))
                return new Vector2(0, delta.Y);

            return Vector2.Zero;
        }

        public void Die()
        {
            //should not be client side
            throw new NotImplementedException();
            //Game.World.Players[Owner].Units.Remove(this);
            Tile.Unit = null;
        }

        public void Attack(Vector2 position)
        {
            //we KNOW there's a unit there, since we checked CanAttack.
            //... you checked CanAttack first, right?
            Unit target = Game.World.Map.At(position).Unit;
            int totalStrength = Strength + target.Strength;

            Random random = new Random();
            int roll = random.Next(totalStrength) + 1;

            Game.Log.Add(Strength + " vs. " + roll + "...");

            if (roll <= Strength)
            {
                //we win!
                Game.Log.Add("Victory!");
                target.Strength -= 1;

                if (target.Strength < 1)
                    target.Die();
            }
            else
            {
                Game.Log.Add("Defeat!");
                Strength -= 1;
            }
        }
    }

    public class Station
    {
        public static fbGame Game;

        public Vector2 Position;
        public Tile Tile
            { get { return Game.World.Map.At(Position); } }
    }

    public class Planet
    {
        public static fbGame Game;

        public Vector2 Position;
        public Tile Tile
            { get { return Game.World.Map.At(Position); } }
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
                string.Format(
                    "Hi, I am {0}<{1}>",
                    game.World.Players[game.We].Name,
                    game.We
                ),
                engine.DefaultFont,
                new Vector2(10)
            ).Draw(engine);

            if (game.World.PlayerIDs.Count > 0)
            {
                Player current = game.World.Players
                    [game.World.PlayerIDs[game.World.CurrentPlayerIndex]];

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

            /*if (game.CurrentPlayer != null)
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
            }*/

            List<string> logTail =
                game.Log
                    .Skip(Math.Max(0, game.Log.Count - 3))
                    .ToList();
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

    public class fbGame
    {
        private fbEngine engine;
        private fbInterface ui;

        //phase out
        /*public List<Player> Players;
        public int CurrentPlayerIndex;
        public Player CurrentPlayer
        {
            get
            {
                if (Players == null) return null;
                return Players[CurrentPlayerIndex];
            }
        }*/

        private const int tileSize = 16;

        //our ID
        public int We = -1;

        //client side of world
        public fbWorld World;

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

        public List<string> Log; 

        public fbGame(fbEngine engine)
        {
            this.engine = engine;
            Unit.Game = this;
            Station.Game = this;
            Planet.Game = this;
            fbNetClient.Game = this;
            Initialize();
        }

        public void Initialize()
        {
            engine.SetSize(1280, 720);

            ui = new fbInterface(this, engine);

            Log = new List<string>();
            Log.Add("Welcome to Farbase.");

            UnitType scout = new UnitType();
            scout.Texture = engine.GetTexture("scout");
            scout.Moves = 2;
            scout.Strength = 3;
            scout.Attacks = 1;
            UnitType.RegisterType("scout", scout);

            UnitType worker = new UnitType();
            worker.Texture = engine.GetTexture("worker");
            worker.Moves = 1;
            worker.Strength = 1;
            UnitType.RegisterType("worker", worker);

            /*Players = new List<Player>();
            Players.Add(new Player("Lukas", Color.CornflowerBlue));
            Players.Add(new Player("Barbarians", Color.Red));

            Map = new Map(80, 45);
            SpawnUnit(scout, Players[0], new Vector2(9, 11));

            Map.At(8, 12).Station = new Station();
            Map.At(9, 12).Station = new Station();

            Map.At(14, 14).Planet = new Planet();

            SpawnUnit(worker, Players[0], new Vector2(10, 12));

            SpawnUnit(worker, Players[1], new Vector2(16, 14));*/
        }

        /*public void PassTurn()
        {
            foreach (Unit u in CurrentPlayer.Units)
                u.Replenish();
        }*/

        public void Update()
        {
            if (engine.KeyPressed(Keys.Escape))
                engine.Exit();

            ui.UpdateCamera();

            if (engine.KeyPressed(Keys.Enter))
            {
                if(OurTurn)
                    engine.NetClient.Send("pass");
                else
                    Log.Add("Not your turn!");
            }

            if (engine.KeyPressed(Keys.Space))
            {
                engine.NetClient.ShouldDie = true;
            }

            if (engine.KeyPressed(Keys.G))
            {
                List<string> names =
                    new List<string>
                    {
                        "Captain Zorblax",
                        "Commander Kneckers"
                    };

                List<Color> colors =
                    new List<Color>
                    {
                        Color.Green,
                        Color.CornflowerBlue
                    };

                engine.NetClient.Send(
                    string.Format(
                        "login:{0},{1}",
                        names[We],
                        ExtensionMethods.ColorToString(colors[We])
                    )
                );
            }

            if (engine.KeyPressed(Keys.H))
            {
                if (OurTurn)
                {
                    engine.NetClient.Send(
                        string.Format(
                            "give-test-scout"
                        )
                    );
                }
            }

            if (
                SelectedUnit != null &&
                SelectedUnit.Owner == World.CurrentPlayer.ID
            ) {
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
                        //SelectedUnit = u;
                    }
                    else if (u.CanAttack(u.Position + moveOrder))
                    {
                        u.Attack(u.Position + moveOrder);
                    }
                }
            }

            /*if (SelectedUnit != null)
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

                if (engine.KeyPressed(Keys.A))
                {
                    SelectedUnit.MoveTo(
                        SelectedUnit.Position +
                        SelectedUnit.StepTowards(new Vector2(0))
                    );
                    SelectedUnit.Moves -= 1;
                }
            }*/

            /*if (engine.KeyPressed(Keys.W))
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
                }*/

            if (engine.ButtonPressed(0))
            {
                if (engine.Active && engine.MouseInside)
                {
                    Vector2 square = ScreenToGrid(engine.MousePosition);
                    Selection = World.Map.At(square);
                }
            }
        }

        public bool OurTurn {
            get { return World.PlayerIDs[World.CurrentPlayerIndex] == We; }
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
                square.X >= 0 && square.X < World.Map.Width &&
                square.Y >= 0 && square.Y < World.Map.Height
            )
                return square;

            //TODO: THIS IS JUST TO STOP CRASHING
            return new Vector2(0);
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
            for (int x = 0; x < World.Map.Width; x++)
            for (int y = 0; y < World.Map.Height; y++)
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
            Tile t = World.Map.At(x, y);
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
                World.Players[u.Owner].Color
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

            for (int i = 0; i < u.Strength; i++)
            {
                Vector2 dingySize = engine.GetTextureSize("strength-dingy");

                engine.Draw(
                    engine.GetTexture("strength-dingy"),
                    ui.WorldToScreen(
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
            for (int x = 0; x < World.Map.Width; x++)
            for (int y = 0; y < World.Map.Height; y++)
            {
                DrawTile(x, y);
            }
        }

        public void Draw()
        {
            DrawBackground();

            if (World == null) return;

            DrawMap();
            ui.DrawUI();
        }
    }

}
