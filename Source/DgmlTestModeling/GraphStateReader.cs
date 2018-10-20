using Microsoft.VisualStudio.GraphModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.DgmlTestModeling
{
    /// <summary>
    /// This class reads the state passed by GraphStateWriter via named pipe.
    /// </summary>
    public class GraphStateReader : IDisposable
    {
        SmartSocketListener server;

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
            server = new SmartSocketListener();
        }

        /// <summary>
        /// Start listening for the various graph events
        /// </summary>
        /// <returns>The async task</returns>
        public async Task Start()
        {
            try {
                await server.StartListening(GraphStateWriter.DefaultPort);
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
            e.MessageReceived += OnMessageReceived;
        }

        private void OnMessageReceived(object sender, Message msg)
        {
            if (MessageReceived != null)
            {
                MessageReceived(this, msg);
            }
        }

        private void OnMessageReceived(ClearTextMessage message)
        {
            if (MessageReceived != null)
            {
                MessageReceived(this, message);
            }
        }

        /// <summary>
        /// Pause the pipe without hitting a breakpoint.
        /// </summary>
        public void Pause()
        {
            server.Pause();
        }

        /// <summary>
        /// Find out if pipe is paused which could be a resuilt of calling Pause or from hitting a breakpoint.
        /// </summary>
        public bool IsPaused
        {
            get { return server.IsPaused; }
        }

        /// <summary>
        /// Resume execution after hitting a breakpoint.
        /// </summary>
        public void Resume()
        {
            server.Resume();
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
                server.Close();
                server = null;
            }
        }

    }
}
