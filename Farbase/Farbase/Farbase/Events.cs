using System;
using Microsoft.Xna.Framework;

namespace Farbase
{
    public enum EventType
    {
        NameEvent,
        UnitMoveEvent
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

    public abstract class fbEventHandler
    {
        protected fbGame Game;

        protected fbEventHandler(fbGame game)
        {
            Game = game;
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
                    fbGame.World.Players[ne.ID].Name = ne.Name;
                    fbGame.World.Players[ne.ID].Color = ne.Color;
                    break;

                case EventType.UnitMoveEvent:
                    UnitMoveEvent ume = (UnitMoveEvent)e;
                    Unit u = fbGame.World.UnitLookup[ume.ID];
                    u.MoveTo(ume.x, ume.y);
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
                            fbGame.World.Players[ne.ID].Name,
                            ne.Name,
                            ne.ID
                        )
                    );
                    break;
            }
        }
    }
}
