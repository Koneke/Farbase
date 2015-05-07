using System;

namespace Farbase
{
    public enum ProjectType
    {
        UnitProject,
        TechProject
    }

    public abstract class Project
    {
        protected fbGame Game;
        protected int Owner;
        protected Station Station;
        public int Length;
        public int Remaining;

        protected bool finished;
        public bool Finished { get { return finished; } }

        protected Project(
            fbGame game,
            int owner,
            Station station,
            int length
        ) {
            Game = game;
            Owner = owner;
            Station = station;
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
            Station station,
            int length,
            UnitType unitType
        ) : base(game, owner, station, length) {
            this.unitType = unitType;
        }

        public override void Finish()
        {
            //todo: this is a temporary hacky thing
            //      just making sure that we're not spawning units on each
            //      other. If we're about to do so, just postpone the finishing
            //      of the project.
            //      In the future we might want to do something like, spawn it
            //      next to the station instead, but we'll take that when the
            //      time is right.

            if (Station.Tile.Unit == null) {
                Game.Build(unitType, Station);
                finished = true;
            }
            else
            {
                Game.Log.Add(
                    string.Format(
                        "Can't finish building {0}," +
                            "there's a unit in the way! ({1},{2})",
                        unitType.Name,
                        Station.Position.X,
                        Station.Position.Y
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
            return ProjectType.UnitProject;
        }

        public override string GetProjectName()
        {
            return "Build " + unitType.Name;
        }
    }

    public class TechProject : Project
    {
        private Tech tech;

        public TechProject(
            fbGame game,
            int owner,
            Station station,
            int length,
            Tech tech
        ) : base(game, owner, station, length) {
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
}