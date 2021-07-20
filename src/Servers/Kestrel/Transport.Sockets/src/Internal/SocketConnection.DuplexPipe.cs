// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO.Pipelines;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal
{
    internal sealed partial class SocketConnection
    {
        // We could implement this on SocketConnection to remove an extra allocation but this is a
        // bit cleaner
        private class SocketDuplexPipe : IDuplexPipe
        {
            public SocketDuplexPipe(SocketConnection connection)
            {
                Input = new SocketPipeReader(connection);
                Output = new SocketPipeWriter(connection);
            }

            public PipeReader Input { get; }

            public PipeWriter Output { get; }
        }
    }
}
