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
        DestroyUnitEvent,
        SetProjectEvent,
        ProjectFinishedEvent
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
        public bool Local;

        public UnitMoveEvent(
            int id,
            int x,
            int y,
            bool local
        ) {
            ID = id;
            this.x = x;
            this.y = y;
            Local = local;
        }

        public UnitMoveEvent(
            int id,
            Vector2i pos,
            bool local
        ) {
            ID = id;
            x = pos.X;
            y = pos.Y;
            Local = local;
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

        public UnitTypes UnitType;
        public int Owner;
        public int x, y;

        public BuildUnitEvent(
            UnitTypes unitType,
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

    public class SetProjectEvent : Event
    {
        public const EventType Type = EventType.SetProjectEvent;
        public override EventType GetEventType() { return Type; }

        public int Owner, Station;
        public ProjectType ProjectType;
        //not really an int, just int form of a suitable enum
        public int Project;

        public SetProjectEvent(
            int owner,
            int station,
            ProjectType projectType,
            int project
        ) {
            Owner = owner;
            Station = station;
            ProjectType = projectType;
            Project = project;
        }
    }

    public class ProjectFinishedEvent : Event
    {
        public const EventType Type = EventType.ProjectFinishedEvent;
        public override EventType GetEventType() { return Type; }

        public Project Project;

        public ProjectFinishedEvent(Project project)
        {
            Project = project;
        }
    }
}
