using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Farbase
{
    public abstract class fbNetMessage
    {
        protected fbApplication application;

        public bool Trash;

        protected fbNetMessage(
            fbApplication app
        ) {
            application = app;
            Trash = false;
        }

        public abstract int GetExpectedArguments();

        public abstract void Handle();

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
                    message = new MsgMessage(app, arguments);
                    break;

                case CreateWorldMessage.Command:
                    message = new CreateWorldMessage(app, arguments);
                    break;

                case CreateStationMessage.Command:
                    message = new CreateStationMessage(app, arguments);
                    break;

                case CreatePlanetMessage.Command:
                    message = new CreatePlanetMessage(app, arguments);
                    break;

                case CreateUnitMessage.Command:
                    message = new CreateUnitMessage(app, arguments);
                    break;

                case MoveUnitMessage.Command:
                    message = new MoveUnitMessage(app, arguments);
                    break;

                case SetUnitMovesMessage.Command:
                    message = new SetUnitMovesMessage(app, arguments);
                    break;

                case NewPlayerMessage.Command:
                    message = new NewPlayerMessage(app, arguments);
                    break;

                case ReplenishPlayerMessage.Command:
                    message = new ReplenishPlayerMessage(app, arguments);
                    break;

                case AssignIDMessage.Command:
                    message = new AssignIDMessage(app, arguments);
                    break;

                case NameMessage.Command:
                    message = new NameMessage(app, arguments);
                    break;

                case CurrentPlayerMessage.Command:
                    message = new CurrentPlayerMessage(app, arguments);
                    break;

                case ReadyMessage.Command:
                    message = new ReadyMessage(app);
                    break;

                case PassMessage.Command:
                    message = new PassMessage(app);
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
                    message = new MsgMessage(
                        app,
                        arguments
                    );

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

        public MsgMessage(
            fbApplication application,
            List<string> arguments
        ) : base(application) {
            content = arguments[0];
        }

        public override void Handle()
        {
            application.Game.Log.Add(content);
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

        public CreateWorldMessage(
            fbApplication app,
            List<string> arguments
        ) : base(app) {
            w = Int32.Parse(arguments[0]);
            h = Int32.Parse(arguments[1]);
        }

        public override void Handle()
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

        public CreateStationMessage(
            fbApplication app,
            List<string> arguments
        ) : base(app) {
            x = Int32.Parse(arguments[0]);
            y = Int32.Parse(arguments[1]);
        }

        public override void Handle()
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
            fbApplication app,
            List<string> arguments
        ) : base(app) {
            x = Int32.Parse(arguments[0]);
            y = Int32.Parse(arguments[1]);
        }

        public override void Handle()
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
            fbApplication app,
            List<string> arguments
        ) : base(app) {
            type = arguments[0];
            owner = Int32.Parse(arguments[1]);
            id = Int32.Parse(arguments[2]);
            x = Int32.Parse(arguments[3]);
            y = Int32.Parse(arguments[4]);
        }

        public override void Handle()
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
            fbApplication app,
            List<string> arguments
        ) : base(app) {
            id = Int32.Parse(arguments[0]);
            x = Int32.Parse(arguments[1]);
            y = Int32.Parse(arguments[2]);
        }

        public MoveUnitMessage(
            fbApplication app,
            int id,
            int x,
            int y
        ) : base(app) {
            this.id = id;
            this.x = x;
            this.y = y;
        }

        public override void Handle()
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
            fbApplication app,
            List<string> arguments
        ) : base(app) {
            id = Int32.Parse(arguments[0]);
            amount = Int32.Parse(arguments[1]);
        }

        public override void Handle()
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
            fbApplication app,
            List<string> arguments
        ) : base(app) {
            id = Int32.Parse(arguments[0]);
        }

        public override void Handle()
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
            fbApplication app,
            List<string> arguments
        ) : base(app) {
            id = Int32.Parse(arguments[0]);
        }

        public override void Handle()
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
            fbApplication app,
            List<string> arguments
        ) : base(app) {
            id = Int32.Parse(arguments[0]);
        }

        public override void Handle()
        {
            application.Game.We = id;
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
            fbApplication app,
            List<string> arguments
        ) : base(app) {
            id = Int32.Parse(arguments[0]);
            name = arguments[1];
            color = ExtensionMethods.ColorFromString(arguments[2]);
        }

        public override void Handle()
        {
            Player p = fbGame.World.Players[id];

            application.Game.Log.Add(
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
            fbApplication app,
            List<string> arguments
        ) : base(app) {
            index = Int32.Parse(arguments[0]);
        }

        public override void Handle()
        {
            fbGame.World.CurrentPlayerIndex = index;
            application.Game.Log.Add(
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

        public ReadyMessage(fbApplication app) : base (app) { }

        public override void Handle()
        {
            application.Engine.NetClient.Ready = true;
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

        public PassMessage(fbApplication app) : base(app) { }

        public override void Handle()
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
