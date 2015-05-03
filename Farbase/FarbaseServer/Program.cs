using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Farbase;
using Microsoft.Xna.Framework;

namespace FarbaseServer
{
    class Client
    {
        public static Dictionary<int, Client> TcpPlayers =
            new Dictionary<int, Client>();
        public static int IDCounter = 0;

        public int ID;
        public TcpClient tcpClient;
        public NetworkStream Stream;

        public Client(TcpClient client)
        {
            ID = IDCounter++;
            tcpClient = client;
            Stream = tcpClient.GetStream();
        }

        public void SendMessage(NetMessage3 message)
        {
            SendMessage(message.Format());
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

        private Random random;
        private List<Client> players;

        private void SendAll(
            NetMessage3 message,
            int except = -1
        ) {
            foreach(Client p in players)
                if (p.ID != except)
                    p.SendMessage(message);
        }

        private void BroadcastUnit(Unit u)
        {
            SendAll(
                new NetMessage3(
                    NM3MessageType.create_unit,
                    u.UnitType.Name,
                    u.Owner,
                    u.ID,
                    (int)u.Position.X,
                    (int)u.Position.Y
                )
            );
        }

        private void HandleMessage(NetMessage3 message)
        {
            switch (message.Signature.MessageType)
            {
                case NM3MessageType.message:
                    Console.WriteLine(
                        message.Get<string>("message")
                    );
                    break;

                case NM3MessageType.name_player:
                    Player p = fbGame.World.Players[message.Get<int>("id")];

                    p.Name = message.Get<string>("name");
                    p.Color = ExtensionMethods.ColorFromString(
                        message.Get<string>("color")
                    );

                    SendAll(message);
                    break;

                case NM3MessageType.pass_turn:
                    fbGame.World.CurrentPlayerIndex =
                        (fbGame.World.CurrentPlayerIndex + 1) %
                        fbGame.World.PlayerIDs.Count;

                    SendAll(
                        new NetMessage3(
                            NM3MessageType.current_player,
                            fbGame.World.CurrentPlayerIndex
                        )
                    );

                    fbGame.World.PassTo(fbGame.World.CurrentID);

                    SendAll(
                        new NetMessage3(
                            NM3MessageType.replenish_player,
                            fbGame.World.CurrentID
                        )
                    );
                    break;

                case NM3MessageType.move_unit:
                    Unit un;
                    //lock necessary?
                    lock (fbGame.World)
                    {
                        un = fbGame.World.UnitLookup
                            [message.Get<int>("id")];

                        if (un.Moves > 0)
                        {
                            un.MoveTo(
                                message.Get<int>("x"),
                                message.Get<int>("y")
                            );
                            un.Moves--;
                        }
                    }

                    //pass along to everyone else
                    SendAll(
                        message,
                        except: un.Owner
                    );

                    SendAll(
                        new NetMessage3(
                            NM3MessageType.set_unit_moves,
                            un.ID, un.Moves
                            ),
                        except: un.Owner
                    );
                    break;

                case NM3MessageType.dev_command:
                    switch (message.Get<int>("number"))
                    {
                        case 0:
                            Unit u = fbGame.World.SpawnUnit(
                                "scout",
                                message.Sender,
                                //we need to manually update the ID counter
                                fbGame.World.UnitIDCounter++,
                                10, 10
                            );
                            BroadcastUnit(u);

                            SendAll(
                                new NetMessage3(
                                    NM3MessageType.player_set_money,
                                    message.Sender,
                                    10
                                )
                            );
                            break;
                    }
                    break;

                case NM3MessageType.attack:
                    //please don't do shit until we've resolved combat
                    Client.TcpPlayers[message.Sender]
                        .SendMessage(
                            new NetMessage3(NM3MessageType.client_unready)
                        );

                    Unit attacker = fbGame.World.UnitLookup
                        [message.Get<int>("attackerid")];

                    Unit target = fbGame.World.UnitLookup
                        [message.Get<int>("targetid")];

                    int totalStrength = attacker.Strength + target.Strength;
                    int roll = random.Next(totalStrength) + 1;

                    Unit loser;
                    if (roll <= attacker.Strength)
                        loser = attacker;
                    else
                        loser = target;

                    loser.Hurt(1);
                    SendAll(
                        new NetMessage3(
                            NM3MessageType.hurt,
                            loser.ID,
                            1
                        )
                    );

                    //we done here
                    Client.TcpPlayers[message.Sender]
                        .SendMessage(
                            new NetMessage3(NM3MessageType.client_ready)
                        );
                    break;

                case NM3MessageType.build_unit:
                    Unit unit = new Unit(
                        UnitType.GetType(message.Get<string>("type")),
                        message.Get<int>("owner"),
                        fbGame.World.UnitIDCounter++,
                        message.Get<int>("x"),
                        message.Get<int>("y")
                    );

                    BroadcastUnit(unit);

                    int newMoney = fbGame.World.Players
                        [message.Sender]
                        .Money - unit.UnitType.Cost;

                    SendAll(
                        new NetMessage3(
                            NM3MessageType.player_set_money,
                            message.Sender,
                            newMoney
                        )
                    );
                    break;

                case NM3MessageType.station_buy_loyalty:
                    Player pl = fbGame.World.Players
                        //we could just use sender...?
                        [message.Get<int>("id")];

                    Station s =
                        fbGame.World.Map.At(
                            message.Get<int>("x"),
                            message.Get<int>("y")
                        )
                        .Station;

                    if (pl.DiplomacyPoints >= 20 && s != null)
                        s.AddLoyalty(pl.ID, 20);
                    else return;

                    SendAll(
                        new NetMessage3(
                            NM3MessageType.player_set_diplo,
                            pl.ID,
                            pl.DiplomacyPoints - 20
                        )
                    );

                    SendAll(
                        new NetMessage3(
                            NM3MessageType.station_set_loyalty,
                            pl.ID,
                            message.Get<int>("x"),
                            message.Get<int>("y"),
                            s.GetLoyalty(pl.ID)
                        )
                    );
                    break;

                default:
                    throw new ArgumentException();
            }
        }

        private void ReceiveMessage(Client source, string message)
        {
            NetMessage3 nm3 = new NetMessage3(message);
            nm3.Sender = source.ID;
            HandleMessage(nm3);
        }

        public void Start()
        {
            players = new List<Client>();
            random = new Random();

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
            NetMessage3.Setup();

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

        private void welcomeClient(Client p)
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
            Client p;

            lock (players)
            {
                p = new Client((TcpClient)clientObject);
                players.Add(p);
                Client.TcpPlayers.Add(p.ID, p);

                fbGame.World.AddPlayer(
                    new Player(
                        "Unnamed player",
                        p.ID,
                        Color.CornflowerBlue
                    )
                );
            }

            //set up world
            welcomeClient(p);

            foreach (int id in Client.TcpPlayers.Keys)
            {
                if (id == p.ID) continue;
                Client.TcpPlayers[id].SendMessage(
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
                        ReceiveMessage(p, msg);
                    }
                }
            }

            p.tcpClient.Close();
            lock (players)
            {
                players.Remove(p);
            }
        }
    }
}
