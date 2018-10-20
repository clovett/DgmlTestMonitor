using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.System.Threading;

namespace Microsoft.VisualStudio.DgmlTestModeling
{

    public class ConnectionLostEventArgs : EventArgs
    {
        public Exception ReceiveError { get; set; }

        public bool AutoReconnect { get; set; }
    }

    /// <summary>
    /// This class connects to a given server/port and setups the ability to exchange events and html assets for
    /// rendering any custom application level UI sent from that server.
    /// </summary>
    public class SmartSocketClient : IDisposable
    {
        StreamSocket socket;
        BinaryWriter writer;
        BinaryReader reader;
        int _port;
        string _serverName;
        bool _closed;

        public SmartSocketClient()
        {
        }

        public SmartSocketClient(StreamSocket socket)
        {
            SetSocket(socket);
        }

        public event EventHandler<Exception> Error;
        public event EventHandler<ConnectionLostEventArgs> ConnectionLost;
        public event EventHandler Connected;
        public event EventHandler<Message> MessageReceived;

        public void Dispose()
        {
            Close();
        }

        internal void Close()
        {
            using (socket)
            {
                socket = null;
            }
            using (reader)
            {
                reader = null;
            }
            using (writer)
            {
                writer = null;
            }
            _closed = true;
        }

        private void OnError(Exception ex)
        {
            if (Error != null)
            {
                Error(this, ex);
            }
        }


        public void ConnectAsync(string server, int port)
        {
            Close();

            if (server == "localhost")
            {
                NetworkAdapter adapter;
                server = SmartSocketListener.GetLocalAddress(out adapter);
                if (server == null)
                {
                    throw new Exception("Could not find your local ethernet");
                }
            }

            this._closed = false;
            _serverName = server;
            _port = port;

            StartConnectThread();
        }

        private void ReceiveThread()
        {
            while (socket != null)
            {
                try
                {
                    Message message = Message.Create(reader);
                    OnMessageReceived(message);
                }
                catch (Exception ex)
                {
                    // connection lost?                    
                    OnConnectionLost(ex);
                    break;
                }
            }
        }

        private void OnMessageReceived(Message message)
        {
            if (MessageReceived != null)
            {
                MessageReceived(this, message);
            }
        }

        private void OnConnectionLost(Exception ex)
        {
            if (ConnectionLost != null)
            {
                ConnectionLostEventArgs args = new ConnectionLostEventArgs() { ReceiveError = ex };
                ConnectionLost(this, args);
                if (args.AutoReconnect)
                {
                    StartConnectThread();
                }
            }
        }

        private void StartConnectThread()
        {
            Task.Run(new Action(() => { RetryConnectThread(); }));
        }

        private async void RetryConnectThread()
        {
            while (!_closed)
            {
                // funny thing about sockets is that if you do a ConnectAsync before other end starts listening
                // it hangs until connect timeout which is usally about 1 minute, even if the other end starts
                // listening soon after.  So this loop ensures we retry more frequently than once a minute.

                Task<bool> task = TryConnect();

                // it should not take more than 5 seconds to connect.
                bool rc = Task.WaitAll(new Task[] { task }, 5000);

                if (rc)
                {
                    if (task.Result)
                    {
                        if (Connected != null)
                        {
                            Connected(this, EventArgs.Empty);
                        }
                        break;
                    }
                }
                else if (socket != null)
                {
                    socket.Dispose();
                    socket = null;
                }

                // delay between retries.
                await Task.Delay(5000);
            }
        }

        internal void SetSocket(StreamSocket socket)
        {
            this.socket = socket;

            this.reader = new BinaryReader(socket.InputStream.AsStreamForRead());
            this.writer = new BinaryWriter(socket.OutputStream.AsStreamForWrite());

            var nowait = Task.Run(new Action(ReceiveThread));
        }

        private async Task<bool> TryConnect()
        {
            var startSocket = new StreamSocket();
            try
            {
                socket = startSocket;

                await socket.ConnectAsync(new HostName(_serverName), _port.ToString());

                OnMessageReceived(new ConnectedMessage());

                SetSocket(startSocket);

                return true;
            }
            catch (Exception ex)
            {
                if (this.socket == startSocket)
                {
                    OnError(new Exception("Error connecting to server at " + _serverName + ": " + ex.Message));
                }
            }
            return false;
        }

        public void SendAsync(Message message)
        {

            if (socket == null)
            {
                throw new Exception("Connection is closed");
            }
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            message.Write(this.writer);
            this.writer.Flush();
        }


    }
}
