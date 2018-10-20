using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Networking.Connectivity;
using Windows.Storage.Streams;
using System.IO;
using Windows.Networking;

namespace Microsoft.VisualStudio.DgmlTestModeling
{

    /// <summary>
    /// This class provides an auto-reconnecting stream socket server for simple bidirection
    /// message passing.  
    /// </summary>
    public sealed class SmartSocketListener
    {
        int serverPort;
        bool connected;
        NetworkAdapter _adapter;
        string _localAddress;
        StreamSocketListener listener;
        List<SmartSocketClient> clients = new List<SmartSocketClient>();
        bool closed;

        public bool IsPaused { get; internal set; }

        public SmartSocketListener()
        {
            NetworkInformation.NetworkStatusChanged += OnNetworkStatusChanged;
        }

        public void Close()
        {
            closed = true;
            foreach (SmartSocketClient client in clients)
            {
                client.Close();
            }
            clients.Clear();
        }

        async void OnNetworkStatusChanged(object sender)
        {
            if (!connected)
            {
                await CheckNetworkProfiles();
            }
        }

        internal static string GetLocalAddress(out NetworkAdapter adapter)
        {
            var inetProfile = NetworkInformation.GetInternetConnectionProfile();
            adapter = null;
            if (inetProfile != null)
            {
                foreach (var name in NetworkInformation.GetHostNames())
                {
                    var ipinfo = name.IPInformation;
                    if (ipinfo != null && name.Type == Windows.Networking.HostNameType.Ipv4)
                    {
                        if (ipinfo.NetworkAdapter.NetworkAdapterId == inetProfile.NetworkAdapter.NetworkAdapterId)
                        {
                            adapter = ipinfo.NetworkAdapter;
                            return name.CanonicalName;
                        }

                    }
                }
            }
            return null;
        }

        async Task CheckNetworkProfiles()
        {
            _localAddress = GetLocalAddress(out _adapter);

            if (_localAddress != null)
            {
                if (AdapterFound != null)
                {
                    AdapterFound(this, _localAddress);
                }

                this.listener = new StreamSocketListener();
                this.listener.ConnectionReceived += OnConnectionReceived;
                await this.listener.BindEndpointAsync(new Windows.Networking.HostName(_localAddress), this.serverPort.ToString());
                //await this.listener.BindServiceNameAsync(this.serverPort.ToString());

                // also listen for UDP datagrams.
                var nowait = Task.Run(new Action(BroadcastServerThread));

                connected = true;
                return;
            }
        }

        private async void BroadcastServerThread()
        {
            var dgramSocket = new DatagramSocket();
            await dgramSocket.BindServiceNameAsync(this.serverPort.ToString(), _adapter);

            while (!closed)
            {
                // Send UDP broadcasts out to phone clients (for some reason phone clients can't send UDP on the MSFT network!)
                // So we have to ping constantly which sucks.

                using (var stream = await dgramSocket.GetOutputStreamAsync(new HostName("255.255.255.255"), this.serverPort.ToString()))
                {
                    using (var writer = new DataWriter(stream))
                    {
                        var data = Encoding.UTF8.GetBytes(_localAddress);
                        writer.WriteBytes(data);
                        await writer.StoreAsync();
                    }
                }

                // send out a ping every 1 second 
                await Task.Delay(1000);
            }
        }

        private void OnReceiveDatagram(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
        {
            var phoneClient = args.RemoteAddress;
            var phonePort = args.RemotePort;
        }

        public event EventHandler<SmartSocketClient> ClientConnected;
        public event EventHandler<SmartSocketClient> ClientDisconnected;
        public event EventHandler<string> AdapterFound;

        public async Task StartListening(int portNumber)
        {
            serverPort = portNumber;
            await CheckNetworkProfiles();
        }

        void OnConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            var socket = args.Socket;
            var client = new SmartSocketClient(socket);
            client.Error += OnClientError;
            this.clients.Add(client);
            OnClientConnected(client);
        }

        private void OnClientError(object sender, Exception e)
        {
            SmartSocketClient client = (SmartSocketClient)sender;
            this.clients.Remove(client);
            OnClientDisconnected(client);
        }

        private void OnClientDisconnected(SmartSocketClient client)
        {
            if (ClientDisconnected != null)
            {
                ClientDisconnected(this, client);
            }
        }

        internal void Resume()
        {         
        }

        internal void Pause()
        {
        }

        private void OnClientConnected(SmartSocketClient client)
        {
            if (ClientConnected != null)
            {
                ClientConnected(this, client);
            }
        }
    }
}
