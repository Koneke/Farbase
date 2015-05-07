using System;
using Microsoft.Xna.Framework;

namespace Farbase
{
    public enum ProjectType
    {
        BuildingProject,
        TechProject
    }

    public abstract class Project
    {
        protected fbGame Game;
        protected int Owner;
        protected Station Location;
        public int Length;
        public int Remaining;

        protected bool finished;
        public bool Finished { get { return finished; } }

        protected Project(
            fbGame game,
            int owner,
            Station location,
            int length
        ) {
            Game = game;
            Owner = owner;
            Location = location;
            Remaining = Length = length;
            finished = false;
        }

        public void Progress()
        {
            Remaining = Math.Max(0, Remaining - 1);

            if (Remaining <= 0)
            {
                Finish();
            }
        }

        public abstract void Finish();

        //should probably not be a string in the future
        //we should do it as either an object (so we can cast depending on
        //project type) or as an int (so we can look it up in the correct enum).
        public abstract string GetProject();
        public abstract ProjectType GetProjectType();

        public abstract string GetProjectName();
    }

    public class BuildingProject : Project
    {
        private UnitType unitType;

        public BuildingProject(
            fbGame game,
            int owner,
            Station location,
            int length,
            UnitType unitType
        ) : base(game, owner, location, length) {
            this.unitType = unitType;
        }

        public override void Finish()
        {
            if (Game.CanBuild(Game.World.GetPlayer(Owner), unitType, Location))
            {
                Game.Build(unitType, Location);
                finished = true;
            }
            else
            {
                Game.Log.Add(
                    string.Format(
                        "Can't finish building {0}," +
                        "there's a unit in the way! ({1},{2})",
                        unitType.Name,
                        Location.Position.X,
                        Location.Position.Y
                    )
                );
            }
        }

        public override string GetProject()
        {
            return unitType.Name;
        }

        public override ProjectType GetProjectType()
        {
            return ProjectType.BuildingProject;
        }

        public override string GetProjectName()
        {
            return "Build " + unitType.Name;
        }
    }

    public enum Tech
    {
    }

    public class TechProject : Project
    {
        private Tech tech;

        public TechProject(
            fbGame game,
            int owner,
            Station location,
            int length,
            Tech tech
        ) : base(game, owner, location, length) {
            this.tech = tech;
        }

        public override void Finish()
        {
            throw new NotImplementedException();
        }

        public override string GetProject()
        {
            throw new NotImplementedException();
        }

        public override ProjectType GetProjectType()
        {
            return ProjectType.TechProject;
        }

        public override string GetProjectName()
        {
            throw new NotImplementedException();
        }
    }

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
    }

    public class Planet
    {
        public Vector2 Position;
    }
}