using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;

namespace Farbase
{
    public class fbNetClient
    {
        public static bool Verbose = false;
        public static fbGame Game;

        private TcpClient client;
        private NetworkStream stream;
        private ASCIIEncoding encoder;

        public bool ShouldDie;
        private List<string> SendQueue;

        private void HandleMessage(string message)
        {
            string command, args;

            int split = message.IndexOf(':');
            command = message.Substring(0, split);
            args = message.Substring(split + 1, message.Length - (split + 1));
            List<String> arguments = args.Split(
                new char[] { ',' },
                StringSplitOptions.RemoveEmptyEntries
            ).ToList();

            if(Verbose)
                Game.Log.Add(
                    String.Format("<- s: {0}", message)
                );

            command = command.ToLower();

            int w, h;
            int x, y;
            int id;
            switch (command)
            {
                case "msg":
                    Game.Log.Add(args);
                    break;

                case "create-world":
                    Int32.TryParse(args.Split(',')[0], out w);
                    Int32.TryParse(args.Split(',')[1], out h);
                    Game.World = new fbWorld(w, h);
                    break;

                case "create-station":
                    Int32.TryParse(args.Split(',')[0], out x);
                    Int32.TryParse(args.Split(',')[1], out y);
                    Game.World.SpawnStation(x, y);
                    break;

                case "create-planet":
                    Int32.TryParse(args.Split(',')[0], out x);
                    Int32.TryParse(args.Split(',')[1], out y);
                    Game.World.SpawnPlanet(x, y);
                    break;

                case "new-player":
                    Int32.TryParse(args, out id);
                    Game.World.AddPlayer(
                        new Player(
                            "Unnamed player",
                            id,
                            Color.White
                        )
                    );
                    break;

                case "assign-id":
                    Int32.TryParse(args, out id);
                    Game.We = id;
                    break;

                case "name":
                    id = Int32.Parse(arguments[0]);
                    string name = arguments[1];
                    Player p = Game.World.Players[id];

                    Game.Log.Add(
                        string.Format(
                            "{0} is now known as {1}.",
                            p.Name,
                            name
                        )
                    );

                    //Game.World.Players[id].Name = name;
                    p.Name = name;
                    break;

                case "current-player":
                    int index = Int32.Parse(arguments[0]);
                    Game.World.CurrentPlayerIndex = index;
                    Game.Log.Add(
                        string.Format(
                            "It is now {0}'s turn.",
                            Game.World.CurrentPlayer.Name
                        )
                    );

                    break;

                default:
                    Game.Log.Add(
                        "Received {0} command from server," +
                        " but no idea what to do with it."
                    );
                    break;
            }
        }

        public void Start()
        {
            ShouldDie = false;
            SendQueue = new List<string>();
            ConnectTo("127.0.0.1", 7777);

            while (!ShouldDie)
            {
                if (stream.DataAvailable)
                {
                    byte[] buffer = new byte[4096];
                    int bytesRead = stream.Read(buffer, 0, 4096);

                    string message = encoder.GetString(buffer, 0, bytesRead);
                    foreach (string msg in message.Split('\n'))
                    {
                        if (msg == "") continue;
                        HandleMessage(msg);
                    }
                }

                lock (SendQueue)
                {
                    if (SendQueue.Count > 0)
                    {
                        send(SendQueue[0]);
                        SendQueue.RemoveAt(0);
                    }
                }
            }

            Close();
        }

        public void ConnectTo(string ip, int port)
        {
            if (client != null)
                client.Close();

            if (encoder == null)
                encoder = new ASCIIEncoding();

            client = new TcpClient();
            client.Connect(ip, port);
            stream = client.GetStream();
        }

        public void Send(string message)
        {
            SendQueue.Add(message);
        }

        private void send(string message)
        {
            byte[] buffer = encoder.GetBytes(message);
            stream.Write(buffer, 0, buffer.Length);
            stream.Flush();
        }

        public void Close()
        {
            if (client.Connected)
            {
                send("msg:Client disconnecting.");
                client.Close();
            }
            else
            {
                //this should not possibly happen?
                //just safeguarding I guess
                //threads hard bluh
                throw new Exception("Already closed.");
            }
        }
    }
}
