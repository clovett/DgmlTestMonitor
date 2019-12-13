using Microsoft.Coyote.SmartSockets;
using Microsoft.VisualStudio.GraphModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LovettSoftware.DgmlTestModeling
{
    /// <summary>
    /// This class reads the state passed by GraphStateWriter via named pipe.
    /// </summary>
    public class GraphStateReader : IDisposable
    {
        SmartSocketServer server;

        /// <summary>
        /// This event is raised when a message is received.
        /// We are expecting LoadGraphMessage, NavigateLinkMessage, NavigateNodeMessage and ClearTextMessages
        /// </summary>
        public event EventHandler<Message> MessageReceived;


        /// <summary>
        /// Construct graph state reader.
        /// </summary>
        public GraphStateReader()
        {
        }

        /// <summary>
        /// Start listening for the various graph events
        /// </summary>
        /// <returns>The async task</returns>
        public void Start()
        {
            try
            {
                var resolver = new SmartSocketTypeResolver(typeof(Message),
                                                           typeof(ConnectedMessage),
                                                           typeof(ClearTextMessage),
                                                           typeof(LoadGraphMessage),
                                                           typeof(CreateNodeMessage),
                                                           typeof(CreateLinkMessage),
                                                           typeof(NavigateNodeMessage),
                                                           typeof(NavigateLinkMessage));
                this.server = SmartSocketServer.StartServer("DgmlTestMonitor", resolver, "127.0.0.1:0");
                server.ClientConnected += OnClientAdded;
                server.ClientDisconnected += OnClientRemoved;
            }
            catch (Exception ex)
            {
                OnMessageReceived(new ClearTextMessage() { Message = ex.Message });
            }
        }
        
        private void OnClientRemoved(object sender, SmartSocketClient e)
        {
            clients.Remove(e);
        }

        List<SmartSocketClient> clients = new List<SmartSocketClient>();

        private void OnClientAdded(object sender, SmartSocketClient e)
        {
            clients.Add(e);

            e.Error += this.OnClientError;

            Task.Run(() => this.HandleClientAsync(e));
        }

        private async void HandleClientAsync(SmartSocketClient client)
        {
            while (client.IsConnected)
            {
                Message e = await client.ReceiveAsync() as Message;
                if (e != null)
                {
                    await client.SendAsync(new SocketMessage("Ok", "DgmlTestMonitor")); // ack
                    OnMessageReceived(e);
                }
            }
        }

        private void OnMessageReceived(Message m)
        {
            if (MessageReceived != null)
            {
                MessageReceived(this, m);
            }
        }

        private void OnClientError(object sender, Exception e)
        {
            // todo: show the error!
        }

        /// <summary>
        /// Pause the pipe without hitting a breakpoint.
        /// </summary>
        public void Pause()
        {
            //server.Pause();
        }

        /// <summary>
        /// Find out if pipe is paused which could be a resuilt of calling Pause or from hitting a breakpoint.
        /// </summary>
        public bool IsPaused
        {
            get { return false; } // server.IsPaused; }
        }

        /// <summary>
        /// Resume execution after hitting a breakpoint.
        /// </summary>
        public void Resume()
        {
            // server.Resume();
        }
        
        /// <summary>
        /// Dispose the reader
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Destructor for the NamedPipeReader
        /// </summary>
        ~GraphStateReader()
        {
            Dispose(false);
        }

        /// <summary>
        /// Called when the reader is being disposed
        /// </summary>
        /// <param name="disposing">Whether Dispose was called</param>
        protected virtual void Dispose(bool disposing)
        {
            if (server != null)
            {
                server.Stop();
            }
            server = null;
        }

    }
}
