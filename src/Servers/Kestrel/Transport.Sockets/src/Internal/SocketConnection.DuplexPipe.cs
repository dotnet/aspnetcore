// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
