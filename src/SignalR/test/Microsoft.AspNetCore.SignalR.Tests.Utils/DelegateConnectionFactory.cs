// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    public class DelegateConnectionFactory : IConnectionFactory
    {
        private readonly Func<TransferFormat, Task<ConnectionContext>> _connectDelegate;
        private readonly Func<ConnectionContext, Task> _disposeDelegate;

        // We have no tests that use the CancellationToken. When we do, we can add it to the delegate. This is test code.
        public DelegateConnectionFactory(Func<TransferFormat, Task<ConnectionContext>> connectDelegate, Func<ConnectionContext, Task> disposeDelegate)
        {
            _connectDelegate = connectDelegate;
            _disposeDelegate = disposeDelegate;
        }

        public Task<ConnectionContext> ConnectAsync(TransferFormat transferFormat, CancellationToken cancellationToken)
        {
            return _connectDelegate(transferFormat);
        }

        public Task DisposeAsync(ConnectionContext connection)
        {
            return _disposeDelegate(connection);
        }
    }
}
