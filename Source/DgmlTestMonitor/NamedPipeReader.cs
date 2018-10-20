using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Pipes;
using System.IO;
using System.Threading;

namespace VTeam.DgmlTestMonitor
{
    public class PipeMessageEventArgs : EventArgs
    {
        public PipeMessageEventArgs(string message)
        {
            Message = message;
        }

        public string Message { get; set; }
    }

    /// <summary>
    /// This class reads information from a named pipe that tells us where we 
    /// are in the model based test execution.
    /// </summary>
    public class NamedPipeReader : IDisposable
    {
        bool closed;
        NamedPipeServerStream pipe;
        TaskFactory uiThreadTaskFactory;
        const int MaxMessageBytes = 1024;
        bool paused;
        ManualResetEvent resumeEvent = new ManualResetEvent(false);

        public NamedPipeReader()
        {
            var scheduler = TaskScheduler.FromCurrentSynchronizationContext();
            uiThreadTaskFactory = new TaskFactory(scheduler);
            Task.Factory.StartNew(ReadPipe);
        }

        public void Close()
        {
            closed = true;
            paused = false;
            resumeEvent.Set();
        }

        public void Pause()
        {
            paused = true;
            resumeEvent.Reset();
        }

        public void Resume()
        {
            paused = false;
            resumeEvent.Set();
        }

        public bool IsPaused { get { return paused; } }

        public event EventHandler Break;

        private void OnBreak()
        {
            CallOnMainThread(new Action(() =>
            {
                if (Break != null)
                {
                    Break(this, EventArgs.Empty);
                }
            }));
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

        void CallOnMainThread(Action action)
        {
            uiThreadTaskFactory.StartNew(action);
        }

        public event EventHandler<PipeMessageEventArgs> MessageArrived;

        private void OnMessageArrived(string msg)
        {
            CallOnMainThread(new Action(() =>
            {
                if (MessageArrived != null)
                {
                    MessageArrived(this, new PipeMessageEventArgs(msg));
                }
            }));
        }

        /// <summary>
        /// Read a string from the pipe. We assume all strings are unicode.
        /// Only MaxBytes characters will be read, meaning the max string length
        /// is MaxBytes / BytesPerChar.
        /// </summary>
        public string ReadMessage()
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
        public bool WriteMessage(string message)
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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~NamedPipeReader()
        {
            Dispose(false);
        }

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
