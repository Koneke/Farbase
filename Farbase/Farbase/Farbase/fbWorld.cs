using System.Collections.Generic;
using System.Linq;
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
        public Dictionary<int, Player> Players;
        public int CurrentID;

        public Map Map;

        //only needs to be serverside
        public int UnitIDCounter;

        public List<Unit> Units;
        public Dictionary<int, Unit> UnitLookup; 

        public fbWorld(int w, int h)
        {
            Map = new Map(w, h);
            Players = new Dictionary<int, Player>();

            Units = new List<Unit>();
            UnitLookup = new Dictionary<int, Unit>();
        }

        public Player GetPlayer(int id)
        {
            if (Players.ContainsKey(id))
                return Players[id];
            return null;
        }

        public void RemovePlayer(int id)
        {
            List<int> ownedIDs =
                new List<int>(
                    Players[id].OwnedUnits
                );

            foreach (int unitid in ownedIDs)
                DespawnUnit(UnitLookup[unitid]);

            if (Players.Count == 1)
            {
                Players.Remove(id);
                CurrentID = -1;
            }

            if (CurrentID == id)
            {
                List<int> ids = Players.Keys.ToList();
                int index = ids.IndexOf(id);
                int newIndex = index % (Players.Count - 1);
                ids.Remove(id);
                CurrentID = ids[newIndex];

                Players.Remove(id);
                PassTo(Players[CurrentID]);
            }
            //if it's not the current player dcing, we don't have to fiddle
            //with currentID, since it's a player ID either way, and no longer
            //an index in the player array
            else
                Players.Remove(id);
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
            Units.Add(u);
            Map.At(u.x, u.y).Unit = u;
            UnitLookup.Add(u.ID, u);
            GetPlayer(u.Owner).OwnedUnits.Add(u.ID);
            return u;
        }

        public void DespawnUnit(Unit u)
        {
            Units.Remove(u);
            Map.At(u.Position).Unit = null;
            UnitLookup.Remove(u.ID);
            GetPlayer(u.Owner).OwnedUnits.Remove(u.ID);
        }

        public void AddPlayer(Player p)
        {
            Players.Add(p.ID, p);
        }

        public void Pass()
        {
            List<int> ids = Players.Keys.ToList();
            int index = ids.IndexOf(CurrentID);
            index = (index + 1) % Players.Count;
            CurrentID = ids[index];

            PassTo(Players[CurrentID]);
        }

        public void PassTo(Player next)
        {
            foreach (int id in next.OwnedUnits)
            {
                Unit u = UnitLookup[id];
                u.Recharge();
                if(u.HasAbility(UnitAbilites.Mining) && u.Tile.Planet != null)
                    Players[u.Owner].Money += 10;
            }
        }
    }
}
