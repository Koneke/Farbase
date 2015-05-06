using System;
using Microsoft.Xna.Framework;

namespace Farbase
{
    public enum EventType
    {
        NameEvent,
        UnitMoveEvent,
        BuildUnitEvent,
        BuildStationEvent,
        PlayerDisconnect,
        CreateUnitEvent,
        DestroyUnitEvent
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

    public class PlayerDisconnectEvent : Event
    {
        public const EventType Type = EventType.PlayerDisconnect;
        public override EventType GetEventType() { return Type; }

        public int id;

        public PlayerDisconnectEvent(int id)
        {
            this.id = id;
        }
    }

    public class CreateUnitEvent : Event
    {
        public const EventType Type = EventType.CreateUnitEvent;
        public override EventType GetEventType() { return Type; }

        public UnitType UnitType;
        public int Owner, ID, x, y;

        public CreateUnitEvent(
            UnitType unitType,
            int owner,
            int id,
            int x,
            int y
        ) {
            UnitType = unitType;
            Owner = owner;
            ID = id;
            this.x = x;
            this.y = y;
        }
    }
}
