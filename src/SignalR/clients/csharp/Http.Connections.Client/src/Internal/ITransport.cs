// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;

namespace Microsoft.AspNetCore.Http.Connections.Client.Internal
{
    internal interface ITransport : IDuplexPipe
    {
        Task StartAsync(Uri url, TransferFormat transferFormat, CancellationToken cancellationToken = default);
        Task StopAsync();
    }
}
