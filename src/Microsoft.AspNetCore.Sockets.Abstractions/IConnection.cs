// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Sockets.Client
{
    public interface IConnection
    {
        Task StartAsync();
        Task SendAsync(byte[] data, CancellationToken cancellationToken);
        Task DisposeAsync();

        event Action Connected;
        event Func<byte[], Task> Received;
        event Action<Exception> Closed;
    }
}
