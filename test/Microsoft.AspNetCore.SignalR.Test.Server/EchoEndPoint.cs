// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Sockets;

namespace Microsoft.AspNetCore.SignalR.Test.Server
{
    public class EchoEndPoint : StreamingEndPoint
    {
        public async override Task OnConnectedAsync(StreamingConnection connection)
        {
            await connection.Transport.Input.CopyToAsync(connection.Transport.Output);
        }
    }
}
