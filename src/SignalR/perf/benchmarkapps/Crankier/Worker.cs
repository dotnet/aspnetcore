// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Connections;

namespace Microsoft.AspNetCore.SignalR.Crankier
{
    public class Worker : IWorker
    {
        private readonly Process _agentProcess;
        private readonly IAgent _agent;
        private readonly int _processId;
        private readonly ConcurrentBag<Client> _clients;
        private readonly CancellationTokenSource _sendStatusCts;
        private int _targetConnectionCount;

        public Worker(int agentProcessId)
        {
            _agentProcess = Process.GetProcessById(agentProcessId);
            _agent = new AgentSender(new StreamWriter(Console.OpenStandardOutput()));
            _processId = Environment.ProcessId;
            _clients = new ConcurrentBag<Client>();
            _sendStatusCts = new CancellationTokenSource();
        }

        public async Task RunAsync()
        {
            _agentProcess.EnableRaisingEvents = true;
            _agentProcess.Exited += OnExited;

            Log("Worker created");

            var receiver = new WorkerReceiver(
                new StreamReader(Console.OpenStandardInput()),
                this);

            receiver.Start();

            await SendStatusUpdateAsync(_sendStatusCts.Token);

            receiver.Stop();
        }

        public async Task PingAsync(int value)
        {
            Log("Worker received ping command with value {0}.", value);

            await _agent.PongAsync(_processId, value);
            Log("Worker sent pong command with value {0}.", value);
        }

        public async Task ConnectAsync(string targetAddress, HttpTransportType transportType, int numberOfConnections)
        {
            Log("Worker received connect command with target address {0} and number of connections {1}", targetAddress, numberOfConnections);

            _targetConnectionCount += numberOfConnections;
            for (var count = 0; count < numberOfConnections; count++)
            {
                var client = new Client(_processId, _agent);
                _clients.Add(client);

                await client.CreateAndStartConnectionAsync(targetAddress, transportType);
            }

            Log("Connections connected successfully");
        }

        public Task StartTestAsync(TimeSpan sendInterval, int sendBytes)
        {
            Log("Worker received start test command with interval {0} and message size {1}.", sendInterval, sendBytes);

            foreach (var client in _clients)
            {
                client.StartTest(sendBytes, sendInterval);
            }

            Log("Test started successfully");
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            Log("Worker received stop command");
            _targetConnectionCount = 0;

            while (!_clients.IsEmpty)
            {
                if (_clients.TryTake(out var client))
                {
                    client.StopConnectionAsync();
                }
            }

            _sendStatusCts.Cancel();
            Log("Connections stopped successfully");
            _targetConnectionCount = 0;

            return Task.CompletedTask;
        }

        private void OnExited(object sender, EventArgs args)
        {
            Environment.Exit(0);
        }

        private void Log(string format, params object[] arguments)
        {
            _agent.LogAsync(_processId, string.Format(format, arguments));
        }

        private async Task SendStatusUpdateAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var connectedCount = 0;
                var connectingCount = 0;
                var disconnectedCount = 0;
                var reconnectingCount = 0;
                var faultedCount = 0;

                foreach (var client in _clients)
                {
                    switch (client.State)
                    {
                        case ConnectionState.Connecting:
                            connectingCount++;
                            break;
                        case ConnectionState.Connected:
                            connectedCount++;
                            break;
                        case ConnectionState.Disconnected:
                            disconnectedCount++;
                            break;
                        case ConnectionState.Reconnecting:
                            reconnectingCount++;
                            break;
                        case ConnectionState.Faulted:
                            faultedCount++;
                            break;
                    }
                }

                await _agent.StatusAsync(
                    _processId,
                    new StatusInformation
                    {
                        ConnectingCount = connectingCount,
                        ConnectedCount = connectedCount,
                        DisconnectedCount = disconnectedCount,
                        ReconnectingCount = reconnectingCount,
                        TargetConnectionCount = _targetConnectionCount,
                        FaultedCount = faultedCount,
                    }
                );

                // Sending once per 5 seconds to avoid overloading the Test Controller
                try
                {
                    await Task.Delay(5000, cancellationToken);
                }
                catch (TaskCanceledException)
                {
                }
            }
        }
    }
}
