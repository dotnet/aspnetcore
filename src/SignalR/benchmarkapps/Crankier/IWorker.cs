// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Connections;

namespace Microsoft.AspNetCore.SignalR.Crankier
{
    public interface IWorker
    {
        Task PingAsync(int value);
        Task ConnectAsync(string targetAddress, HttpTransportType transportType, int numberOfConnections);
        Task StartTestAsync(TimeSpan sendInterval, int sendBytes);
        Task StopAsync();
    }
}
