// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Connections
{
    public interface IConnectionListener : IAsyncDisposable
    {
        EndPoint EndPoint { get; }

        ValueTask<ConnectionContext> AcceptAsync(CancellationToken cancellationToken = default);

        ValueTask UnbindAsync(CancellationToken cancellationToken = default);
    }
}
