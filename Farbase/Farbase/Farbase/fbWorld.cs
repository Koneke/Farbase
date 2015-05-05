using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Farbase
{
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

    public class fbWorld
    {
        public List<int> PlayerIDs;
        public int CurrentPlayerIndex;
        public int CurrentID
        {
            get { return PlayerIDs[CurrentPlayerIndex]; }
        }
        public Player CurrentPlayer
        {
            get { return Players[CurrentID]; }
        }

        public Dictionary<int, Player> Players;
        public Map Map;

        //only needs to be serverside
        public int UnitIDCounter;

        public List<Unit> Units;
        public Dictionary<int, Unit> UnitLookup; 

        public fbWorld(int w, int h)
        {
            Players = new Dictionary<int, Player>();
            PlayerIDs = new List<int>();
            CurrentPlayerIndex = 0;
            Map = new Map(w, h);

            Units = new List<Unit>();
            UnitLookup = new Dictionary<int, Unit>();
        }

        public Player GetPlayer(int id)
        {
            if (!Players.ContainsKey(id))
                return null;
            return Players[id];
        }

        public void SpawnStation(int owner, int x, int y)
        {
            Station s = new Station();
            s.Owner = owner;
            s.Position = new Vector2i(x, y);
            Map.At(x, y).Station = s;
        }

        public void SpawnPlanet(int x, int y)
        {
            Planet p = new Planet();
            p.Position = new Vector2(x, y);
            Map.At(x, y).Planet = p;
        }

        public Unit SpawnUnit(Unit u)
        {
            Map.At(u.x, u.y).Unit = u;

            Units.Add(u);
            UnitLookup.Add(u.ID, u);
            Players[u.Owner].OwnedUnits.Add(u.ID);
            return u;
        }

        public Unit SpawnUnit(
            String type,
            int owner,
            int id,
            int x,
            int y
        ) {
            Unit u = new Unit(
                this,
                UnitType.GetType(type),
                owner,
                id,
                x, y
            );
            Map.At(x, y).Unit = u;

            Units.Add(u);
            UnitLookup.Add(u.ID, u);
            Players[owner].OwnedUnits.Add(id);
            return u;
        }

        public void AddPlayer(Player p)
        {
            Players.Add(p.ID, p);
            PlayerIDs.Add(p.ID);
        }

        public void PassTo(int playerID)
        {
            Player p = Players[playerID];

            foreach (int id in p.OwnedUnits)
            {
                UnitLookup[id].Replenish();
            }
        }
    }
}
