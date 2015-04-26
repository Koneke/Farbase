using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace FarbaseServer
{
    class Player
    {
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

        private TcpListener listener;
        private Thread listenThread;
        public static ASCIIEncoding encoder;

        private bool die;

        private List<Player> players; 

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
                default:
                    Console.WriteLine(
                        "Received {0} command from id {1}," +
                        " but no idea what to do with it.",
                        command, source.ID
                    );
                    break;
            }
        }

        private void handleClient(object clientObject)
        {
            Player p;

            lock (players)
            {
                p = new Player((TcpClient)clientObject);
                players.Add(p);
            }

            p.SendMessage("msg:Hello, client!");
            p.SendMessage("msg:Your ID is " + p.ID + ".");

            p.SendMessage("create-world:80,45");

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
                    //Console.Write(encoder.GetString(message, 0, bytesRead));

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

        public void Start()
        {
            encoder = new ASCIIEncoding();
            players = new List<Player>();

            try
            {
                listener = new TcpListener(IPAddress.Any, 7777);
                listenThread = new Thread(listen);
                listenThread.Start();

                Console.WriteLine("Running at 7777.");
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
    }
}
