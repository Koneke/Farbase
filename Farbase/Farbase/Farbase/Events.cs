using System;
using Microsoft.Xna.Framework;

namespace Farbase
{
    public abstract class Event
    {
        public abstract string GetEventType();
    }

    public class NameEvent : Event
    {
        public const string EventType = "name";
        public override string GetEventType() { return EventType; }

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
        public const string EventType = "unit-move";
        public override string GetEventType() { return EventType; }

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
}
