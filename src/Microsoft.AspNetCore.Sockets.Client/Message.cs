// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;

namespace Microsoft.AspNetCore.Sockets.Client
{
    public struct Message : IDisposable
    {
        public Format MessageFormat { get; }
        public PreservedBuffer Payload { get; }

        public Message(PreservedBuffer payload, Format messageFormat)
        {
            MessageFormat = messageFormat;
            Payload = payload;
        }

        public void Dispose()
        {
            Payload.Dispose();
        }
    }
}
