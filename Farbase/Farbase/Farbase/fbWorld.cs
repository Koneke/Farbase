using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Farbase
{
    public class fbWorld
    {
        public List<int> PlayerIDs;
        public int CurrentPlayerIndex;

        public Player CurrentPlayer
        {
            get { return Players[PlayerIDs[CurrentPlayerIndex]]; }
        }

        public Dictionary<int, Player> Players; 
        public Map Map;

        public fbWorld(int w, int h)
        {
            Players = new Dictionary<int, Player>();
            PlayerIDs = new List<int>();
            CurrentPlayerIndex = 0;
            Map = new Map(w, h);
        }

        public void SpawnStation(int x, int y)
        {
            Station s = new Station();
            s.Position = new Vector2(x, y);
            Map.At(x, y).Station = s;
        }

        public void SpawnPlanet(int x, int y)
        {
            Planet p = new Planet();
            p.Position = new Vector2(x, y);
            Map.At(x, y).Planet = p;
        }

        public void AddPlayer(Player p)
        {
            Players.Add(p.ID, p);
            PlayerIDs.Add(p.ID);
        }
    }
}
