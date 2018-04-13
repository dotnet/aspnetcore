// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;

namespace Microsoft.AspNetCore.SignalR.Client
{
    public interface IConnectionFactory
    {
        Task<ConnectionContext> ConnectAsync(TransferFormat transferFormat);

        Task DisposeAsync(ConnectionContext connection);
    }
}