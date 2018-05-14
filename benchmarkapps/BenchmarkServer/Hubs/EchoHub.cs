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
        public async Task Broadcast(int duration)
        {
            var sent = 0;
            try
            {
                var t = new CancellationTokenSource();
                t.CancelAfter(TimeSpan.FromSeconds(duration));
                while (!t.IsCancellationRequested && !Context.ConnectionAborted.IsCancellationRequested)
                {
                    await Clients.All.SendAsync("send", DateTime.UtcNow);
                    sent++;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            Console.WriteLine("Broadcast exited: Sent {0} messages", sent);
        }

        public DateTime Echo(DateTime time)
        {
            return time;
        }

        public Task EchoAll(DateTime time)
        {
            return Clients.All.SendAsync("send", time);
        }

        public void SendPayload(string payload)
        {
            // Dump the payload, we don't care
        }

        public DateTime GetCurrentTime()
        {
            return DateTime.UtcNow;
        }
    }
}
