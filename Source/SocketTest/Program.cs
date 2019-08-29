using LovettSoftware.DgmlTestModeling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketTest
{
    class Program
    {
        int DefaultPort = 25777;

        static void Main(string[] args)
        {
            Program p = new Program();

            if (args.Length > 0 && args[0] == "/client")
            {
                p.RunClient().Wait();
            }
            else
            {
                p.RunServer().Wait(); 
            }

        }

        async Task RunClient()
        {
            SmartSocketClient client = new SmartSocketClient();
            client.Connected += OnClientConnected;
            client.ConnectAsync("localhost", DefaultPort);
            await Task.Delay(60000);
        }

        private void OnClientConnected(object sender, EventArgs e)
        {
            SmartSocketClient client = (SmartSocketClient)sender;
            client.MessageReceived += OnClientMessageReceived;
            Task.Run(() => PingServer(client));
        }

        async void PingServer(SmartSocketClient client)
        {
            int ping = 1;
            while (true)
            {
                client.SendAsync(new ClearTextMessage("ping " + ping++));
                await Task.Delay(1000);
            }
        }

        private void OnClientMessageReceived(object sender, Message e)
        {
            ClearTextMessage c = (ClearTextMessage)e;
            Console.WriteLine("Client received: " + c.Message);
        }

        async Task RunServer()
        { 
            SmartSocketListener listener = new SmartSocketListener();
            listener.ClientConnected += OnClientReceived;
            listener.ClientDisconnected += OnClientLost;
            await listener.StartListening(DefaultPort);
            await Task.Delay(60000);
        }

        private void OnClientLost(object sender, SmartSocketClient e)
        {
            Console.WriteLine("Server lost client");
        }

        private void OnClientReceived(object sender, SmartSocketClient e)
        {
            Console.WriteLine("Server found client");
            e.MessageReceived += OnServerMessageReceived;
            e.ConnectionLost += OnServerConnectionLost;
        }

        private void OnServerConnectionLost(object sender, ConnectionLostEventArgs e)
        {
            Console.WriteLine("Server connection lost");
        }

        private void OnServerMessageReceived(object sender, Message e)
        {
            ClearTextMessage c = (ClearTextMessage)e;
            Console.WriteLine("Server received: " + c.Message);
            SmartSocketClient client = (SmartSocketClient)sender;
            client.SendAsync(new ClearTextMessage("Message received"));
        }
    }
}
