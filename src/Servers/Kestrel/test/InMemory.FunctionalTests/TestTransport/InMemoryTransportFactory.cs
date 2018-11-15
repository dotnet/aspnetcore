// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport
{
    public class InMemoryTransportFactory : ITransportFactory
    {
        public ITransport Create(IEndPointInformation endPointInformation, IConnectionDispatcher dispatcher)
        {
            if (ConnectionDispatcher != null)
            {
                throw new InvalidOperationException("InMemoryTransportFactory doesn't support creating multiple endpoints");
            }

            ConnectionDispatcher = dispatcher;

            return new NoopTransport();
        }

        public IConnectionDispatcher ConnectionDispatcher { get; private set; }

        private class NoopTransport : ITransport
        {
            public Task BindAsync()
            {
                return Task.CompletedTask;
            }

            public Task StopAsync()
            {
                return Task.CompletedTask;
            }

            public Task UnbindAsync()
            {
                return Task.CompletedTask;
            }
        }
    }
}
