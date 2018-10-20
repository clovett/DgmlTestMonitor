using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Pipes;
using System.IO;

namespace Microsoft.VisualStudio.DgmlTestModeling
{
    /// <summary>
    /// This class writes information to a named pipe that tells the DGML Test Monitor
    /// VSIX plugin where we are at during the model execution.  You 
    /// </summary>
    internal class NamedPipeWriter
    {
        NamedPipeClientStream pipe;
        TextWriter log;
        const int MaxMessageBytes = 1024;

        /// <summary>
        /// Construct a NamedPipeWriter for writing messages to the DGML Test Monitor VSIX plugin.
        /// This class is used by the DgmlTestModel and so you should not need to use it directly.
        /// </summary>
        /// <param name="log"></param>
        public NamedPipeWriter(TextWriter log)
        {
            this.log = log;
            try
            {
                pipe = new NamedPipeClientStream(".", "63642A12-F751-41E3-A9D3-279EE34A0EDB-DgmlTestMonitor", PipeDirection.InOut);
                pipe.Connect(1000);
            }
            catch
            {
                // no listener then
                Close();
            }
        }

        /// <summary>
        /// Close the pipe.
        /// </summary>
        public void Close()
        {
            using (pipe)
            {
                if (pipe != null)
                {
                    pipe.Close();
                }
            }
            pipe = null;
        }

        public string WriteMessage(string format, params object[] args)
        {
            string msg = string.Format(format, args);
            WriteMessage(msg);

            string response = ReadMessage();
            return response;
        }

        /// <summary>
        /// Read a string from the pipe. We assume all strings are unicode.
        /// Only MaxBytes characters will be read, meaning the max string length
        /// is MaxBytes / BytesPerChar.
        /// </summary>
        public string ReadMessage()
        {
            if (pipe != null && pipe.IsConnected)
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
            return "";
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
                log.WriteLine(message);
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

    }
}
