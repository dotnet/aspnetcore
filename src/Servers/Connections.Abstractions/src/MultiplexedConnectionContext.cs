// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Connections
{
    public abstract class MultiplexedConnectionContext : ConnectionContext
    {
        public override IDuplexPipe Transport { get; set; } = null;
        public abstract ValueTask<StreamContext> AcceptAsync(CancellationToken cancellationToken = default);
        public abstract ValueTask<StreamContext> ConnectAsync(IFeatureCollection features = null, bool unidirectional = false, CancellationToken cancellationToken = default);
    }
}
