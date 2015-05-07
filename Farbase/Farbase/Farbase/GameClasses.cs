using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Farbase
{
    public enum TechID
    {
        FighterTech
    }

    public class Tech
    {
        public static Dictionary<TechID, Tech> Techs =
            new Dictionary<TechID, Tech>();

        public TechID ID;
        public string Name;
        public string Description;
        public int Cost;
        public int ResearchTime;
        public List<TechID> Prerequisites; 

        public Tech(
            TechID id,
            string name,
            int cost,
            int time
        ) {
            ID = id;
            Name = name;
            Description = "Lorem ipsum";
            Techs.Add(id, this);
            Prerequisites = new List<TechID>();
            Cost = cost;
            ResearchTime = time;
        }
    }

    public class Planet
    {
        public Vector2 Position;
    }
}