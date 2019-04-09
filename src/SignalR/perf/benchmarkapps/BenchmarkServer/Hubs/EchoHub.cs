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
        static int connectionCount = 0;
        static int peakConnectionCount = 0;
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

        public static string Status => $"{connectionCount} current, {peakConnectionCount} peak.";

        public override Task OnConnectedAsync() {
            connectionCount++;
            peakConnectionCount = Math.Max(connectionCount, peakConnectionCount);
            if (connectionCount < 1000 || connectionCount % 100 == 0)
                Console.WriteLine($"Connected: {Status}");
            return Task.CompletedTask;
        }

        public override Task OnDisconnectedAsync(Exception exception) {
            connectionCount--;
            if (connectionCount < 1000 || connectionCount % 100 == 0)
                Console.WriteLine($"Disconnected: {Status}");
            return Task.CompletedTask;
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
