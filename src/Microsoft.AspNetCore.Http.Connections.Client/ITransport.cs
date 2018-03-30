// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;

namespace Microsoft.AspNetCore.Http.Connections.Client
{
    public interface ITransport
    {
        Task StartAsync(Uri url, IDuplexPipe application, TransferFormat transferFormat, IConnection connection);
        Task StopAsync();
    }
}
