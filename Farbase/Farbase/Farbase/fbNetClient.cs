using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Farbase
{
    public class fbNetClient
    {
        public static bool Verbose = true;
        public static fbGame Game;

        private TcpClient client;
        private NetworkStream stream;
        private ASCIIEncoding encoder;

        public bool ShouldDie;
        private List<string> SendQueue;
        public bool Ready;

        public fbNetClient()
        {
            Ready = false;
        }

        private void ReceiveMessage(string message)
        {
            if (Verbose)
                Game.Log.Add(
                    string.Format(
                        "<- s: {0}",
                        message
                    )
                );

            NetMessage3 nm3message = new NetMessage3(message);
            Game.HandleNetMessage(nm3message);
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

        public void Send(NetMessage3 message)
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
