namespace Farbase
{
    public abstract class Structure
    {
        public fbWorld World;

        public Tile Tile { get { return World.Map.At(Position); } }

        protected Structure(
            fbWorld world
        ) {
            World = world;
        }

        public static int IDCounter = 0;

        public int Owner;
        public int ID;
        public Vector2i Position;
    }

    public class Station : Structure
    {
        public Station(fbWorld world) : base(world)
        {
            Texture = "station";
        }

        public Project Project;
        public string Texture;
    }
}