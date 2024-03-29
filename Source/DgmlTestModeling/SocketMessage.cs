﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Runtime.Serialization;

namespace Microsoft.Coyote.SmartSockets
{
    /// <summary>
    /// This is the base class for messages send over SmartSockets.
    /// </summary>
    [DataContract]
    public class SocketMessage
    {
        /// <summary>
        /// Construct a new SocketMessage object.
        /// </summary>
        /// <param name="id">The message identifier</param>
        /// <param name="sender">The message sender</param>
        public SocketMessage(string id, string sender)
        {
            this.Id = id;
            this.Sender = sender;
        }

        /// <summary>
        /// This is like a message type, class of message or are completely unique id.
        /// It's up to you how you want to use it.
        /// </summary>
        [DataMember]
        public string Id { get; set; }

        /// <summary>
        /// This will be filled automatically by the SmartSocket class so you
        /// </summary>
        [DataMember]
        public string Sender { get; set; }

        /// <summary>
        /// An optional string message
        /// </summary>
        [DataMember]
        public string Message { get; set; }
    }
}
