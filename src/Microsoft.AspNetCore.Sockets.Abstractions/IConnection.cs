// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Connections;

namespace Microsoft.AspNetCore.Sockets.Client
{
    public interface IConnection
    {
        IDuplexPipe Transport { get; }
        IFeatureCollection Features { get; }

        Task StartAsync();
        Task StartAsync(TransferFormat transferFormat);
        Task DisposeAsync();
    }
}
