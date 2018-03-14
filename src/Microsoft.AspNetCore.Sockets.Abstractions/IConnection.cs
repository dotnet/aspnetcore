// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Protocols;

namespace Microsoft.AspNetCore.Sockets.Client
{
    public interface IConnection
    {
        Task StartAsync(TransferFormat transferFormat);
        Task SendAsync(byte[] data, CancellationToken cancellationToken);
        Task StopAsync();
        Task DisposeAsync();
        Task AbortAsync(Exception ex);

        IDisposable OnReceived(Func<byte[], object, Task> callback, object state);

        event Action<Exception> Closed;

        IFeatureCollection Features { get; }
    }
}
