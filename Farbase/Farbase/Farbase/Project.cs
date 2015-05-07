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
        public int Owner;
        public Station Station;
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

        public void Finish()
        {
            Game.EventHandler.Push(new ProjectFinishedEvent(this));
        }

        //should probably not be a string in the future
        //we should do it as either an object (so we can cast depending on
        //project type) or as an int (so we can look it up in the correct enum).
        public abstract int GetProject();
        public abstract ProjectType GetProjectType();

        public abstract string GetProjectName();

        public void SetFinished()
        {
            finished = true;
        }
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

        public override int GetProject()
        {
            return (int)unitType.Type;
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

        public override int GetProject()
        {
            //tech.ID is an enum, but we can just cast it to int completely fine
            //like this. trust me.
            return (int)tech.ID;
        }

        public override ProjectType GetProjectType()
        {
            return ProjectType.TechProject;
        }

        public override string GetProjectName()
        {
            return "Researching " + tech.Name;
        }
    }
}