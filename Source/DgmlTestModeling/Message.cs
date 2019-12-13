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
        /// Client is connected
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

    /// <summary>
    /// The ConnectedMessage is sent to the server when client is connected to server.
    /// </summary>
    [DataContract]
    public class ConnectedMessage : Message
    {
        string userName;

        /// <summary>
        /// Construct new ConnectedMessage
        /// </summary>
        public ConnectedMessage()
            : base(MessageType.Connected)
        {
        }

        /// <summary>
        /// Construct new ConnectedMessage with given user name.
        /// </summary>
        /// <param name="user"></param>
        public ConnectedMessage(string user)
            : base(MessageType.Connected)
        {
            this.userName = user;
        }

        /// <summary>
        /// The user name of connected client.
        /// </summary>
        [DataMember]
        public string User
        {
            get { return this.userName; }
            set { this.userName = value; }
        }

    }

    /// <summary>
    /// ClearTextMessages are sent to simply communicate a simple string.
    /// </summary>
    [DataContract]
    public class ClearTextMessage : Message
    {
        /// <summary>
        /// Construct new empty ClearTextMessage
        /// </summary>
        public ClearTextMessage()
            : base(MessageType.ClearText)
        {
        }

        /// <summary>
        /// Construct new ClearTextMessage with given message
        /// </summary>
        /// <param name="text">The message to send</param>
        public ClearTextMessage(string text)
            : base(MessageType.ClearText, text)
        {
        }

    }

    /// <summary>
    /// LoadGraphMessage is used to load a given DGML file, this file can act as a template for the
    /// subsequent nodes and links that will be navigated during this session.
    /// </summary>
    [DataContract]
    public class LoadGraphMessage : Message
    {
        string path;

        /// <summary>
        /// Construct new empty LoadGraphMessage
        /// </summary>
        public LoadGraphMessage()
            : base(MessageType.LoadGraph)
        {
        }

        /// <summary>
        /// Construct new LoadGraphMessage with given file path.
        /// </summary>
        /// <param name="path">The path to the DGML graph file</param>
        public LoadGraphMessage(string path)
            : base(MessageType.LoadGraph)
        {
            this.path = path;
        }

        /// <summary>
        /// Get or set the DGML file path
        /// </summary>
        [DataMember]
        public string Path
        {
            get { return this.path; }
            set { this.path = value; }
        }

    }

    /// <summary>
    /// CreateNodeMessage is sent to communicate that we are creating a new node (in optional group)
    /// </summary>
    [DataContract]
    public class CreateNodeMessage : Message
    {
        string nodeId;
        string nodeLabel;
        bool isGroup;
        string category;
        string parentGroupId;

        /// <summary>
        /// Construct new empty CreateNodeMessage 
        /// </summary>
        public CreateNodeMessage()
            : base(MessageType.NavigateToNode)
        {
        }

        /// <summary>
        /// Construct new CreateNodeMessage with given details.
        /// </summary>
        /// <param name="nodeId">The id of the node we are navigating to</param>
        /// <param name="nodeLabel">The label for that node</param>
        /// <param name="category">An optional node category</param>
        /// <param name="isGroup">Whether the new node should be a group</param>
        /// <param name="parentGroupId">The optional id of the group this node is contained in</param>
        public CreateNodeMessage(string nodeId, string nodeLabel, string category=null, bool isGroup=false, string parentGroupId=null)
            : base(MessageType.NavigateToNode)
        {
            this.nodeId = nodeId;
            this.nodeLabel = nodeLabel;
        }

        /// <summary>
        /// Get or set the node id of the new node.
        /// </summary>
        [DataMember]
        public string NodeId
        {
            get { return this.nodeId; }
            set { this.nodeId = value; }
        }

        /// <summary>
        /// Get or set the node label.
        /// </summary>
        [DataMember]
        public string NodeLabel
        {
            get { return this.nodeLabel; }
            set { this.nodeLabel = value; }
        }

        /// <summary>
        /// Get or set the node category.
        /// </summary>
        [DataMember]
        public string Category
        {
            get { return this.category; }
            set { this.category = value; }
        }

        /// <summary>
        /// Get or set a boolean indicating if the new node should be turned into a Group node.
        /// </summary>
        [DataMember]
        public bool IsGroup
        {
            get { return this.isGroup; }
            set { this.isGroup = value; }
        }

        /// <summary>
        /// Get or set an optional id of a parent group that this node should be contained inside of.
        /// </summary>
        [DataMember]
        public string ParentGroupId
        {
            get { return this.parentGroupId; }
            set { this.parentGroupId = value; }
        }
    }

    /// <summary>
    /// CreatLinkMessage is sent to communicate that we are creating a new link
    /// </summary>
    [DataContract]
    public class CreateLinkMessage : Message
    {
        string sourceId;
        string targetId;
        string label;
        int index;
        string category;

        /// <summary>
        /// Construct new empty CreatLinkMessage 
        /// </summary>
        public CreateLinkMessage()
            : base(MessageType.NavigateToNode)
        {
        }

        /// <summary>
        /// Construct new CreatLinkMessage with given details.
        /// </summary>
        /// <param name="sourceId">The id of the source node</param>
        /// <param name="targetId">The id of the target node</param>
        /// <param name="label">An optional link label</param>
        /// <param name="index">An optional link index (default to zero)</param>
        /// <param name="category">An optional link category</param>
        public CreateLinkMessage(string sourceId, string targetId, string label=null, int index=0, string category=null)
            : base(MessageType.NavigateToNode)
        {
            this.sourceId = sourceId;
            this.targetId = targetId;
            this.label = label;
            this.index = index;
            this.category = category;
        }

        /// <summary>
        /// Get or set the source node id.
        /// </summary>
        [DataMember]
        public string SourceId
        {
            get { return this.sourceId; }
            set { this.sourceId = value; }
        }

        /// <summary>
        /// Get or set the target node id.
        /// </summary>
        [DataMember]
        public string TargetId
        {
            get { return this.targetId; }
            set { this.targetId = value; }
        }

        /// <summary>
        /// Get or set a the link label.
        /// </summary>
        [DataMember]
        public string Label
        {
            get { return this.label; }
            set { this.label = value; }
        }

        /// <summary>
        /// Get or set a the link index.
        /// </summary>
        [DataMember]
        public int Index
        {
            get { return this.index; }
            set { this.index = value; }
        }

        /// <summary>
        /// Get or set the node category.
        /// </summary>
        [DataMember]
        public string Category
        {
            get { return this.category; }
            set { this.category = value; }
        }

    }

    /// <summary>
    /// NavigateNodeMessage is sent to communicate that we are navigating to a given node.
    /// </summary>
    [DataContract]
    public class NavigateNodeMessage : Message
    {
        string nodeId;

        /// <summary>
        /// Construct new empty NavigateNodeMessage 
        /// </summary>
        public NavigateNodeMessage()
            : base(MessageType.NavigateToNode)
        {
        }

        /// <summary>
        /// Construct new NavigateNodeMessage with given node id and label.  It is assumed this node 
        /// has been created already using CreateNodeMessage.
        /// </summary>
        /// <param name="nodeId">The id of the node we are navigating to</param>
        public NavigateNodeMessage(string nodeId)
            : base(MessageType.NavigateToNode)
        {
            this.nodeId = nodeId;
        }

        /// <summary>
        /// Get or set the node id.
        /// </summary>
        [DataMember]
        public string NodeId
        {
            get { return this.nodeId; }
            set { this.nodeId = value; }
        }
    }

    /// <summary>
    /// NavigateLinkMessage is used to indicate we are navigating a particular link in the graph.
    /// It is assumed this link has been created already using CreateLinkMessage.
    /// </summary>
    [DataContract]
    public class NavigateLinkMessage : Message
    {
        string srcNodeId;
        string targetNodeId;

        /// <summary>
        /// Construct new empty NavigateLinkMessage message
        /// </summary>
        public NavigateLinkMessage()
            : base(MessageType.NavigateLink)
        {
        }

        /// <summary>
        /// Construct new NavigateLinkMessage with the given link details.
        /// </summary>
        /// <param name="srcNodeId">The source node id</param>
        /// <param name="targetNodeId">The target node id</param>
        public NavigateLinkMessage(string srcNodeId, string targetNodeId)
            : base(MessageType.NavigateLink)
        {
            this.srcNodeId = srcNodeId;
            this.targetNodeId = targetNodeId;
        }

        /// <summary>
        /// Get or set the link source node id
        /// </summary>
        [DataMember]
        public string SourceNodeId { get { return this.srcNodeId; } set { this.srcNodeId = value; } }

        /// <summary>
        /// Get or set the link target node id
        /// </summary>
        [DataMember]
        public string TargetNodeId { get { return this.targetNodeId; } set { this.targetNodeId = value; } }
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
