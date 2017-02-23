// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Adapter;
using Microsoft.AspNetCore.Server.Kestrel.Adapter.Internal;
using Microsoft.AspNetCore.Testing;

namespace Microsoft.AspNetCore.Testing
{
    public class PassThroughConnectionAdapter : IConnectionAdapter
    {
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

            public void PrepareRequest(IFeatureCollection requestFeatures)
            {
            }
        }
    }
}
