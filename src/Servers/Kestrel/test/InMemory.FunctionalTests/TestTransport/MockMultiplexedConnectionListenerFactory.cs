// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections.Experimental;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport
{
    internal class MockMultiplexedConnectionListenerFactory : IMultiplexedConnectionListenerFactory
    {
        public Action<EndPoint, IFeatureCollection> OnBindAsync { get; set; }

        public ValueTask<IMultiplexedConnectionListener> BindAsync(EndPoint endpoint, IFeatureCollection features = null, CancellationToken cancellationToken = default)
        {
            OnBindAsync?.Invoke(endpoint, features);

            return ValueTask.FromResult<IMultiplexedConnectionListener>(new MockMultiplexedConnectionListener(endpoint));
        }

        private class MockMultiplexedConnectionListener : IMultiplexedConnectionListener
        {
            public MockMultiplexedConnectionListener(EndPoint endpoint)
            {
                EndPoint = endpoint;
            }

            public EndPoint EndPoint { get; }

            public ValueTask<MultiplexedConnectionContext> AcceptAsync(IFeatureCollection features = null, CancellationToken cancellationToken = default)
            {
                return ValueTask.FromResult<MultiplexedConnectionContext>(null);
            }

            public ValueTask DisposeAsync()
            {
                return default;
            }

            public ValueTask UnbindAsync(CancellationToken cancellationToken = default)
            {
                return default;
            }
        }
    }
}
