using System.Collections.Generic;
using Microsoft.Xna.Framework;

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
                map[x, y] = new Tile(this, x, y);
        }

        public int Width;
        public int Height;

        public Tile At(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height)
                return null;
            return map[x, y];
        }

        public Tile At(Vector2i p)
        {
            return At(p.X, p.Y);
        }

        public Tile At(Vector2 position)
        {
            return At(
                (int)position.X,
                (int)position.Y
            );
        }
    }

    public class Tile
    {
        private Map map;
        public Unit Unit;
        public Station Station;
        public Planet Planet;
        public Vector2i Position;

        public Tile(Map map, int x, int y)
        {
            this.map = map;
            Position = new Vector2i(x, y);
        }

        public List<Tile> GetNeighbours()
        {
            List<Tile> neighbours = new List<Tile>();

            for (int x = -1; x < 1; x++)
            for (int y = -1; y < 1; y++)
                if (!(x == 0 && y == 0))
                {
                    Tile t = map.At(x, y);
                    if (t != null)
                        neighbours.Add(t);
                }

            return neighbours;
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
            scout.Cost = 10;
            UnitType.RegisterType("scout", scout);

            UnitType worker = new UnitType();
            worker.Texture = engine.GetTexture("worker");
            worker.Moves = 1;
            worker.Strength = 1;
            worker.Cost = 5;
            UnitType.RegisterType("worker", worker);
        }

        public void Update()
        {
            foreach (Event e in engine.Poll(NameEvent.EventType))
                HandleEvent((NameEvent)e);

            foreach (Event e in engine.Poll(UnitMoveEvent.EventType))
                HandleEvent((UnitMoveEvent)e);
        }

        public void HandleEvent(NameEvent ne)
        {
            World.Players[ne.ID].Name = ne.Name;
            World.Players[ne.ID].Color = ne.Color;
        }

        public void HandleEvent(UnitMoveEvent ume)
        {
            Unit u = World.UnitLookup[ume.ID];
            u.MoveTo(ume.x, ume.y);
        }
    }
}
