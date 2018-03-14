// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace BenchmarkServer.Hubs
{
    public class EchoHub : Hub
    {
        public async Task Echo(int duration)
        {
            try
            {
                var t = new CancellationTokenSource();
                t.CancelAfter(TimeSpan.FromSeconds(duration));
                while (!t.IsCancellationRequested && !Context.Connection.ConnectionAbortedToken.IsCancellationRequested)
                {
                    await Clients.All.SendAsync("echo", DateTime.UtcNow);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            Console.WriteLine("Echo exited");
        }
    }
}
