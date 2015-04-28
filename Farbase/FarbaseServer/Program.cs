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

        public void SendMessage(fbNetMessage message)
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
            fbNetMessage message,
            int except = -1
        ) {
            foreach (Client p in players)
                if (p.ID != except)
                    p.SendMessage(message.Format());
        }

        private void BroadcastUnit(Unit u)
        {
            SendAll(
                new CreateUnitMessage(
                    u.UnitType.Name,
                    u.Owner,
                    u.ID,
                    (int)u.Position.X,
                    (int)u.Position.Y
                )
            );
        }

        private void HandleMessage(fbNetMessage message)
        {
            switch (message.GetMessageType())
            {
                case MsgMessage.Command:
                    HandleMessage((MsgMessage)message);
                    break;

                case NameMessage.Command:
                    HandleMessage((NameMessage)message);
                    break;

                case PassMessage.Command:
                    HandleMessage((PassMessage)message);
                    break;

                case MoveUnitMessage.Command:
                    HandleMessage((MoveUnitMessage)message);
                    break;

                case DevCommandMessage.Command:
                    HandleMessage((DevCommandMessage)message);
                    break;

                case AttackMessage.Command:
                    HandleMessage((AttackMessage)message);
                    break;

                case BuildUnitMessage.Command:
                    HandleMessage((BuildUnitMessage)message);
                    break;

                default:
                    //should probably be handled more gracefully in the future,
                    //but works for unknown messages for now.
                    throw new ArgumentException();
            }
        }

        private void HandleMessage(MsgMessage message)
        {
            Console.WriteLine(message.Content);
        }

        private void HandleMessage(NameMessage message)
        {
            fbGame.World.Players[message.id].Name = message.name;
            fbGame.World.Players[message.id].Color = message.color;

            SendAll(message);
        }

        private void HandleMessage(PassMessage message)
        {
            fbGame.World.CurrentPlayerIndex =
                (fbGame.World.CurrentPlayerIndex + 1) %
                fbGame.World.PlayerIDs.Count;

            SendAll(
                new CurrentPlayerMessage(fbGame.World.CurrentPlayerIndex)
            );

            fbGame.World.PassTo(fbGame.World.CurrentID);

            SendAll(
                new ReplenishPlayerMessage(fbGame.World.CurrentID)
            );
        }

        private void HandleMessage(MoveUnitMessage message)
        {
            Unit un;
            //lock necessary?
            lock (fbGame.World)
            {
                un = fbGame.World.UnitLookup[message.id];

                if (un.Moves > 0)
                {
                    un.MoveTo(message.x, message.y);
                    un.Moves--;
                }
            }

            //pass along to everyone else
            SendAll(
                message,
                un.Owner
            );

            SendAll(
                new SetUnitMovesMessage(un.ID, un.Moves),
                un.Owner
            );
        }

        private void HandleMessage(DevCommandMessage message)
        {
            switch (message.Number)
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

                    SendAll(new SetMoneyMessage(message.Sender, 10));
                    break;
            }
        }

        private void HandleMessage(AttackMessage message)
        {
            //please don't do shit until we've resolved combat
            Client.TcpPlayers[message.Sender]
                .SendMessage(new UnreadyMessage());

            Unit attacker = fbGame.World.UnitLookup[message.attackerid];
            Unit target = fbGame.World.UnitLookup[message.targetid];

            int totalStrength = attacker.Strength + target.Strength;
            int roll = random.Next(totalStrength) + 1;

            Unit loser;
            if (roll <= attacker.Strength)
                loser = attacker;
            else
                loser = target;

            loser.Hurt(1);
            SendAll(new HurtMessage(loser.ID, 1));

            //we done here
            Client.TcpPlayers[message.Sender]
                .SendMessage(new ReadyMessage());
        }

        private void HandleMessage(BuildUnitMessage message)
        {
            BroadcastUnit(
                fbGame.World.SpawnUnit(
                    message.type,
                    message.Sender,
                    fbGame.World.UnitIDCounter++,
                    message.x,
                    message.y
                )
            );

            int newMoney =
                fbGame.World.Players[message.Sender].Money -
                UnitType.GetType(message.type).Cost;

            SendAll(
                new SetMoneyMessage(
                    message.Sender,
                    newMoney
                )
            );
        }

        private void ReceiveMessage(Client source, string message)
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
            msg.Sender = source.ID;

            HandleMessage(msg);
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
