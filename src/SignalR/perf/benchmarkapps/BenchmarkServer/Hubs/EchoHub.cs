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
        static int _connectionCount = 0;
        static int _peakConnectionCount = 0;

        static DateTime _serverStart = DateTime.Now;

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

        public static string Status => $"{_connectionCount} current, {_peakConnectionCount} peak.";

        public void LogConnections(string label) {
            if (_connectionCount < 1000 || _connectionCount % 100 == 0)
            {
                var timeSinceServerStart = DateTime.Now.Subtract(_serverStart).ToString(@"hh\:mm\:ss");
                Console.WriteLine($"[{timeSinceServerStart}] {label}: {Status}");
            }
        }

        public override Task OnConnectedAsync() {
            _connectionCount++;
            _peakConnectionCount = Math.Max(_connectionCount, _peakConnectionCount);
            LogConnections("Connected");
            return Task.CompletedTask;
        }

        public override Task OnDisconnectedAsync(Exception exception) {
            _connectionCount--;
            LogConnections("Disconnected");
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
