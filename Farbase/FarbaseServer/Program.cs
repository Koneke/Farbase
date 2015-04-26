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

        private Farbase.fbWorld World;

        private void SendAll(string message)
        {
            foreach (Player p in players)
                p.SendMessage(message);
        }

        private void HandleMessage(Player source, string message)
        {
            string command, args;

            int split = message.IndexOf(':');
            command = message.Substring(0, split);
            args = message.Substring(split + 1, message.Length - (split + 1));

            Console.WriteLine("<- {0}: {1}", source.ID, message);

            switch (command)
            {
                case "msg":
                    Console.WriteLine(args);
                    break;

                case "login":
                    World.Players[source.ID].Name = args;
                    SendAll(
                        string.Format(
                            "name:{0},{1}",
                            source.ID,
                            args
                        )
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

            World = new fbWorld(80, 45);
            World.SpawnStation(10, 12);
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
            encoder = new ASCIIEncoding();

            try
            {
                listener = new TcpListener(IPAddress.Any, 7777);
                listenThread = new Thread(listen);
                listenThread.Start();

                Console.WriteLine("Running at 7777.");
                Console.WriteLine("Local EP is: " + listener.LocalEndpoint);
                Console.WriteLine("Waiting...");

                Console.ReadKey();
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
            p.SendMessage("msg:Hello, client!");
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
                    World.Map.Width,
                    World.Map.Height
                )
            );

            for (int x = 0; x < World.Map.Width; x++)
            for (int y = 0; y < World.Map.Height; y++)
            {
                if (World.Map.At(x, y).Station != null)
                    p.SendMessage(
                        string.Format(
                            "create-station:{0},{1}",
                            x, y
                        )
                    );
                if (World.Map.At(x, y).Planet != null)
                    p.SendMessage(
                        string.Format(
                            "create-planet:{0},{1}",
                            x, y
                        )
                    );
            }

            foreach (int id in World.PlayerIDs)
            {
                //don't message us about ourselves (again)
                if (p.ID == id) continue;

                p.SendMessage(
                    string.Format(
                        "new-player:{0}",
                        id
                    )
                );

                p.SendMessage(
                    string.Format(
                        "name:{0},{1}",
                        id,
                        World.Players[id].Name
                    )
                );
            }
        }

        private void handleClient(object clientObject)
        {
            Player p;

            lock (players)
            {
                p = new Player((TcpClient)clientObject);
                players.Add(p);
                Player.TcpPlayers.Add(p.ID, p);

                World.AddPlayer(
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
