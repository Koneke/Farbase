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

        public Tech(
            TechID id,
            string name
        ) {
            ID = id;
            Name = name;
            Description = "Lorem ipsum";
            Techs.Add(id, this);
        }
    }

    public class Planet
    {
        public Vector2 Position;
    }
}