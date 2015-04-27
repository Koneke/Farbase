﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Farbase
{
    public abstract class fbNetMessage
    {
        public int Sender;
        public bool Trash;

        protected fbNetMessage()
        {
            Sender = -1; //no explicit sender/it's the server
            Trash = false;
        }

        public abstract string GetMessageType();
        public abstract int GetExpectedArguments();

        public abstract string Format();

        public static fbNetMessage Spawn(
            string command,
            List<string> arguments
        ) {
            fbNetMessage message;

            switch (command)
            {
                case MsgMessage.Command:
                    message = new MsgMessage(arguments);
                    break;

                case CreateWorldMessage.Command:
                    message = new CreateWorldMessage(arguments);
                    break;

                case CreateStationMessage.Command:
                    message = new CreateStationMessage(arguments);
                    break;

                case CreatePlanetMessage.Command:
                    message = new CreatePlanetMessage(arguments);
                    break;

                case CreateUnitMessage.Command:
                    message = new CreateUnitMessage(arguments);
                    break;

                case MoveUnitMessage.Command:
                    message = new MoveUnitMessage(arguments);
                    break;

                case SetUnitMovesMessage.Command:
                    message = new SetUnitMovesMessage(arguments);
                    break;

                case NewPlayerMessage.Command:
                    message = new NewPlayerMessage(arguments);
                    break;

                case ReplenishPlayerMessage.Command:
                    message = new ReplenishPlayerMessage(arguments);
                    break;

                case AssignIDMessage.Command:
                    message = new AssignIDMessage(arguments);
                    break;

                case NameMessage.Command:
                    message = new NameMessage(arguments);
                    break;

                case CurrentPlayerMessage.Command:
                    message = new CurrentPlayerMessage(arguments);
                    break;

                case ReadyMessage.Command:
                    message = new ReadyMessage();
                    break;

                case PassMessage.Command:
                    message = new PassMessage();
                    break;

                case DevCommandMessage.Command:
                    message = new DevCommandMessage(arguments);
                    break;

                default:
                    string argline = "";
                    if (arguments.Count > 0)
                        argline += arguments.Aggregate((c, n) => c + "," + n);

                    string msg =
                        string.Format(
                            "Received unknown command {0}:{1}.",
                            command,
                            argline
                        );

                    arguments = new List<string> { msg };
                    message = new MsgMessage(arguments);

                    message.Trash = true;
                    break;
            }

            if (arguments.Count != message.GetExpectedArguments())
                throw new ArgumentException();

            return message;
        }
    }

    public class MsgMessage : fbNetMessage
    {
        public const string Command = "msg";
        public string Content;

        public override int GetExpectedArguments() { return 1; }
        public override string GetMessageType() { return Command; }

        public MsgMessage(List<string> arguments)
        {
            Content = arguments[0];
        }

        public MsgMessage(string text)
        {
            Content = text;
        }

        public override string Format()
        {
            return string.Format(
                "{0}:{1}",
                Command,
                Content
            );
        }
    }

    public class CreateWorldMessage : fbNetMessage
    {
        public const string Command = "create-world";
        public override int GetExpectedArguments() { return 2; }
        public override string GetMessageType() { return Command; }

        public int w;
        public int h;

        public CreateWorldMessage(List<string> arguments)
        {
            w = Int32.Parse(arguments[0]);
            h = Int32.Parse(arguments[1]);
        }

        public override string Format()
        {
            return string.Format(
                "{0}:{1},{2}",
                Command,
                w, h
            );
        }
    }

    public class CreateStationMessage : fbNetMessage
    {
        public const string Command = "create-station";
        public override int GetExpectedArguments() { return 2; }
        public override string GetMessageType() { return Command; }

        public int x;
        public int y;

        public CreateStationMessage(List<string> arguments)
        {
            x = Int32.Parse(arguments[0]);
            y = Int32.Parse(arguments[1]);
        }

        public override string Format()
        {
            return string.Format(
                "{0}:{1},{2}",
                Command,
                x, y
            );
        }
    }

    public class CreatePlanetMessage : fbNetMessage
    {
        public const string Command = "create-planet";
        public override int GetExpectedArguments() { return 2; }
        public override string GetMessageType() { return Command; }

        public int x;
        public int y;

        public CreatePlanetMessage(
            List<string> arguments
        ) {
            x = Int32.Parse(arguments[0]);
            y = Int32.Parse(arguments[1]);
        }

        public override string Format()
        {
            return string.Format(
                "{0}:{1},{2}",
                Command,
                x, y
            );
        }
    }

    public class CreateUnitMessage : fbNetMessage
    {
        public const string Command = "create-unit";
        public override int GetExpectedArguments() { return 5; }
        public override string GetMessageType() { return Command; }

        public string type;
        public int id;
        public int owner;
        public int x;
        public int y;

        public CreateUnitMessage(
            List<string> arguments
        ) {
            type = arguments[0];
            owner = Int32.Parse(arguments[1]);
            id = Int32.Parse(arguments[2]);
            x = Int32.Parse(arguments[3]);
            y = Int32.Parse(arguments[4]);
        }

        public CreateUnitMessage(
            string type,
            int owner,
            int id,
            int x,
            int y
        ) {
            this.type = type;
            this.owner = owner;
            this.id = id;
            this.x = x;
            this.y = y;
        }

        public override string Format()
        {
            return string.Format(
                "{0}:{1},{2},{3},{4},{5}",
                Command,
                type,
                id, owner,
                x, y
            );
        }
    }

    public class MoveUnitMessage : fbNetMessage
    {
        public const string Command = "move";
        public override int GetExpectedArguments() { return 3; }
        public override string GetMessageType() { return Command; }

        public int id;
        public int x;
        public int y;

        public MoveUnitMessage(
            List<string> arguments
        ) {
            id = Int32.Parse(arguments[0]);
            x = Int32.Parse(arguments[1]);
            y = Int32.Parse(arguments[2]);
        }

        public MoveUnitMessage(
            int id,
            int x,
            int y
        ) {
            this.id = id;
            this.x = x;
            this.y = y;
        }

        public override string Format()
        {
            return string.Format(
                "{0}:{1},{2},{3}",
                Command,
                id, x, y
            );
        }
    }

    public class SetUnitMovesMessage : fbNetMessage
    {
        public const string Command = "set-moves";
        public override int GetExpectedArguments() { return 2; }
        public override string GetMessageType() { return Command; }

        public int id;
        public int amount;

        public SetUnitMovesMessage(
            List<string> arguments
        ) {
            id = Int32.Parse(arguments[0]);
            amount = Int32.Parse(arguments[1]);
        }

        public SetUnitMovesMessage(
            int id,
            int amount
        ) {
            this.id = id;
            this.amount = amount;
        }

        public override string Format()
        {
            return string.Format(
                "{0}:{1},{2}",
                Command,
                id, amount
            );
        }
    }

    public class NewPlayerMessage : fbNetMessage
    {
        public const string Command = "new-player";
        public override int GetExpectedArguments() { return 1; }
        public override string GetMessageType() { return Command; }

        public int id;

        public NewPlayerMessage(
            List<string> arguments
        ) {
            id = Int32.Parse(arguments[0]);
        }

        public override string Format()
        {
            return string.Format(
                "{0}:{1}",
                Command,
                id
            );
        }
    }

    public class ReplenishPlayerMessage : fbNetMessage
    {
        public const string Command = "replenish";
        public override int GetExpectedArguments() { return 1; }
        public override string GetMessageType() { return Command; }

        public int id;

        public ReplenishPlayerMessage(
            List<string> arguments
        ) {
            id = Int32.Parse(arguments[0]);
        }

        public ReplenishPlayerMessage(
            int id
        ) {
            this.id = id;
        }

        public override string Format()
        {
            return string.Format(
                "{0}:{1}",
                Command,
                id
            );
        }
    }

    public class AssignIDMessage : fbNetMessage
    {
        public const string Command = "assign-id";
        public override int GetExpectedArguments() { return 1; }
        public override string GetMessageType() { return Command; }

        public int id;

        public AssignIDMessage(
            List<string> arguments
        ) {
            id = Int32.Parse(arguments[0]);
        }

        public override string Format()
        {
            return string.Format(
                "{0}:{1}",
                Command,
                id
            );
        }
    }

    public class NameMessage : fbNetMessage
    {
        public const string Command = "name";
        public override int GetExpectedArguments() { return 3; }
        public override string GetMessageType() { return Command; }

        public int id;
        public string name;
        public Color color;

        public NameMessage(
            List<string> arguments
        ) {
            id = Int32.Parse(arguments[0]);
            name = arguments[1];
            color = ExtensionMethods.ColorFromString(arguments[2]);
        }

        public NameMessage(
            int id,
            string name,
            Color color
        ) {
            this.id = id;
            this.name = name;
            this.color = color;
        }

        public override string Format()
        {
            return string.Format(
                "{0}:{1},{2},{3}",
                Command,
                id,
                name,
                ExtensionMethods.ColorToString(color)
            );
        }
    }

    public class CurrentPlayerMessage : fbNetMessage
    {
        public const string Command = "current-player";
        public override int GetExpectedArguments() { return 1; }
        public override string GetMessageType() { return Command; }

        public int index;

        public CurrentPlayerMessage(
            List<string> arguments
        ) {
            index = Int32.Parse(arguments[0]);
        }

        public CurrentPlayerMessage(
            int index
        ) {
            this.index = index;
        }

        public override string Format()
        {
            return string.Format(
                "{0}:{1}",
                Command,
                index
            );
        }
    }

    public class ReadyMessage : fbNetMessage
    {
        public const string Command = "ready";
        public override int GetExpectedArguments() { return 0; }
        public override string GetMessageType() { return Command; }

        public override string Format()
        {
            return string.Format(
                "{0}",
                Command
            );
        }
    }

    public class PassMessage : fbNetMessage
    {
        public const string Command = "pass";
        public override string GetMessageType() { return Command; }
        public override int GetExpectedArguments() { return 0; }

        public override string Format()
        {
            return string.Format(
                "{0}",
                Command
            );
        }
    }

    public class DevCommandMessage : fbNetMessage
    {
        public const string Command = "dev";
        public override string GetMessageType() { return Command; }
        public override int GetExpectedArguments() { return 1; }

        public int Number;

        public DevCommandMessage(List<string> arguments)
        {
            Number = Int32.Parse(arguments[0]);
        }

        public DevCommandMessage(int number)
        {
            Number = number;
        }

        public override string Format()
        {
            return string.Format(
                "{0}:{1}",
                Command,
                Number
            );
        }
    }
}
