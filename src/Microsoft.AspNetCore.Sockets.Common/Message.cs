// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Text;
using System.Text.Formatting;

namespace Microsoft.AspNetCore.Sockets
{
    public struct Message : IDisposable
    {
        public bool EndOfMessage { get; }
        public MessageType Type { get; }
        public PreservedBuffer Payload { get; }

        public Message(PreservedBuffer payload, MessageType type)
            : this(payload, type, endOfMessage: true)
        {

        }

        public Message(PreservedBuffer payload, MessageType type, bool endOfMessage)
        {
            Type = type;
            EndOfMessage = endOfMessage;
            Payload = payload;
        }

        public void Dispose()
        {
            Payload.Dispose();
        }
    }
}
