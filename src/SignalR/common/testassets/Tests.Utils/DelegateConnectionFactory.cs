// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    public class DelegateConnectionFactory : IConnectionFactory
    {
        private readonly Func<EndPoint, ValueTask<ConnectionContext>> _connectDelegate;

        // We have no tests that use the CancellationToken. When we do, we can add it to the delegate. This is test code.
        public DelegateConnectionFactory(Func<EndPoint, ValueTask<ConnectionContext>> connectDelegate)
        {
            _connectDelegate = connectDelegate;
        }

        public ValueTask<ConnectionContext> ConnectAsync(EndPoint endPoint, CancellationToken cancellationToken)
        {
            return _connectDelegate(endPoint);
        }
    }
}
