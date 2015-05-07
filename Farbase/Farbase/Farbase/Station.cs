using System;

namespace Farbase
{
    public class Station
    {
        //I don't really like having this as public, but it does make
        //it a bit easier to work with.
        //Just looks a bit ugly, is all.
        public fbWorld World;

        public Tile Tile
        {
            get { return World.Map.At(Position); }
        }

        public Station(
            fbWorld world
        ) {
            World = world;
        }

        public static int IDCounter = 0;

        public int Owner;
        public int ID;
        public Vector2i Position;
        public Project Project;

        public void StartProject(
            ProjectType projectType,
            int project
        ) {
            switch (projectType)
            {
                case ProjectType.UnitProject:
                    World.Game.EventHandler.Push(
                        new SetProjectEvent(
                            Owner,
                            ID,
                            projectType,
                            project
                        )
                    );
                    break;

                default:
                    throw new ArgumentException();
            }
        }
    }
}