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
        protected abstract void Handle(Event e);
    }

    public class GameEventHandler : fbEventHandler
    {
        protected override void Handle(Event e)
        {
            switch (e.GetEventType())
            {
                case EventType.NameEvent:
                    break;

                case EventType.UnitMoveEvent:
                    break;

                default:
                    throw new ArgumentException();
            }
        }
    }
}
