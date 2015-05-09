using System;
using System.Collections.Generic;

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
        public int Cost;
        public int Length;
        public int Remaining;

        public List<TechID> Prerequisites; 

        protected bool finished;
        public bool Finished { get { return finished; } }

        protected Project(
            fbGame game,
            int owner,
            Station station,
            int cost,
            int length
        ) {
            Game = game;
            Owner = owner;
            Station = station;
            Cost = cost;
            Remaining = Length = length;
            finished = false;
            Prerequisites = new List<TechID>();
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
        ) : base(
            game,
            owner,
            station,
            unitType.Cost,
            length
        ) {
            this.unitType = unitType;
            Prerequisites.AddRange(unitType.Prerequisites);
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
        private TechID tech;

        public TechProject(
            fbGame game,
            int owner,
            Station station,
            int length,
            TechID tech
        ) : base(game, owner, station, Tech.Techs[tech].Cost, length) {
            this.tech = tech;
            Prerequisites.AddRange(Tech.Techs[tech].Prerequisites);
        }

        public override int GetProject()
        {
            //tech.ID is an enum, but we can just cast it to int completely fine
            //like this. trust me.
            return (int)tech;
        }

        public override ProjectType GetProjectType()
        {
            return ProjectType.TechProject;
        }

        public override string GetProjectName()
        {
            return "Researching " + Tech.Techs[tech].Name;
        }
    }
}