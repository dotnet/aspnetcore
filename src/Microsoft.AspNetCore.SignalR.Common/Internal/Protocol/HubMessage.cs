// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;

namespace Microsoft.AspNetCore.SignalR.Internal.Protocol
{
    public abstract class HubMessage
    {
        protected HubMessage()
        {
        }

        private object _lock = new object();
        private List<SerializedMessage> _serializedMessages;

        public byte[] WriteMessage(IHubProtocol protocol)
        {
            // REVIEW: Revisit lock
            // Could use a reader/writer lock to allow the loop to take place in "unlocked" code
            // Or, could use a fixed size array and Interlocked to manage it.
            // Or, Immutable *ducks*

            lock (_lock)
            {
                for (var i = 0; i < _serializedMessages?.Count; i++)
                {
                    if (_serializedMessages[i].Protocol.Equals(protocol))
                    {
                        return _serializedMessages[i].Message;
                    }
                }

                var bytes = protocol.WriteToArray(this);

                if (_serializedMessages == null)
                {
                    // Initialize with capacity 2 for the 2 built in protocols
                    _serializedMessages = new List<SerializedMessage>(2);
                }

                // We don't want to balloon memory if someone writes a poor IHubProtocolResolver
                // So we cap how many caches we store and worst case just serialize the message for every connection
                if (_serializedMessages.Count < 10)
                {
                    _serializedMessages.Add(new SerializedMessage(protocol, bytes));
                }

                return bytes;
            }
        }

        private readonly struct SerializedMessage
        {
            public readonly IHubProtocol Protocol;
            public readonly byte[] Message;

            public SerializedMessage(IHubProtocol protocol, byte[] message)
            {
                Protocol = protocol;
                Message = message;
            }
        }
    }
}
