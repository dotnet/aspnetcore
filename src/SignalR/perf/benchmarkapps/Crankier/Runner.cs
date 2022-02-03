// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Connections;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.SignalR.Crankier
{
    public class Runner : IRunner
    {
        private readonly Agent _agent;
        private readonly string _targetUrl;
        private readonly int _numberOfWorkers;
        private readonly int _numberOfConnections;
        private readonly int _sendDurationSeconds;
        private readonly HttpTransportType _transportType;

        public Runner(Agent agent, string targetUrl, int numberOfWorkers, int numberOfConnections, int sendDurationInSeconds, HttpTransportType transportType)
        {
            _agent = agent;
            _targetUrl = targetUrl;
            _numberOfWorkers = numberOfWorkers;
            _numberOfConnections = numberOfConnections;
            _sendDurationSeconds = sendDurationInSeconds;
            _transportType = transportType;
        }

        public async Task RunAsync()
        {
            _agent.Runner = this;

            await _agent.StartWorkersAsync(_targetUrl, _numberOfWorkers, _transportType, _numberOfConnections);

            // Begin writing worker status information
            var writeStatusCts = new CancellationTokenSource();
            var writeStatusTask = WriteConnectionStatusAsync(writeStatusCts.Token);

            // Wait until all connections are connected
            while (_agent.GetWorkerStatus().Aggregate(0, (state, status) => state + status.Value.ConnectedCount) <
                _agent.TotalConnectionsRequested)
            {
                await Task.Delay(1000);
            }

            // Stay connected for the duration of the send phase
            await Task.Delay(TimeSpan.FromSeconds(_sendDurationSeconds));

            // Disconnect
            await _agent.StopWorkersAsync();

            // Stop writing worker status information
            writeStatusCts.Cancel();
            await writeStatusTask;
        }

        private Task WriteConnectionStatusAsync(CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                var peakConnections = 0;
                while (!cancellationToken.IsCancellationRequested)
                {
                    var statusDictionary = _agent.GetWorkerStatus();

                    // Total things up
                    var status = new StatusInformation();
                    foreach (var value in statusDictionary.Values)
                    {
                        status = status.Add(value);
                    }

                    peakConnections = Math.Max(peakConnections, status.ConnectedCount);
                    status.PeakConnections = peakConnections;

                    Trace.WriteLine(JsonConvert.SerializeObject(status));

                    await Task.Delay(1000);
                }
            });
        }

        public Task PongWorkerAsync(int workerId, int value)
        {
            throw new NotImplementedException();
        }

        public Task LogAgentAsync(string format, params object[] arguments)
        {
            Trace.WriteLine(string.Format(format, arguments));
            return Task.CompletedTask;
        }

        public Task LogWorkerAsync(int workerId, string format, params object[] arguments)
        {
            Trace.WriteLine(string.Format("({0}) {1}", workerId, string.Format(format, arguments)));
            return Task.CompletedTask;
        }
    }
}
