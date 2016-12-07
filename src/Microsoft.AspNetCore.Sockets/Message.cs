// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;

namespace Microsoft.AspNetCore.Sockets
{
    public struct Message : IDisposable
    {
        public bool EndOfMessage { get; }
        public Format MessageFormat { get; }
        public PreservedBuffer Payload { get; }

        public Message(PreservedBuffer payload, Format messageFormat, bool endOfMessage)
        {
            MessageFormat = messageFormat;
            EndOfMessage = endOfMessage;
            Payload = payload;
        }

        public void Dispose()
        {
            Payload.Dispose();
        }
    }
}
