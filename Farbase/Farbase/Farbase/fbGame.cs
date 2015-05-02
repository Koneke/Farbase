using System;
using System.Linq;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Farbase
{
    public class Player
    {
        public const int DiplomacyPointsMax = 100;

        public int ID;
        public string Name;
        public Color Color;
        public int Money;
        public int DiplomacyPoints;
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

        private Dictionary<int, int> loyalty;

        public Station()
        {
            loyalty = new Dictionary<int, int>();
        }

        public int GetLoyalty(int id)
        {
            if (loyalty.ContainsKey(id))
                return loyalty[id];
            return 0;
        }

        public void SetLoyalty(int id, int amount)
        {
            if (loyalty.ContainsKey(id))
                loyalty[id] = amount;
            else loyalty.Add(id, amount);
        }

        public void AddLoyalty(int id, int amount)
        {
            if (loyalty.ContainsKey(id))
                loyalty[id] =
                    Math.Min(100, GetLoyalty(id) + amount);
            else loyalty.Add(id, amount);
        }

        public void RemoveLoyalty(int id, int amount)
        {
            if (loyalty.ContainsKey(id))
                loyalty[id] =
                    Math.Max(0, GetLoyalty(id) - amount);
            else loyalty.Add(id, 0);
        }
    }

    public class Planet
    {
        public Vector2 Position;
        public Tile Tile
            { get { return fbGame.World.Map.At(Position); } }
    }

    public class fbGame
    {
        private fbEngine engine;

        private Dictionary<string, Property> properties;

        public Property GetProperty(string name)
        {
            if(properties.ContainsKey(name.ToLower()))
                return properties[name.ToLower()];
            return null;
        }

        public void RegisterProperty(string name, Property property)
        {
            properties.Add(name.ToLower(), property);
        }

        //our ID
        public int We = -1;

        public bool OurTurn {
            get { return World.PlayerIDs[World.CurrentPlayerIndex] == We; }
        }

        public Player LocalPlayer {
            get
            {
                return We == -1
                    ? null
                    : World.Players[We];
            }
        }

        //client side of world
        public static fbWorld World;

        public List<string> Log;

        public fbGame(fbEngine engine)
        {
            this.engine = engine;
            Unit.Game = this;
            fbNetClient.Game = this;
            Initialize();
        }

        public void Initialize()
        {
            engine.SetSize(1280, 720);
            properties = new Dictionary<string, Property>();

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

            //have yet to get map data from server
            //this is a pretty clumpsy way of doing shit, but it
            //works for the time being
            if (World == null) return;

            GetProperty("player-names")
                .SetValue(
                    World.PlayerIDs
                        .Select(id => World.Players[id].Name)
                        .ToList()
                );

            GetProperty("current-player-name")
                .SetValue(World.CurrentPlayer.Name);

            GetProperty("current-player-id")
                .SetValue(World.CurrentPlayer.ID);

            GetProperty("local-player-name")
                .SetValue(LocalPlayer.Name);

            GetProperty("local-player-id")
                .SetValue(LocalPlayer.ID);

            GetProperty("local-player-money")
                .SetValue(LocalPlayer.Money);

            GetProperty("local-player-diplo")
                .SetValue(LocalPlayer.DiplomacyPoints);

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

        public void BuyLoyalty(Station station)
        {
            if (LocalPlayer.DiplomacyPoints >= 20)
                engine.NetClient.Send(
                    new PurchaseStationLoyaltyMessage(
                        We,
                        station.Tile.Position.X,
                        station.Tile.Position.Y
                    )
                );
        }
    }
}
