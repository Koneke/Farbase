using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using Microsoft.Xna.Framework;

namespace Farbase
{
    public class fbNetClient
    {
        private fbApplication app;

        public static bool Verbose = true;
        public static fbGame Game;

        private TcpClient client;
        private NetworkStream stream;
        private ASCIIEncoding encoder;

        public bool ShouldDie;
        private List<string> SendQueue;
        public bool Ready;

        public fbNetClient(fbApplication app)
        {
            this.app = app;
            Ready = false;
        }

        private void ReceiveMessage(string message)
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

            if(Verbose)
                Game.Log.Add(
                    String.Format("<- s: {0}", message)
                );

            command = command.ToLower();

            fbNetMessage netmsg = fbNetMessage.Spawn(command, arguments);
            HandleMessage(netmsg);
        }

        private void HandleMessage(fbNetMessage message)
        {
            switch (message.GetMessageType())
            {
                case MsgMessage.Command:
                    HandleMessage((MsgMessage)message);
                    break;

                case CreateWorldMessage.Command:
                    HandleMessage((CreateWorldMessage)message);
                    break;

                case CreateStationMessage.Command:
                    HandleMessage((CreateStationMessage)message);
                    break;

                case CreatePlanetMessage.Command:
                    HandleMessage((CreatePlanetMessage)message);
                    break;

                case CreateUnitMessage.Command:
                    HandleMessage((CreateUnitMessage)message);
                    break;

                case MoveUnitMessage.Command:
                    HandleMessage((MoveUnitMessage)message);
                    break;

                case SetUnitMovesMessage.Command:
                    HandleMessage((SetUnitMovesMessage)message);
                    break;

                case NewPlayerMessage.Command:
                    HandleMessage((NewPlayerMessage)message);
                    break;

                case ReplenishPlayerMessage.Command:
                    HandleMessage((ReplenishPlayerMessage)message);
                    break;

                case AssignIDMessage.Command:
                    HandleMessage((AssignIDMessage)message);
                    break;

                case NameMessage.Command:
                    HandleMessage((NameMessage)message);
                    break;

                case CurrentPlayerMessage.Command:
                    HandleMessage((CurrentPlayerMessage)message);
                    break;

                case ReadyMessage.Command:
                    HandleMessage((ReadyMessage)message);
                    break;

                case UnreadyMessage.Command:
                    HandleMessage((UnreadyMessage)message);
                    break;

                case HurtMessage.Command:
                    HandleMessage((HurtMessage)message);
                    break;

                default:
                    //should probably be handled more gracefully in the future,
                    //but works for unknown messages for now.
                    throw new ArgumentException();
            }
        }

        private void HandleMessage(MsgMessage message)
        {
            app.Game.Log.Add(message.Content);
        }

        private void HandleMessage(CreateWorldMessage message)
        {
            fbGame.World = new fbWorld(message.w, message.h);
        }

        private void HandleMessage(CreateStationMessage message)
        {
            fbGame.World.SpawnStation(message.x, message.y);
        }

        private void HandleMessage(CreatePlanetMessage message)
        {
            fbGame.World.SpawnPlanet(message.x, message.y);
        }

        private void HandleMessage(CreateUnitMessage message)
        {
            fbGame.World.SpawnUnit(
                message.type,
                message.owner,
                message.id,
                message.x,
                message.y
            );
        }

        private void HandleMessage(MoveUnitMessage message)
        {
            fbGame.World.UnitLookup[message.id].MoveTo(message.x, message.y);
        }

        private void HandleMessage(SetUnitMovesMessage message)
        {
            fbGame.World.UnitLookup[message.id].Moves = message.amount;
        }

        private void HandleMessage(NewPlayerMessage message)
        {
            fbGame.World.AddPlayer(
                new Player(
                    "Unnnamed player",
                    message.id,
                    Color.White
                )
            );
        }

        private void HandleMessage(ReplenishPlayerMessage message)
        {
            fbGame.World.ReplenishPlayer(message.id);
        }

        private void HandleMessage(AssignIDMessage message)
        {
            app.Game.We = message.id;
        }

        private void HandleMessage(NameMessage message)
        {
            Player p = fbGame.World.Players[message.id];

            app.Game.Log.Add(
                string.Format(
                    "{0}<{2}> is now known as {1}<{2}>.",
                    p.Name,
                    message.name,
                    message.id
                )
            );

            p.Name = message.name;
            p.Color = message.color;
        }

        private void HandleMessage(CurrentPlayerMessage message)
        {
            fbGame.World.CurrentPlayerIndex = message.index;
            app.Game.Log.Add(
                string.Format(
                    "It is now {0}'s turn.",
                    fbGame.World.CurrentPlayer.Name
                )
            );
        }

        private void HandleMessage(ReadyMessage message)
        {
            Ready = true;
        }

        private void HandleMessage(UnreadyMessage message)
        {
            Ready = false;
        }

        private void HandleMessage(HurtMessage message)
        {
            fbGame.World.UnitLookup[message.id].Hurt(message.amount);
        }

        public void Start()
        {
            ShouldDie = false;
            SendQueue = new List<string>();
            ConnectTo("127.0.0.1", 7707);

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
                        ReceiveMessage(msg);
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

        public void Send(fbNetMessage message)
        {
            SendQueue.Add(message.Format());
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

            if(Verbose)
                Game.Log.Add(
                    String.Format("-> s: {0}", message)
                );
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
