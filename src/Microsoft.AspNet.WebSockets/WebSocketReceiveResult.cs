//------------------------------------------------------------------------------
// <copyright file="WebSocketReceiveResult.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics.Contracts;

namespace Microsoft.AspNet.WebSockets
{
    public class WebSocketReceiveResult
    {
        public WebSocketReceiveResult(int count, WebSocketMessageType messageType, bool endOfMessage)
            : this(count, messageType, endOfMessage, null, null)
        {
        }

        public WebSocketReceiveResult(int count, 
            WebSocketMessageType messageType, 
            bool endOfMessage, 
            WebSocketCloseStatus? closeStatus,
            string closeStatusDescription)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count");
            }

            this.Count = count;
            this.EndOfMessage = endOfMessage;
            this.MessageType = messageType;
            this.CloseStatus = closeStatus;
            this.CloseStatusDescription = closeStatusDescription;
        }

        public int Count { get; private set; }
        public bool EndOfMessage { get; private set; }
        public WebSocketMessageType MessageType { get; private set; }
        public WebSocketCloseStatus? CloseStatus { get; private set; }
        public string CloseStatusDescription { get; private set; }

        internal WebSocketReceiveResult Copy(int count)
        {
            Contract.Assert(count >= 0, "'count' MUST NOT be negative.");
            Contract.Assert(count <= this.Count, "'count' MUST NOT be bigger than 'this.Count'.");
            this.Count -= count;
            return new WebSocketReceiveResult(count, 
                this.MessageType,
                this.Count == 0 && this.EndOfMessage, 
                this.CloseStatus, 
                this.CloseStatusDescription);
        }
    }
}