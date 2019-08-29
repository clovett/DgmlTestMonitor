using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using System.Windows.Input;

namespace LovettSoftware.DgmlTestModeling
{
    public enum MessageType
    {
        Connected,
        Disconnected,
        Ping,
        ClearText,
        LoadGraph,
        NavigateToNode,
        NavigateLink
    }

    public class Message
    {
        long messageId;
        long timestamp;
        MessageType type;
        public static uint MessageHeader = 0xFE771325;

        public Message(MessageType type)
        {
            this.type = type;
        }

        public long MessageId
        {
            get { return messageId; }
            set { messageId = value; }
        }

        public long Timestamp
        {
            get { return timestamp; }
            set { timestamp = value; }
        }

        public MessageType Type { get { return type; } }

        public virtual bool Merge(Message other)
        {
            return false;
        }

        public virtual void Write(BinaryWriter writer)
        {
            writer.Write(MessageHeader);
            writer.Write((int)this.type);
            writer.Write((long)this.messageId);
            writer.Write((long)this.timestamp);
        }

        public virtual void Read(BinaryReader reader)
        {
            // MessageHeader and type is already read by Message.Create method
            this.messageId = reader.ReadInt64();
            this.timestamp = reader.ReadInt64();
        }

        public static Message Create(BinaryReader reader)
        {
            uint header = reader.ReadUInt32();
            while (header != MessageHeader)
            {
                // hmmm, we are out of position, try skipping to the next header.
                header >>= 8;
                header += ((uint)reader.ReadByte() << 24);
            }

            MessageType type = (MessageType)reader.ReadInt32();
            Message msg = null;

            switch (type)
            {
                case MessageType.ClearText:
                    msg = new ClearTextMessage();
                    break;
                case MessageType.Connected:
                    msg = new ConnectedMessage();
                    break;
                case MessageType.Disconnected:
                    msg = new Message(MessageType.Disconnected);
                    break;
                case MessageType.Ping:
                    msg = new Message(MessageType.Ping);
                    break;
                case MessageType.LoadGraph:
                    msg = new LoadGraphMessage();
                    break;
                case MessageType.NavigateLink:
                    msg = new NavigateLinkMessage();
                    break;
                case MessageType.NavigateToNode:
                    msg = new NavigateNodeMessage();
                    break;
                default:
                    Debug.WriteLine("Unexpected MessageType: " + type);
                    break;
            }
            if (msg != null)
            {
                msg.Read(reader);
            }
            return msg;
        }

        /// <summary>
        /// Serialize the message into a buffer for sending over the wire.
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray()
        {
            // Successfully connected to the server 
            // Send data to the server 
            MemoryStream ms = new MemoryStream();
            using (BinaryWriter dw = new BinaryWriter(ms))
            {
                this.Write(dw);
                dw.Flush();
            }

            byte[] buffer = ms.ToArray();
            int length = buffer.Length;

            // now send the message prefixed by the length of the message.                
            ms = new MemoryStream();
            using (BinaryWriter dw = new BinaryWriter(ms))
            {
                dw.Write(length);
                dw.Write(buffer, 0, length);
                dw.Flush();
            }

            return ms.ToArray();
        }
    }

    public class ConnectedMessage : Message
    {
        string userName;

        public ConnectedMessage()
            : base(MessageType.Connected)
        {
        }

        public ConnectedMessage(string user)
            : base(MessageType.Connected)
        {
            this.userName = user;
        }

        public string User { get { return this.userName; } set { this.userName = value; } }

        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);
            writer.Write("" + this.userName);
        }
        public override void Read(BinaryReader reader)
        {
            base.Read(reader);
            this.userName = reader.ReadString();
        }
    }

    public class ClearTextMessage : Message
    {
        string text;

        public ClearTextMessage()
            : base(MessageType.ClearText)
        {
        }

        public ClearTextMessage(string text)
            : base(MessageType.ClearText)
        {
            this.text = text;
        }

        public string Message { get { return this.text; } set { this.text = value; } }

        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);
            writer.Write("" + this.text);
        }
        public override void Read(BinaryReader reader)
        {
            base.Read(reader);
            this.text = reader.ReadString();
        }
    }


    public class LoadGraphMessage : Message
    {
        string path;

        public LoadGraphMessage()
            : base(MessageType.LoadGraph)
        {
        }

        public LoadGraphMessage(string path)
            : base(MessageType.LoadGraph)
        {
            this.path = path;
        }

        public string Path { get { return this.path; } set { this.path = value; } }

        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);
            writer.Write("" + this.path);
        }
        public override void Read(BinaryReader reader)
        {
            base.Read(reader);
            this.path = reader.ReadString();
        }
    }


    public class NavigateNodeMessage : Message
    {
        string nodeId;
        string nodeLabel;

        public NavigateNodeMessage()
            : base(MessageType.NavigateToNode)
        {
        }

        public NavigateNodeMessage(string nodeId, string nodeLabel)
            : base(MessageType.NavigateToNode)
        {
            this.nodeId = nodeId;
            this.nodeLabel = nodeLabel;
        }

        public string NodeId { get { return this.nodeId; } set { this.nodeId = value; } }
        public string NodeLabel { get { return this.nodeLabel; } set { this.nodeLabel = value; } }

        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);
            writer.Write("" + this.nodeId);
            writer.Write("" + this.nodeLabel);
        }
        public override void Read(BinaryReader reader)
        {
            base.Read(reader);
            this.nodeId = reader.ReadString();
            this.nodeLabel = reader.ReadString();
        }
    }



    public class NavigateLinkMessage : Message
    {
        string srcNodeId;
        string srcNodeLabel;
        string targetNodeId;
        string targetNodeLabel;
        string label;
        int index;

        public NavigateLinkMessage()
            : base(MessageType.NavigateLink)
        {
        }

        public NavigateLinkMessage(string srcNodeId, string srcNodeLabel, string targetNodeId, string targetNodeLabel, string label, int index)
            : base(MessageType.NavigateLink)
        {
            this.srcNodeId = srcNodeId;
            this.srcNodeLabel = srcNodeLabel;
            this.targetNodeId = targetNodeId;
            this.targetNodeLabel = targetNodeLabel;
            this.label = label;
            this.index = index;
        }

        public string SourceNodeId { get { return this.srcNodeId; } set { this.srcNodeId = value; } }
        public string SourceNodeLabel { get { return this.srcNodeLabel; } set { this.srcNodeLabel = value; } }

        public string TargetNodeId { get { return this.targetNodeId; } set { this.targetNodeId = value; } }
        public string TargetNodeLabel { get { return this.targetNodeLabel; } set { this.targetNodeLabel = value; } }
        public string Label { get { return this.label; } set { this.label = value; } }
        public int Index { get { return this.index; } set { this.index = value; } }


        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);
            writer.Write("" + this.srcNodeId);
            writer.Write("" + this.srcNodeLabel);
            writer.Write("" + this.targetNodeId);
            writer.Write("" + this.targetNodeLabel);
            writer.Write("" + this.label);
            writer.Write(this.index);
        }

        public override void Read(BinaryReader reader)
        {
            base.Read(reader);
            this.srcNodeId = reader.ReadString();
            this.srcNodeLabel = reader.ReadString();
            this.targetNodeId = reader.ReadString();
            this.targetNodeLabel = reader.ReadString();
            this.label = reader.ReadString();
            this.index = reader.ReadInt32();
        }
    }



    /// <summary>
    /// This event args is used to communicate messages read from the pipe.
    /// </summary>
    public class MessageEventArgs : EventArgs
    {
        /// <summary>
        /// Construct a new PipeMessageEventArgs with the given message.
        /// This is used by the MessageArrived event on the NamedPipeReader
        /// </summary>
        /// <param name="message">The message that was read by the NamedPipeReader</param>
        public MessageEventArgs(Message message)
        {
            Message = message;
        }

        /// <summary>
        /// The message that was read by the NamedPipeReader
        /// </summary>
        public Message Message { get; set; }
    }

}
