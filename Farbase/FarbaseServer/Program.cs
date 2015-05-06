using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Microsoft.Xna.Framework;
using Farbase;

namespace FarbaseServer
{
    class Client
    {
        public static int IDCounter = 0;

        public int ID;
        public TcpClient tcpClient;
        public NetworkStream Stream;
        public bool Disconnected;

        public Client(TcpClient client)
        {
            ID = IDCounter++;
            tcpClient = client;
            Stream = tcpClient.GetStream();
        }

        public void SendMessage(NetMessage3 message)
        {
            Console.WriteLine("-> {0}: {1}", ID, message.Format());
            byte[] buffer = Program.encoder.GetBytes(
                message.Format() + '\n'
            );
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

        public Dictionary<int, Client> TcpPlayers =
            new Dictionary<int, Client>();

        private fbWorld World;

        private void SendAll(
            NetMessage3 message,
            int except = -1
        ) {
            foreach(int id in TcpPlayers.Keys)
                if (id != except)
                   TcpPlayers[id].SendMessage(message);
        }

        private void BroadcastUnit(Unit u)
        {
            SendAll(
                new NetMessage3(
                    NM3MessageType.unit_create,
                    u.UnitType.Name,
                    u.Owner,
                    u.ID,
                    u.Position.X,
                    u.Position.Y
                )
            );
        }

        private void HandleMessage(NetMessage3 message)
        {
            Console.WriteLine(
                "<- {0}: {1}", 
                message.Sender,
                message.Format()
            );

            switch (message.Signature.MessageType)
            {
                case NM3MessageType.message:
                    Console.WriteLine(
                        message.Get<string>("message")
                    );
                    break;

                case NM3MessageType.player_name:
                    Player p = World.GetPlayer(message.Get<int>("id"));

                    p.Name = message.Get<string>("name");
                    p.Color = ExtensionMethods.ColorFromString(
                        message.Get<string>("color")
                    );

                    SendAll(message);
                    break;

                case NM3MessageType.client_pass:
                    Pass();
                    break;

                case NM3MessageType.unit_move:
                    Unit un;
                    //lock necessary?
                    lock (World)
                    {
                        un = World.UnitLookup
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
                            NM3MessageType.unit_status,
                            un.ID,
                            un.Moves,
                            un.Attacks,
                            un.Strength
                        ),
                        except: un.Owner
                    );
                    break;

                case NM3MessageType.dev_command:
                    switch (message.Get<int>("number"))
                    {
                        case 0:
                            Unit u = World.SpawnUnit(
                                "scout",
                                message.Sender,
                                //we need to manually update the ID counter
                                World.UnitIDCounter++,
                                10, 10
                            );
                            BroadcastUnit(u);

                            World.GetPlayer(message.Sender).Money = 10;

                            //might make this a generic "update"
                            //so we don't have to manually resync this every
                            //time we change stuff for a player
                            SendAll(
                                new NetMessage3(
                                    NM3MessageType.player_status,
                                    message.Sender,
                                    World.GetPlayer(message.Sender).Money
                                )
                            );
                            break;
                    }
                    break;

                case NM3MessageType.unit_attack:
                    //please don't do shit until we've resolved combat
                    TcpPlayers[message.Sender]
                        .SendMessage(
                            new NetMessage3(NM3MessageType.client_unready)
                        );

                    Unit attacker = World.UnitLookup
                        [message.Get<int>("attackerid")];

                    Unit target = World.UnitLookup
                        [message.Get<int>("targetid")];

                    int totalStrength = attacker.Strength + target.Strength;
                    int roll = random.Next(totalStrength) + 1;

                    Unit loser;
                    if (roll <= attacker.Strength)
                        loser = attacker;
                    else
                        loser = target;

                    loser.Strength -= 1;
                    if (loser.Strength <= 0)
                        loser.Despawn();

                    SendAll(
                        new NetMessage3(
                            NM3MessageType.unit_status,
                            loser.ID,
                            loser.Moves,
                            loser.Attacks,
                            loser.Strength
                        )
                    );

                    //we done here
                    TcpPlayers[message.Sender]
                        .SendMessage(
                            new NetMessage3(NM3MessageType.client_ready)
                        );
                    break;

                case NM3MessageType.unit_build:
                    Unit unit = new Unit(
                        World,
                        UnitType.GetType(message.Get<string>("type")),
                        message.Sender,
                        World.UnitIDCounter++,
                        message.Get<int>("x"),
                        message.Get<int>("y")
                    );

                    World.SpawnUnit(unit);

                    BroadcastUnit(unit);

                    int newMoney = World
                        .GetPlayer(message.Sender)
                        .Money - unit.UnitType.Cost;

                    SendAll(
                        new NetMessage3(
                            NM3MessageType.player_status,
                            message.Sender,
                            newMoney
                        )
                    );
                    break;

                case NM3MessageType.station_create:
                    World.SpawnStation(
                        message.Get<int>("owner"),
                        message.Get<int>("x"),
                        message.Get<int>("y")
                    );
                    SendAll(message);
                    break;

                case NM3MessageType.client_disconnect:
                    TcpPlayers[message.Sender].Disconnected = true;
                    World.RemovePlayer(message.Sender);

                    TcpPlayers.Remove(message.Sender);

                    SendAll(
                        message,
                        except: message.Sender
                    );

                    break;

                default:
                    throw new ArgumentException();
            }
        }

        private void Pass()
        {
            World.Pass();
            SendAll(
                new NetMessage3(NM3MessageType.client_pass)
            );
        }

        private void ReceiveMessage(Client source, string message)
        {
            NetMessage3 nm3 = new NetMessage3(message);
            nm3.Sender = source.ID;
            HandleMessage(nm3);
        }

        public void Start()
        {
            TcpPlayers = new Dictionary<int, Client>();
            random = new Random();

            UnitType scout = new UnitType();
            scout.Moves = 2;
            scout.Strength = 3;
            scout.Attacks = 1;
            scout.Cost = 10;
            UnitType.RegisterType("scout", scout);

            UnitType worker = new UnitType();
            worker.Moves = 1;
            worker.Strength = 1;
            worker.Cost = 5;
            worker.Abilities.Add(UnitAbilites.Mining);
            UnitType.RegisterType("worker", worker);

            World = new fbWorld(80, 45);
            World.SpawnStation(0, 10, 12);
            World.SpawnPlanet(14, 14);

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
            p.SendMessage(
                new NetMessage3(
                    NM3MessageType.message,
                    "msg:Welcome to Farbase."
                )
            );

            p.SendMessage(
                new NetMessage3(
                    NM3MessageType.message,
                    "msg:Your ID is " + p.ID + "."
                )
            );

            p.SendMessage(
                new NetMessage3(
                    NM3MessageType.player_assign_id,
                    p.ID
                )
            );

            p.SendMessage(
                new NetMessage3(
                    NM3MessageType.world_create,
                    World.Map.Width,
                    World.Map.Height
                )
            );

            //tell new client about all existing players
            foreach (int id in World.Players.Keys)
            {
                Player existingPlayer = World.Players[id];

                p.SendMessage(
                    new NetMessage3(
                        NM3MessageType.player_new,
                        existingPlayer.ID
                    )
                );

                p.SendMessage(
                    new NetMessage3(
                        NM3MessageType.player_name,
                        existingPlayer.ID,
                        existingPlayer.Name,
                        ExtensionMethods.ColorToString(
                            existingPlayer.Color
                        )
                    )
                );

                p.SendMessage(
                    new NetMessage3(
                        NM3MessageType.player_status,
                        existingPlayer.ID,
                        existingPlayer.Money
                    )
                );
            }

            for (int x = 0; x < World.Map.Width; x++)
            for (int y = 0; y < World.Map.Height; y++)
            {
                Tile t = World.Map.At(x, y);
                if (t.Station != null)
                {
                    p.SendMessage(
                        new NetMessage3(
                            NM3MessageType.station_create,
                            t.Station.Owner,
                            x,
                            y
                        )
                    );
                }

                if (World.Map.At(x, y).Planet != null)
                {
                    p.SendMessage(
                        new NetMessage3(
                            NM3MessageType.planet_create,
                            x,
                            y
                        )
                    );
                }

                Unit u = World.Map.At(x, y).Unit;

                if (u != null)
                {
                    p.SendMessage(
                        new NetMessage3(
                            NM3MessageType.unit_create,
                            u.UnitType.Name,
                            u.Owner,
                            u.ID,
                            u.Position.X,
                            u.Position.Y
                        )
                    );

                    p.SendMessage(
                        new NetMessage3(
                            NM3MessageType.unit_status,
                            u.ID,
                            u.Moves,
                            u.Attacks,
                            u.Strength
                        )
                    );
                }
            }

            //no current player -> new player is current player
            if (World.CurrentID == -1)
                World.CurrentID = p.ID;

            //tell new client about whose turn it is
            p.SendMessage(
                new NetMessage3(
                    NM3MessageType.player_current,
                    World.CurrentID
                )
            );

            //we gucci now
            p.SendMessage(
                new NetMessage3(NM3MessageType.client_ready)
            );
        }

        private void handleClient(object clientObject)
        {
            Client p;

            lock (TcpPlayers)
            {
                p = new Client((TcpClient)clientObject);
                TcpPlayers.Add(p.ID, p);

                World.AddPlayer(
                    new Player(
                        "Unnamed player",
                        p.ID,
                        Color.CornflowerBlue
                    )
                );
            }

            //set up world
            welcomeClient(p);

            foreach (int id in TcpPlayers.Keys)
            {
                if (id == p.ID) continue;
                TcpPlayers[id].SendMessage(
                    new NetMessage3(
                        NM3MessageType.player_new,
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

            lock (TcpPlayers)
            {
                TcpPlayers.Remove(p.ID);
            }
        }
    }
}
