// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
