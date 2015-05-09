using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Farbase
{
    public class Tile
    {
        private fbMap map;
        public Unit Unit;
        public Station Station;
        public Planet Planet;
        public Vector2i Position;

        public Tile(fbMap map, int x, int y)
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
        public fbGame Game;
        public fbMap Map;

        public Dictionary<int, Player> Players;
        public int CurrentID;

        private Dictionary<int, List<int>> PlayerUnits;
        private Dictionary<int, List<int>> PlayerStations; 

        public Dictionary<int, Unit> Units;

        public List<Vector2i> PlayerStarts; 

        public fbWorld(fbGame game, int w, int h)
        {
            Game = game;
            Map = new fbMap(w, h);
            Players = new Dictionary<int, Player>();

            Units = new Dictionary<int, Unit>();
            PlayerUnits = new Dictionary<int, List<int>>();
            PlayerStations = new Dictionary<int, List<int>>();
            PlayerStarts = new List<Vector2i>();
        }

        public Player GetPlayer(int id)
        {
            if (Players.ContainsKey(id))
                return Players[id];
            return null;
        }

        public List<int> GetPlayerUnits(int id)
        {
            if (!PlayerUnits.ContainsKey(id))
                PlayerUnits.Add(id, new List<int>());

            return PlayerUnits[id];
        }

        public List<int> GetPlayerStations(int id)
        {
            if (!PlayerStations.ContainsKey(id))
                PlayerStations.Add(id, new List<int>());

            return PlayerStations[id];
        }

        public void RemovePlayer(int id)
        {
            List<int> ownedIDs = new List<int>(GetPlayerUnits(id));

            foreach (int unitid in ownedIDs)
                DespawnUnit(Units[unitid]);

            foreach (int stationid in GetPlayerStations(id))
                Map.Stations[stationid].Owner = -1;

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

        public void SpawnStation(
            int owner,
            int id,
            int x,
            int y
        ) {
            Station s = new Station(this);
            s.Owner = owner;
            s.ID = id;
            s.Position = new Vector2i(x, y);

            Map.At(x, y).Station = s;
            Map.Stations.Add(s.ID, s);

            if (!PlayerStations.ContainsKey(s.Owner))
                PlayerStations.Add(s.Owner, new List<int>());

            PlayerStations[s.Owner].Add(s.ID);
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
            Units.Add(u.ID, u);

            if (!PlayerUnits.ContainsKey(u.Owner))
                PlayerUnits.Add(u.Owner, new List<int>());

            PlayerUnits[u.Owner].Add(u.ID);
            return u;
        }

        public void DespawnUnit(Unit u)
        {
            Map.At(u.Position).Unit = null;
            Units.Remove(u.ID);
            PlayerUnits[u.Owner].Remove(u.ID);
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
            foreach (int id in GetPlayerUnits(next.ID))
            {
                Unit u = Units[id];
                u.Recharge();
                if(u.HasAbility(UnitAbilites.Mining) && u.Tile.Planet != null)
                    Players[u.Owner].Money += 10;
            }

            foreach (int id in GetPlayerStations(next.ID))
            {
                Station s = Map.Stations[id];
                if (s.Project != null)
                {
                    s.Project.Progress();
                    if (s.Project.Finished)
                        s.Project = null;
                }
            }
        }
    }
}
