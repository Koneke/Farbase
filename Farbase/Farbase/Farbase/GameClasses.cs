using Microsoft.Xna.Framework;

namespace Farbase
{
    public class Station
    {
        public Vector2 Position;
        public Tile Tile
        {
            get {
                return fbGame.World.Map.At(Position);
            }
        }

        public int Owner;

        public Station()
        {
        }
    }

    public class Planet
    {
        public Vector2 Position;
        public Tile Tile
        { get { return fbGame.World.Map.At(Position); } }
    }
}