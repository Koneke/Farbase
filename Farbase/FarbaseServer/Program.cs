using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Farbase;
using Microsoft.Xna.Framework;

namespace FarbaseServer
{
    class Player
    {
        public static Dictionary<int, Player> TcpPlayers =
            new Dictionary<int, Player>();
        public static int IDCounter = 0;

        public int ID;
        public TcpClient Client;
        public NetworkStream Stream;

        public Player(TcpClient client)
        {
            ID = IDCounter++;
            Client = client;
            Stream = client.GetStream();
        }

        public void SendMessage(string message)
        {
            Console.WriteLine("-> {0}: {1}", ID, message);
            byte[] buffer = Program.encoder.GetBytes(message + '\n');
            Stream.Write(buffer, 0, buffer.Length);
            Stream.Flush();
        }
    }

    class Program
    {
        static void Main()
        {
            Program program = new Program();
            program.Start();
        }

        private List<Player> players;

        private void SendAll(
            string message,
            int except = -1
        ) {
            foreach (Player p in players)
                if (p.ID != except)
                    p.SendMessage(message);
        }

        private void SendAll(
            string message,
            List<int> exceptions 
        ) {
            foreach (Player p in players)
                if(!exceptions.Contains(p.ID))
                    p.SendMessage(message);
        }

        private void BroadcastUnit(Unit u)
        {
            SendAll(
                string.Format(
                    "create-unit:{0},{1},{2},{3},{4}",
                    u.UnitType.Name,
                    u.Owner,
                    u.ID,
                    (int)u.Position.X,
                    (int)u.Position.Y
                )
            );
        }

        private void HandleMessage(Player source, string message)
        {
            string command, args;

            int split = message.IndexOf(':');
            if (split >= 0)
            {
                command = message.Substring(0, split);
                args = message.Substring(
                    split + 1,
                    message.Length - (split + 1)
                    );
            }
            else
            {
                command = message;
                args = "";
            }

            List<String> arguments = args.Split(
                new[] { ',' },
                StringSplitOptions.RemoveEmptyEntries
            ).ToList();

            Console.WriteLine("<- {0}: {1}", source.ID, message);

            fbNetMessage msg = fbNetMessage.Spawn(command, arguments);

            switch (command)
            {
                case "msg":
                    Console.WriteLine(args);
                    break;

                case "login":
                    fbGame.World.Players[source.ID].Name = arguments[0];
                    fbGame.World.Players[source.ID].Color =
                        ExtensionMethods.ColorFromString(arguments[1]);

                    SendAll(
                        string.Format(
                            "name:{0},{1},{2}",
                            source.ID,
                            arguments[0],
                            arguments[1]
                        )
                    );
                    break;

                case "pass":
                    fbGame.World.CurrentPlayerIndex =
                        (fbGame.World.CurrentPlayerIndex + 1) %
                        fbGame.World.PlayerIDs.Count;
                    SendAll(
                        string.Format(
                            "current-player:{0}",
                            fbGame.World.CurrentPlayerIndex
                        )
                    );
                    fbGame.World.ReplenishPlayer(fbGame.World.CurrentID);
                    SendAll(
                        string.Format(
                            "replenish:{0}",
                            fbGame.World.CurrentID
                        )
                    );
                    break;

                case "give-test-scout":
                    Unit u = fbGame.World.SpawnUnit(
                        "scout",
                        source.ID,
                        //we need to manually update the ID counter
                        fbGame.World.UnitIDCounter++,
                        10, 10
                    );
                    BroadcastUnit(u);
                    break;

                case "move":
                    int id = Int32.Parse(arguments[0]);
                    int x = Int32.Parse(arguments[1]);
                    int y = Int32.Parse(arguments[2]);

                    Unit un;
                    //lock necessary?
                    lock (fbGame.World)
                    {
                        un = fbGame.World.UnitLookup[id];

                        if (un.Moves > 0)
                        {
                            un.MoveTo(x, y);
                            un.Moves--;
                        }
                    }

                    //should send to everyone except the source
                    //which should already have made the movements locally
                    SendAll(
                        string.Format(
                            "move:{0},{1},{2}",
                            un.ID,
                            x, y
                        ),
                        source.ID
                    );
                    SendAll(
                        string.Format(
                            "set-moves:{0},{1}",
                            un.ID,
                            un.Moves
                        ),
                        source.ID
                    );

                    break;

                default:
                    Console.WriteLine(
                        "Received {0} command from id {1}," +
                        " but no idea what to do with it.",
                        command, source.ID
                    );
                    break;
            }
        }

        public void Start()
        {
            players = new List<Player>();

            UnitType scout = new UnitType();
            scout.Moves = 2;
            scout.Strength = 3;
            scout.Attacks = 1;
            UnitType.RegisterType("scout", scout);

            UnitType worker = new UnitType();
            worker.Moves = 1;
            worker.Strength = 1;
            UnitType.RegisterType("worker", worker);

            fbGame.World = new fbWorld(80, 45);
            fbGame.World.SpawnStation(10, 12);
            fbGame.World.SpawnPlanet(14, 14);

            netStart();
        }

        // ~~~ ABANDON HOPE ALL YE WHO ENTER HERE ~~~
        //       ~~~ FOR HERE BE NETWORKING ~~~
        //    ~~~ TURN AWAY WHILE YE STILL CAN ~~~

        private TcpListener listener;
        private Thread listenThread;
        public static ASCIIEncoding encoder;

        private bool die;

        private void netStart()
        {
            encoder = new ASCIIEncoding();

            try
            {
                listener = new TcpListener(IPAddress.Any, 7707);
                listenThread = new Thread(listen);
                listenThread.Start();

                Console.WriteLine("Running at 7707.");
                Console.WriteLine("Local EP is: " + listener.LocalEndpoint);
                Console.WriteLine("Waiting...");

                Console.ReadLine();
                die = true;
                listener.Stop();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
        }
        private void listen()
        {
            listener.Start();

            while (!die)
            {
                if (!listener.Pending())
                {
                    Thread.Sleep(500);
                    continue;
                }

                TcpClient client = listener.AcceptTcpClient();
                if (die) break;

                Console.WriteLine("New client connected!");

                Thread clientThread = new Thread(handleClient);
                clientThread.Start(client);
            }
        }

        private void welcomeClient(Player p)
        {
            p.SendMessage("msg:Welcome to Farbase.");
            p.SendMessage("msg:Your ID is " + p.ID + ".");
            p.SendMessage(
                string.Format(
                    "assign-id:{0}",
                    p.ID
                )
            );

            p.SendMessage(
                string.Format(
                    "create-world:{0},{1}",
                    fbGame.World.Map.Width,
                    fbGame.World.Map.Height
                )
            );

            //tell new client about all existing players
            foreach (int id in fbGame.World.PlayerIDs)
            {
                p.SendMessage(
                    string.Format(
                        "new-player:{0}",
                        id
                    )
                );

                p.SendMessage(
                    string.Format(
                        "name:{0},{1},{2}",
                        id,
                        fbGame.World.Players[id].Name,
                        ExtensionMethods.ColorToString(
                            fbGame.World.Players[id].Color
                        )
                    )
                );
            }

            for (int x = 0; x < fbGame.World.Map.Width; x++)
            for (int y = 0; y < fbGame.World.Map.Height; y++)
            {
                if (fbGame.World.Map.At(x, y).Station != null)
                    p.SendMessage(
                        string.Format(
                            "create-station:{0},{1}",
                            x, y
                        )
                    );
                if (fbGame.World.Map.At(x, y).Planet != null)
                    p.SendMessage(
                        string.Format(
                            "create-planet:{0},{1}",
                            x, y
                        )
                    );

                Unit u = fbGame.World.Map.At(x, y).Unit;
                if (u != null)
                {
                    p.SendMessage(
                        string.Format(
                            "create-unit:{0},{1},{2},{3},{4}",
                            u.UnitType.Name,
                            u.Owner,
                            u.ID,
                            (int)u.Position.X,
                            (int)u.Position.Y
                        )
                    );
                }
            }

            //tell new client about whose turn it is
            p.SendMessage(
                string.Format(
                    "current-player:{0}",
                    fbGame.World.CurrentPlayerIndex
                )
            );

            //we gucci now
            p.SendMessage("ready");
        }

        private void handleClient(object clientObject)
        {
            Player p;

            lock (players)
            {
                p = new Player((TcpClient)clientObject);
                players.Add(p);
                Player.TcpPlayers.Add(p.ID, p);

                fbGame.World.AddPlayer(
                    new Farbase.Player(
                        "Unnamed player",
                        p.ID,
                        Color.CornflowerBlue
                    )
                );
            }

            //set up world
            welcomeClient(p);

            foreach (int id in Player.TcpPlayers.Keys)
            {
                if (id == p.ID) continue;
                Player.TcpPlayers[id].SendMessage(
                    string.Format(
                        "new-player:{0}",
                        p.ID
                    )
                );
            }

            byte[] message = new byte[4096];
            int bytesRead;

            while (!die)
            {
                bytesRead = 0;

                try
                {
                    if(p.Stream.DataAvailable)
                        bytesRead = p.Stream.Read(message, 0, 4096);
                }
                catch
                {
                    //socket error
                    Console.WriteLine("Client died.");
                    break;
                }

                if (bytesRead > 0)
                {
                    string pmessage = encoder.GetString(message, 0, bytesRead);
                    foreach (string msg in pmessage.Split('\n'))
                    {
                        if (msg == "" || msg[0] == '\0') continue;
                        HandleMessage(p, msg);
                    }
                }
            }

            p.Client.Close();
            lock (players)
            {
                players.Remove(p);
            }
        }
    }
}
