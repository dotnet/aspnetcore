// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.SignalR.Crankier.Server
{
    public class ConnectionCounterHostedService : IHostedService, IDisposable
    {
        private Stopwatch _timeSinceFirstConnection;
        private readonly ConnectionCounter _counter;
        private ConnectionSummary _lastSummary;
        private Timer _timer;
        private int _executingDoWork;

        public ConnectionCounterHostedService(ConnectionCounter counter)
        {
            _counter = counter;
            _timeSinceFirstConnection = new Stopwatch();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
            
            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            if (Interlocked.Exchange(ref _executingDoWork, 1) == 0)
            {
                var summary = _counter.Summary;

                if (summary.PeakConnections > 0)
                {
                    if (_timeSinceFirstConnection.ElapsedTicks == 0)
                    {
                        _timeSinceFirstConnection.Start();
                    }

                    var elapsed = _timeSinceFirstConnection.Elapsed;

                    if (_lastSummary != null)
                    {
                        Console.WriteLine(@"[{0:hh\:mm\:ss}] Current: {1}, peak: {2}, connected: {3}, disconnected: {4}, rate: {5}/s",
                            elapsed,
                            summary.CurrentConnections,
                            summary.PeakConnections,
                            summary.TotalConnected - _lastSummary.TotalConnected,
                            summary.TotalDisconnected - _lastSummary.TotalDisconnected,
                            summary.CurrentConnections - _lastSummary.CurrentConnections
                            );
                    }

                    _lastSummary = summary;
                }

                Interlocked.Exchange(ref _executingDoWork, 0);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}