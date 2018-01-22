// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using System.Threading.Channels;

namespace Microsoft.AspNetCore.Sockets.Client
{
    public interface ITransport
    {
        Task StartAsync(Uri url, Channel<byte[], SendMessage> application, TransferMode requestedTransferMode, IConnection connection);
        Task StopAsync();
        TransferMode? Mode { get; }
    }
}
