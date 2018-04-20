// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;

namespace Microsoft.AspNetCore.SignalR.Client
{
    public interface IConnectionFactory
    {
        Task<ConnectionContext> ConnectAsync(TransferFormat transferFormat, CancellationToken cancellationToken = default);

        // Current plan for IAsyncDisposable is that DisposeAsync will NOT take a CancellationToken
        // https://github.com/dotnet/csharplang/blob/195efa07806284d7b57550e7447dc8bd39c156bf/proposals/async-streams.md#iasyncdisposable
        Task DisposeAsync(ConnectionContext connection);
    }
}
