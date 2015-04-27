using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Farbase
{
    public abstract class fbNetMessage
    {
        protected fbApplication application;
        protected string command;
        protected List<string> arguments;

        public bool Trash;

        protected fbNetMessage(
            fbApplication app,
            string cmd,
            List<string> args
        ) {
            application = app;
            command = cmd;
            arguments = args;
            Trash = false;
        }

        public abstract int GetExpectedArguments();

        public abstract void Handle();

        public static fbNetMessage Spawn(
            fbApplication app,
            string command,
            List<string> arguments
        ) {
            fbNetMessage message = null;
            switch (command)
            {
                case "msg":
                    message = new MsgMessage(app, command, arguments);
                    break;

                case "create-world":
                    message = new CreateWorldMessage(app, command, arguments);
                    break;

                case "create-station":
                    message = new CreateStationMessage(app, command, arguments);
                    break;

                case "create-planet":
                    message = new CreatePlanetMessage(app, command, arguments);
                    break;

                case "create-unit":
                    message = new CreateUnitMessage(app, command, arguments);
                    break;

                case "move":
                    message = new MoveUnitMessage(app, command, arguments);
                    break;

                case "set-moves":
                    message = new SetUnitMovesMessage(app, command, arguments);
                    break;

                case "new-player":
                    message = new NewPlayerMessage(app, command, arguments);
                    break;

                case "replenish":
                    message = new ReplenishPlayerMessage(
                        app,
                        command,
                        arguments
                    );
                    break;

                case "assign-id":
                    message = new AssignIDMessage(app, command, arguments);
                    break;

                case "name":
                    message = new NameMessage(app, command, arguments);
                    break;

                case "current-player":
                    message = new CurrentPlayerMessage(app, command, arguments);
                    break;

                case "ready":
                    message = new ReadyMessage(app, command, arguments);
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
                        "msg",
                        arguments
                    );

                    message.Trash = true;
                    break;
            }

            if (arguments.Count != message.GetExpectedArguments())
                throw new ArgumentException();

            return message;
        }

        public string Format()
        {
            return string.Format(
                "{0}:{1}",
                command,
                string.Join(",", arguments)
            );
        }
    }

    public class MsgMessage : fbNetMessage
    {
        private string content;

        public override int GetExpectedArguments() { return 1; }

        public MsgMessage(
            fbApplication application,
            string command,
            List<string> arguments
        ) : base(application, command, arguments) {
            content = arguments[0];
        }

        public override void Handle()
        {
            application.Game.Log.Add(content);
        }
    }

    public class CreateWorldMessage : fbNetMessage
    {
        public override int GetExpectedArguments() { return 2; }

        private int w, h;

        public CreateWorldMessage(
            fbApplication app,
            string command,
            List<string> arguments
        ) : base(app, command, arguments) {
            w = Int32.Parse(arguments[0]);
            h = Int32.Parse(arguments[1]);
        }

        public override void Handle()
        {
            fbGame.World = new fbWorld(w, h);
        }
    }

    public class CreateStationMessage : fbNetMessage
    {
        public override int GetExpectedArguments() { return 2; }

        private int x, y;

        public CreateStationMessage(
            fbApplication app,
            string command,
            List<string> arguments
        ) : base(app, command, arguments) {
            x = Int32.Parse(arguments[0]);
            y = Int32.Parse(arguments[1]);
        }

        public override void Handle()
        {
            fbGame.World.SpawnStation(x, y);
        }
    }

    public class CreatePlanetMessage : fbNetMessage
    {
        public override int GetExpectedArguments() { return 2; }

        private int x, y;

        public CreatePlanetMessage(
            fbApplication app,
            string command,
            List<string> arguments
        ) : base(app, command, arguments) {
            x = Int32.Parse(arguments[0]);
            y = Int32.Parse(arguments[1]);
        }

        public override void Handle()
        {
            fbGame.World.SpawnPlanet(x, y);
        }
    }

    public class CreateUnitMessage : fbNetMessage
    {
        public override int GetExpectedArguments() { return 5; }

        private string type;
        private int x, y, id, owner;

        public CreateUnitMessage(
            fbApplication app,
            string command,
            List<string> arguments
        ) : base(app, command, arguments) {
            type = arguments[0];
            owner = Int32.Parse(arguments[1]);
            owner = Int32.Parse(arguments[2]);
            x = Int32.Parse(arguments[3]);
            y = Int32.Parse(arguments[4]);
        }

        public override void Handle()
        {
            fbGame.World.SpawnUnit(type, owner, id, x, y);
        }
    }

    public class MoveUnitMessage : fbNetMessage
    {
        public override int GetExpectedArguments() { return 3; }

        private int id, x, y;

        public MoveUnitMessage(
            fbApplication app,
            string command,
            List<string> arguments
        ) : base(app, command, arguments) {
            id = Int32.Parse(arguments[0]);
            x = Int32.Parse(arguments[1]);
            y = Int32.Parse(arguments[2]);
        }

        public override void Handle()
        {
            fbGame.World.UnitLookup[id].MoveTo(x, y);
        }
    }

    public class SetUnitMovesMessage : fbNetMessage
    {
        public override int GetExpectedArguments() { return 2; }

        private int id, amount;

        public SetUnitMovesMessage(
            fbApplication app,
            string command,
            List<string> arguments
        ) : base(app, command, arguments) {
            id = Int32.Parse(arguments[0]);
            amount = Int32.Parse(arguments[1]);
        }

        public override void Handle()
        {
            fbGame.World.UnitLookup[id].Moves = amount;
        }
    }

    public class NewPlayerMessage : fbNetMessage
    {
        public override int GetExpectedArguments() { return 1; }

        private int id;

        public NewPlayerMessage(
            fbApplication app,
            string command,
            List<string> arguments
        ) : base(app, command, arguments) {
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
    }

    public class ReplenishPlayerMessage : fbNetMessage
    {
        public override int GetExpectedArguments() { return 1; }

        private int id;

        public ReplenishPlayerMessage(
            fbApplication app,
            string command,
            List<string> arguments
        ) : base(app, command, arguments) {
            id = Int32.Parse(arguments[0]);
        }

        public override void Handle()
        {
            fbGame.World.ReplenishPlayer(id);
        }
    }

    public class AssignIDMessage : fbNetMessage
    {
        public override int GetExpectedArguments() { return 1; }

        private int id;

        public AssignIDMessage(
            fbApplication app,
            string command,
            List<string> arguments
        ) : base(app, command, arguments) {
            id = Int32.Parse(arguments[0]);
        }

        public override void Handle()
        {
            application.Game.We = id;
        }
    }

    public class NameMessage : fbNetMessage
    {
        public override int GetExpectedArguments() { return 3; }

        private int id;
        private string name;
        private Color color;

        public NameMessage(
            fbApplication app,
            string command,
            List<string> arguments
        ) : base(app, command, arguments) {
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
    }

    public class CurrentPlayerMessage : fbNetMessage
    {
        public override int GetExpectedArguments() { return 1; }

        private int index;

        public CurrentPlayerMessage(
            fbApplication app,
            string command,
            List<string> arguments
        ) : base(app, command, arguments) {
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
    }

    public class ReadyMessage : fbNetMessage
    {
        public override int GetExpectedArguments() { return 0; }

        public ReadyMessage(
            fbApplication app,
            string command,
            List<string> arguments
        ) : base (app, command, arguments) {
        }

        public override void Handle()
        {
            application.Engine.NetClient.Ready = true;
        }
    }
}
