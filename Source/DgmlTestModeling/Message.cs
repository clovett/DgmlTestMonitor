using Microsoft.Coyote.SmartSockets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
//using System.Windows.Input;

namespace LovettSoftware.DgmlTestModeling
{
    /// <summary>
    /// An enum that describes the type of messages we will send.
    /// </summary>
    public enum MessageType
    {
        /// <summary>
        /// Client is conencted
        /// </summary>
        Connected,
        /// <summary>
        /// Client has disconnected
        /// </summary>
        Disconnected,
        /// <summary>
        /// A simple ping message
        /// </summary>
        Ping,
        /// <summary>
        /// A simple text message
        /// </summary>
        ClearText,
        /// <summary>
        /// Command to load a DGML graph
        /// </summary>
        LoadGraph,
        /// <summary>
        /// Navigate to a node
        /// </summary>
        NavigateToNode,
        /// <summary>
        /// Navigate a link.
        /// </summary>
        NavigateLink
    }

    /// <summary>
    /// The base class for all DgmlTestModeling messages
    /// </summary>
    [DataContract]
    public class Message : SocketMessage
    {
        long timestamp;
        MessageType type;

        /// <summary>
        /// Construct a new message.
        /// </summary>
        public Message(MessageType type, string message = null) : base(type.ToString(), message)
        {
            this.type = type;
        }

        /// <summary>
        /// A timestamp for when the message was sent.
        /// </summary>
        [DataMember]
        public long Timestamp
        {
            get { return timestamp; }
            set { timestamp = value; }
        }

        /// <summary>
        /// The message type
        /// </summary>
        [DataMember]
        public MessageType Type
        {
            get { return type; }
            set { type = value; }
        }

        /// <summary>
        /// An optional method for merging messages if they start piling up.
        /// </summary>
        public virtual bool Merge(Message other)
        {
            return false;
        }
    }

    [DataContract]
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

        [DataMember]
        public string User
        {
            get { return this.userName; }
            set { this.userName = value; }
        }

    }

    [DataContract]
    public class ClearTextMessage : Message
    {
        public ClearTextMessage()
            : base(MessageType.ClearText)
        {
        }

        public ClearTextMessage(string text)
            : base(MessageType.ClearText, text)
        {
        }

    }


    [DataContract]
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

        [DataMember]
        public string Path
        {
            get { return this.path; }
            set { this.path = value; }
        }

    }


    [DataContract]
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

        [DataMember]
        public string NodeId
        {
            get { return this.nodeId; }
            set { this.nodeId = value; }
        }

        [DataMember]
        public string NodeLabel
        {
            get { return this.nodeLabel; }
            set { this.nodeLabel = value; }
        }
    }

    [DataContract]
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

        [DataMember]
        public string SourceNodeId { get { return this.srcNodeId; } set { this.srcNodeId = value; } }

        [DataMember]
        public string SourceNodeLabel { get { return this.srcNodeLabel; } set { this.srcNodeLabel = value; } }


        [DataMember]
        public string TargetNodeId { get { return this.targetNodeId; } set { this.targetNodeId = value; } }

        [DataMember]
        public string TargetNodeLabel { get { return this.targetNodeLabel; } set { this.targetNodeLabel = value; } }

        [DataMember]
        public string Label { get { return this.label; } set { this.label = value; } }

        [DataMember]
        public int Index { get { return this.index; } set { this.index = value; } }
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
