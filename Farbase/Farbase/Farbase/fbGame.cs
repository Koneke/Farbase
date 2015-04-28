using System;
using System.Collections.Generic;
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
        public List<int> OwnedUnits; 

        public Player(string name, int id, Color color)
        {
            Name = name;
            ID = id;
            Color = color;
            OwnedUnits = new List<int>();
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

        public Tile At(Vector2i p)
        {
            return map[p.X, p.Y];
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
        public int ID;

        public int x, y;

        public Vector2 Position
        {
            get { return new Vector2(x, y); }
        }

        public Tile Tile
        {
            get {
                return fbGame.World.Map.At(x, y);
            }
        }

        public int Moves;
        public int Attacks;
        public int Strength;

        public Unit(
            UnitType unitType,
            int owner,
            int id,
            int x,
            int y
        ) {
            UnitType = unitType;
            Owner = owner;
            ID = id;
            this.x = x;
            this.y = y;
            Moves = UnitType.Moves;
            Attacks = UnitType.Attacks;
            Strength = UnitType.Strength;
        }

        public Unit(
            UnitType unitType,
            int owner,
            int id,
            Vector2 position
        ) : this(unitType, owner, id, (int)position.X, (int)position.Y) {
        }

        public void Replenish()
        {
            Moves = UnitType.Moves;
            Attacks = UnitType.Attacks;

            //workers generate cash if they start the turn on a planet
            /*if(UnitType == UnitType.GetType("worker"))
                if (Tile.Planet != null)
                    fbGame.World.Players[Owner].Money += 10;*/

            if (Tile.Station != null)
            {
                if (Strength < UnitType.Strength)
                    Strength++;
            }
        }

        public bool CanMoveTo(Vector2 position)
        {
            //only bad condition atm is collision
            if (fbGame.World.Map.At(position).Unit == null)
                return true;
            return false;
        }

        public bool CanAttack(Vector2 position)
        {
            Vector2 delta = Position - position;
            //only neighbours
            if (Math.Abs(delta.X) > 1 || Math.Abs(delta.Y) > 1)
                return false;

            if (fbGame.World.Map.At(position).Unit != null)
                if (fbGame.World.Map.At(position).Unit.Owner != Owner)
                    return true;

            return false;
        }

        //public void MoveTo(Vector2 position)
        public void MoveTo(int tx, int ty)
        {
            Tile.Unit = null;
            x = tx;
            y = ty;
            fbGame.World.Map.At(tx, ty).Unit = this;
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

        public void Hurt(int amount)
        {
            Strength -= amount;
            if (Strength <= 0)
            {
                //fbGame.World.DespawnUnit(this)?
                fbGame.World.Units.Remove(this);
                fbGame.World.Map.At(x, y).Unit = null;
                fbGame.World.UnitLookup.Remove(ID);
                fbGame.World.Players[Owner].OwnedUnits.Remove(ID);
            }
        }
    }

    public class Station
    {
        public Vector2 Position;
        public Tile Tile
            { get { return fbGame.World.Map.At(Position); } }
    }

    public class Planet
    {
        public Vector2 Position;
        public Tile Tile
            { get { return fbGame.World.Map.At(Position); } }
    }

    public class fbGame
    {
        private fbApplication app;

        private fbEngine engine { get { return app.Engine; } }
        private fbInterface ui { get { return app.UI; } }

        private const int tileSize = 16;

        //our ID
        public int We = -1;

        public bool OurTurn {
            get { return World.PlayerIDs[World.CurrentPlayerIndex] == We; }
        }
        //client side of world
        public static fbWorld World;

        public List<string> Log; 

        public fbGame(fbApplication app)
        {
            this.app = app;
            Unit.Game = this;
            fbNetClient.Game = this;
            Initialize();
        }

        public void Initialize()
        {
            engine.SetSize(1280, 720);

            Log = new List<string>();

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
        }

        //this entire thing should probably not exist?
        //like, this is all interface stuff
        public void Update()
        {
            if (engine.KeyPressed(Keys.Escape))
                engine.Exit();

            //this should probably be moved out to application level?
            ui.Cam.UpdateCamera();

            //if we're still receiving data, wait.
            if (!engine.NetClient.Ready) return;
            if (engine.KeyPressed(Keys.Enter))
            {
                if (OurTurn)
                {
                    //engine.NetClient.Send("pass");
                    engine.NetClient.Send(new PassMessage());
                }
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
                    new NameMessage(We, names[We], colors[We])
                );
            }

            if (engine.KeyPressed(Keys.H))
            {
                if (OurTurn)
                {
                    engine.NetClient.Send(
                        new DevCommandMessage(0)
                    );
                }
            }

            if (
                OurTurn &&
                ui.SelectedUnit != null &&
                ui.SelectedUnit.Owner == World.CurrentPlayer.ID
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

                if (moveOrder != Vector2.Zero && ui.SelectedUnit.Moves > 0)
                {
                    Unit u = ui.SelectedUnit;

                    if(u.CanMoveTo(u.Position + moveOrder))
                    {
                        int x = (int)(u.Position + moveOrder).X;
                        int y = (int)(u.Position + moveOrder).Y;

                        engine.NetClient.Send(
                            string.Format(
                                "move:{0},{1},{2}",
                                u.ID,
                                x, y
                            )
                        );

                        //we want to send this instead later
                        //currently becomes the exact same string,
                        //but I want to give the actual message object to send
                        //instead of the string
                        //new MoveUnitMessage(app, u.ID, x, y).Format();

                        u.Moves -= 1;
                        u.MoveTo(x, y);
                    }
                    else if (u.CanAttack(u.Position + moveOrder))
                    {
                        Vector2 targettile = u.Position + moveOrder;
                        Unit target = World.Map.At(
                            (int)targettile.X,
                            (int)targettile.Y
                        ).Unit;

                        engine.NetClient.Send(
                            new AttackMessage(u.ID, target.ID)
                        );
                        //u.Attack(u.Position + moveOrder);
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
                    Vector2? square = ScreenToGrid(engine.MousePosition);

                    //null means we clicked on the screen, but outside the grid.
                    if (square != null)
                    {
                        ui.Select(
                            new Vector2i(
                                (int)square.Value.X,
                                (int)square.Value.Y
                                )
                            );
                    }
                }
            }
        }

        private Vector2? ScreenToGrid(Vector2 position)
        {
            Vector2 worldPoint = ui.Cam.ScreenToWorld(position);
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

            return null;
        }
    }
}
