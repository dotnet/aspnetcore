// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core.Adapter.Internal;

namespace Microsoft.AspNetCore.Testing
{
    public class PassThroughConnectionAdapter : IConnectionAdapter
    {
        public bool IsHttps => false;

        public Task<IAdaptedConnection> OnConnectionAsync(ConnectionAdapterContext context)
        {
            var adapted = new AdaptedConnection(new LoggingStream(context.ConnectionStream, new TestApplicationErrorLogger()));
            return Task.FromResult<IAdaptedConnection>(adapted);
        }

        private class AdaptedConnection : IAdaptedConnection
        {
            public AdaptedConnection(Stream stream)
            {
                ConnectionStream = stream;
            }

            public Stream ConnectionStream { get; }

            public void Dispose()
            {
            }
        }
    }
}
