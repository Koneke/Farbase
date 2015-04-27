using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;

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
}
