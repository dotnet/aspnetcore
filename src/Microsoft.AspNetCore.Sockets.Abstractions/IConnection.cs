// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Sockets.Client
{
    public interface IConnection
    {
        Task StartAsync();
        Task SendAsync(byte[] data, CancellationToken cancellationToken);
        Task DisposeAsync();
        Task AbortAsync(Exception ex);

        IDisposable OnReceived(Func<byte[], object, Task> callback, object state);

        Task Closed { get; }

        IFeatureCollection Features { get; }
    }
}
