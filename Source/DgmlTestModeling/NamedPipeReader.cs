using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Pipes;
using System.IO;
using System.Threading;

namespace Microsoft.VisualStudio.DgmlTestModeling
{
    /// <summary>
    /// This event args is used to communicate messages read from the pipe.
    /// </summary>
    public class PipeMessageEventArgs : EventArgs
    {
        /// <summary>
        /// Construct a new PipeMessageEventArgs with the given message.
        /// This is used by the MessageArrived event on the NamedPipeReader
        /// </summary>
        /// <param name="message">The message that was read by the NamedPipeReader</param>
        public PipeMessageEventArgs(string message)
        {
            Message = message;
        }

        /// <summary>
        /// The message that was read by the NamedPipeReader
        /// </summary>
        public string Message { get; set; }
    }

    /// <summary>
    /// This class reads information from a named pipe that tells us where we 
    /// are in the model based test execution.  This class is used by the DGML Test Monitor
    /// VSIX plugin, but you could use it if you want to build your own user interface.
    /// </summary>
    public class NamedPipeReader : IDisposable
    {
        bool closed;
        NamedPipeServerStream pipe;
        const int MaxMessageBytes = 1024;
        bool paused;
        ManualResetEvent resumeEvent = new ManualResetEvent(false);

        /// <summary>
        /// Construct a new NamedPipeReader for reading messages from the pipe.
        /// Use the MessageArrived event to get the messages.  
        /// </summary>
        public NamedPipeReader()
        {            
            Task.Factory.StartNew(ReadPipe);
        }

        /// <summary>
        /// This event is raised (on a background thread) whenever a message has been
        /// received 
        /// </summary>
        public event EventHandler<PipeMessageEventArgs> MessageArrived;

        /// <summary>
        /// Close the pipe.
        /// </summary>
        public void Close()
        {
            closed = true;
            paused = false;
            resumeEvent.Set();
        }

        /// <summary>
        /// Pause execution of the model.
        /// </summary>
        public void Pause()
        {
            paused = true;
            resumeEvent.Reset();
        }

        /// <summary>
        /// Resume execution of the model.
        /// </summary>
        public void Resume()
        {
            paused = false;
            resumeEvent.Set();
        }

        /// <summary>
        /// Get whether the execution is currently paused.
        /// </summary>
        public bool IsPaused { get { return paused; } }

        /// <summary>
        /// This event is raised whenever the model is paused.
        /// This event is raised from a background thread.
        /// </summary>
        public event EventHandler Break;

        private void OnBreak()
        {
            if (Break != null)
            {
                Break(this, EventArgs.Empty);
            }
        }

        private void ReadPipe()
        {
            while (!closed)
            {
                try
                {
                    if (pipe == null)
                    {
                        pipe = new NamedPipeServerStream("63642A12-F751-41E3-A9D3-279EE34A0EDB-DgmlTestMonitor",
                                    PipeDirection.InOut, 1, PipeTransmissionMode.Message,
                                    PipeOptions.Asynchronous, MaxMessageBytes, MaxMessageBytes);
                    }

                    if (!pipe.IsConnected)
                    {
                        pipe.WaitForConnection();
                    }

                    string msg = ReadMessage();
                    if (!string.IsNullOrEmpty(msg))
                    {
                        OnMessageArrived(msg);
                        if (paused)
                        {
                            OnBreak();
                            // wait forever...
                            resumeEvent.WaitOne();
                            if (closed)
                            {
                                return;
                            }
                        }
                        WriteMessage("ok");
                    }
                    else
                    {
                        Thread.Sleep(100);
                    }                    
                }
                catch (Exception e)
                {
                    IOException io = e as IOException;
                    if (io != null)
                    {
                        uint hr = (uint)io.HResult;
                        if (hr == 0x800700e7)
                        {
                            // multiple VS instances, all pipe instances are busy, so go to deep sleep
                            // waiting for other VS instances to go away.
                            Thread.Sleep(1000);
                        }
                        else if (hr == 0x80131620)
                        {
                            // Pipe is broken, need to recreate it.
                            using (pipe)
                            {
                                pipe = null;
                            }
                        }
                    }
                    else
                    {
                        // on standby, waiting for test to fire up...
                        Thread.Sleep(100);
                    }
                }
            }
        }

        private void OnMessageArrived(string msg)
        {
            if (MessageArrived != null)
            {
                MessageArrived(this, new PipeMessageEventArgs(msg));
            }
        }

        /// <summary>
        /// Read a string from the pipe. We assume all strings are unicode.
        /// Only MaxBytes characters will be read, meaning the max string length
        /// is MaxBytes / BytesPerChar.
        /// </summary>
        private string ReadMessage()
        {
            byte[] buffer = new byte[MaxMessageBytes];
            int numBytesRead = pipe.Read(buffer, 0, MaxMessageBytes);

            // Each unicode character takes BytesPerChar. We require at least one character or we return null.
            int count = Encoding.Unicode.GetMaxCharCount(numBytesRead);
            if (count == 0)
                return null;

            // Trim any null terminator from the end of the string
            return Encoding.Unicode.GetString(buffer).Trim('\0');
        }



        /// <summary>
        /// Send a string down the pipe using Unicode encoding.
        /// Only the first MaxBytes will be sent, meaning the max
        /// string length is MaxBytes / BytesPerChar.
        /// </summary>
        private bool WriteMessage(string message)
        {
            try
            {
                if (pipe != null && pipe.IsConnected)
                {
                    // Don't use a StreamWriter here because it has buffering which messes up the synchronization
                    // of messages across the pipe.
                    byte[] bytes = Encoding.Unicode.GetBytes(message);
                    pipe.Write(bytes, 0, Math.Min(bytes.Length, MaxMessageBytes));
                }
            }
            catch (IOException)
            {
                return false;
            }
            return true;
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
        ~NamedPipeReader()
        {
            Dispose(false);
        }

        /// <summary>
        /// Called when the reader is being disposed
        /// </summary>
        /// <param name="disposing">Whether Dispose was called</param>
        protected virtual void Dispose(bool disposing)
        {
            Close();
            using (pipe)
            {
                pipe = null;
            }
            using (resumeEvent) 
            {
            }
        }
    }
}
