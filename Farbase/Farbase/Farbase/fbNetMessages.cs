using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Farbase
{
    public abstract class fbNetMessage
    {
        public bool Trash;

        protected fbNetMessage() {
            Trash = false;
        }

        public abstract int GetExpectedArguments();

        public abstract void ClientSideHandle(fbApplication app);

        public abstract string Format();

        public static fbNetMessage Spawn(
            fbApplication app,
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
        private string content;

        public override int GetExpectedArguments() { return 1; }

        public MsgMessage(List<string> arguments)
        {
            content = arguments[0];
        }

        public override void ClientSideHandle(fbApplication app)
        {
            app.Game.Log.Add(content);
        }

        public override string Format()
        {
            return string.Format(
                "{0}:{1}",
                Command,
                content
            );
        }
    }

    public class CreateWorldMessage : fbNetMessage
    {
        public const string Command = "create-world";
        public override int GetExpectedArguments() { return 2; }

        private int w, h;

        public CreateWorldMessage(List<string> arguments)
        {
            w = Int32.Parse(arguments[0]);
            h = Int32.Parse(arguments[1]);
        }

        public override void ClientSideHandle(fbApplication app)
        {
            fbGame.World = new fbWorld(w, h);
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

        private int x, y;

        public CreateStationMessage(List<string> arguments)
        {
            x = Int32.Parse(arguments[0]);
            y = Int32.Parse(arguments[1]);
        }

        public override void ClientSideHandle(fbApplication app)
        {
            fbGame.World.SpawnStation(x, y);
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

        private int x, y;

        public CreatePlanetMessage(
            List<string> arguments
        ) {
            x = Int32.Parse(arguments[0]);
            y = Int32.Parse(arguments[1]);
        }

        public override void ClientSideHandle(fbApplication app)
        {
            fbGame.World.SpawnPlanet(x, y);
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

        private string type;
        private int x, y, id, owner;

        public CreateUnitMessage(
            List<string> arguments
        ) {
            type = arguments[0];
            owner = Int32.Parse(arguments[1]);
            id = Int32.Parse(arguments[2]);
            x = Int32.Parse(arguments[3]);
            y = Int32.Parse(arguments[4]);
        }

        public override void ClientSideHandle(fbApplication app)
        {
            fbGame.World.SpawnUnit(type, owner, id, x, y);
        }

        public override string Format()
        {
            return string.Format(
                "{0}:{1},{2},{3},{4},{5}",
                Command,
                type,
                x, y,
                id, owner
            );
        }
    }

    public class MoveUnitMessage : fbNetMessage
    {
        public const string Command = "move";
        public override int GetExpectedArguments() { return 3; }

        private int id, x, y;

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

        public override void ClientSideHandle(fbApplication app)
        {
            fbGame.World.UnitLookup[id].MoveTo(x, y);
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

        private int id, amount;

        public SetUnitMovesMessage(
            List<string> arguments
        ) {
            id = Int32.Parse(arguments[0]);
            amount = Int32.Parse(arguments[1]);
        }

        public override void ClientSideHandle(fbApplication app)
        {
            fbGame.World.UnitLookup[id].Moves = amount;
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

        private int id;

        public NewPlayerMessage(
            List<string> arguments
        ) {
            id = Int32.Parse(arguments[0]);
        }

        public override void ClientSideHandle(fbApplication app)
        {
            fbGame.World.AddPlayer(
                new Player(
                    "Unnnamed player",
                    id,
                    Color.White
                )
            );
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

        private int id;

        public ReplenishPlayerMessage(
            List<string> arguments
        ) {
            id = Int32.Parse(arguments[0]);
        }

        public override void ClientSideHandle(fbApplication app)
        {
            fbGame.World.ReplenishPlayer(id);
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

        private int id;

        public AssignIDMessage(
            List<string> arguments
        ) {
            id = Int32.Parse(arguments[0]);
        }

        public override void ClientSideHandle(fbApplication app)
        {
            app.Game.We = id;
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

        private int id;
        private string name;
        private Color color;

        public NameMessage(
            List<string> arguments
        ) {
            id = Int32.Parse(arguments[0]);
            name = arguments[1];
            color = ExtensionMethods.ColorFromString(arguments[2]);
        }

        public override void ClientSideHandle(fbApplication app)
        {
            Player p = fbGame.World.Players[id];

            app.Game.Log.Add(
                string.Format(
                    "{0}<{2}> is now known as {1}<{2}>.",
                    p.Name,
                    name,
                    id
                )
            );

            p.Name = name;
            p.Color = color;
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

        private int index;

        public CurrentPlayerMessage(
            List<string> arguments
        ) {
            index = Int32.Parse(arguments[0]);
        }

        public override void ClientSideHandle(fbApplication app)
        {
            fbGame.World.CurrentPlayerIndex = index;
            app.Game.Log.Add(
                string.Format(
                    "It is now {0}'s turn.",
                    fbGame.World.CurrentPlayer.Name
                )
            );
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

        public ReadyMessage() { }

        public override void ClientSideHandle(fbApplication app)
        {
            app.Engine.NetClient.Ready = true;
        }

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
        public override int GetExpectedArguments() { return 0; }

        public PassMessage() { }

        public override void ClientSideHandle(fbApplication app)
        {
            //semi-weird spot here, this is a message strictly sent
            //  client -> server
            //handle has, SO FAR, only been used client side
            //we might want to make a ClientHandle() and ServerHandle()
            throw new NotImplementedException();
        }

        public override string Format()
        {
            return string.Format(
                "{0}",
                Command
            );
        }
    }
}
