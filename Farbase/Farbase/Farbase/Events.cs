using System;
using Microsoft.Xna.Framework;

namespace Farbase
{
    public enum EventType
    {
        NameEvent,
        UnitMoveEvent,
        BuildUnitEvent,
        BuildStationEvent
    }

    public abstract class Event
    {
        public abstract EventType GetEventType();
    }

    public class NameEvent : Event
    {
        public const EventType Type = EventType.NameEvent;
        public override EventType GetEventType() { return Type; }

        public int ID;
        public string Name;
        public Color Color;

        public NameEvent(
            int id,
            String name,
            Color color
        ) {
            ID = id;
            Name = name;
            Color = color;
        }

        public NameEvent(int id, string name, string color)
            : this(id, name, ExtensionMethods.ColorFromString(color)) { }
    }

    public class UnitMoveEvent : Event
    {
        public const EventType Type = EventType.UnitMoveEvent;
        public override EventType GetEventType() { return Type; }

        public int ID;
        public int x, y;

        public UnitMoveEvent(
            int id,
            int x,
            int y
        ) {
            ID = id;
            this.x = x;
            this.y = y;
        }
    }

    public class BuildStationEvent : Event
    {
        public const EventType Type = EventType.BuildStationEvent;
        public override EventType GetEventType() { return Type; }

        public int Owner;
        public int x, y;

        public BuildStationEvent(
            int owner,
            int x,
            int y
        ) {
            Owner = owner;
            this.x = x;
            this.y = y;
        }
    }

    public class BuildUnitEvent : Event
    {
        public const EventType Type = EventType.BuildUnitEvent;
        public override EventType GetEventType() { return Type; }

        public string UnitType;
        public int Owner;
        public int x, y;

        public BuildUnitEvent(
            string unitType,
            int owner,
            int x,
            int y
        ) {
            UnitType = unitType;
            Owner = owner;
            this.x = x;
            this.y = y;
        }
    }

    public abstract class fbEventHandler
    {
        protected fbGame Game;
        protected fbEngine Engine;

        protected fbEventHandler(fbGame game)
        {
            Game = game;
            Engine = Game.Engine;
        }

        public abstract void Handle(Event e);
    }

    public class GameEventHandler : fbEventHandler
    {
        public GameEventHandler(fbGame fbGame)
            : base(fbGame)
        {
        }

        public override void Handle(Event e)
        {
            switch (e.GetEventType())
            {
                case EventType.NameEvent:
                    NameEvent ne = (NameEvent)e;
                    Game.World.Players[ne.ID].Name = ne.Name;
                    Game.World.Players[ne.ID].Color = ne.Color;
                    break;

                case EventType.UnitMoveEvent:
                    UnitMoveEvent ume = (UnitMoveEvent)e;
                    Unit u = Game.World.UnitLookup[ume.ID];
                    u.MoveTo(ume.x, ume.y);
                    break;

                case EventType.BuildStationEvent:
                    BuildStationEvent bse = (BuildStationEvent)e;
                    Engine.NetClient.Send(
                        new NetMessage3(
                            NM3MessageType.create_station,
                            bse.Owner,
                            bse.x,
                            bse.y
                        )
                    );
                    break;

                default:
                    throw new ArgumentException();
            }
        }
    }

    public class InterfaceEventHandler : fbEventHandler
    {
        private fbInterface ui;

        public InterfaceEventHandler(fbGame game, fbInterface ui)
            : base(game)
        {
            Game = game;
            this.ui = ui;
        }

        public override void Handle(Event e)
        {
            switch (e.GetEventType())
            {
                case EventType.NameEvent:
                    NameEvent ne = (NameEvent)e;
                    Game.Log.Add(
                        string.Format(
                            "{0}<{2}> is now known as {1}<{2}>.",
                            Game.World.Players[ne.ID].Name,
                            ne.Name,
                            ne.ID
                        )
                    );
                    break;

                case EventType.BuildUnitEvent:
                    //this should probably not be a thing later,
                    //because unit building units takes time?
                    //(so it'd actually be annoying)
                    //but right now it's instant and you probably want it
                    //selected asap

                    BuildUnitEvent bue = (BuildUnitEvent)e;
                    Vector2i selected = ui.Selection.GetSelection();

                    //reselect, so our tile selection -> unit selection
                    //still clunky, but it's what we go with for now
                    if (bue.x == selected.X && bue.y == selected.Y)
                        ui.Select(new Vector2i(bue.x, bue.y));
                    break;

                default:
                    throw new ArgumentException();
            }
        }
    }
}
