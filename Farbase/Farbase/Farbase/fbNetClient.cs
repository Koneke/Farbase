using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using Microsoft.Xna.Framework.GamerServices;

namespace Farbase
{
    public class fbNetClient
    {
        public static fbGame Game;

        private TcpClient client;
        private NetworkStream stream;
        private ASCIIEncoding encoder;

        public bool ShouldDie;
        public List<string> SendQueue;

        private void HandleMessage(string message)
        {
            string command, args;

            int split = message.IndexOf(':');
            command = message.Substring(0, split);
            args = message.Substring(split + 1, message.Length - (split + 1));

            Game.Log.Add(
                String.Format("<- s: {0}", message)
            );

            switch (command)
            {
                case "msg":
                    Game.Log.Add(args);
                    break;
                case "create-world":
                    int w, h;
                    Int32.TryParse(args.Split(',')[0], out w);
                    Int32.TryParse(args.Split(',')[1], out h);
                    Game.CreateMap(w, h);
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
                        Send(SendQueue[0]);
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
            byte[] buffer = encoder.GetBytes(message);
            stream.Write(buffer, 0, buffer.Length);
            stream.Flush();
        }

        public void Close()
        {
            if (client.Connected)
            {
                Send("msg:Client disconnecting.");
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
