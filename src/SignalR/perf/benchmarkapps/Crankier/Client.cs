// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace Microsoft.AspNetCore.SignalR.Crankier
{
    public class Client
    {
        private readonly int _processId;
        private readonly IAgent _agent;
        private HubConnection _connection;
        private CancellationTokenSource _sendCts;
        private bool _sendInProgress;
        private volatile ConnectionState _connectionState = ConnectionState.Connecting;

        public ConnectionState State => _connectionState;
        public Client(int processId, IAgent agent)
        {
            _processId = processId;
            _agent = agent;
        }

        private void LogFault(string description, Exception exception)
        {
            var message = $"{description}: {exception.GetType()}: {exception.Message}";
            Trace.WriteLine(message);
            _agent.LogAsync(_processId, message);
        }

        public async Task CreateAndStartConnectionAsync(string url, HttpTransportType transportType)
        {
            _connection = new HubConnectionBuilder()
                .WithUrl(url, options => options.Transports = transportType)
                .Build();

            _connection.Closed += (ex) =>
            {
                if (ex == null)
                {
                    Trace.WriteLine("Connection terminated");
                    _connectionState = ConnectionState.Disconnected;
                }
                else
                {
                    LogFault("Connection terminated with error", ex);
                    _connectionState = ConnectionState.Faulted;
                }

                return Task.CompletedTask;
            };

            _sendCts = new CancellationTokenSource();

            await ConnectAsync();
        }

        private async Task ConnectAsync()
        {
            for (int connectCount = 0; connectCount <= 3; connectCount++)
            {
                try
                {
                    await _connection.StartAsync();
                    _connectionState = ConnectionState.Connected;
                    break;
                }
                catch (Exception ex)
                {
                    LogFault("Connection.Start Failed", ex);

                    if (connectCount == 3)
                    {
                        _connectionState = ConnectionState.Faulted;
                        throw;
                    }
                }

                await Task.Delay(1000);
            }
        }

        public void StartTest(int sendSize, TimeSpan sendInterval)
        {
            var payload = (sendSize == 0) ? String.Empty : new string('a', sendSize);

            if (_sendInProgress)
            {
                _sendCts.Cancel();
                _sendCts = new CancellationTokenSource();
            }
            else
            {
                _sendInProgress = true;
            }

            if (!String.IsNullOrEmpty(payload))
            {
                _ = Task.Run(async () =>
                {
                    while (!_sendCts.Token.IsCancellationRequested && State != ConnectionState.Disconnected)
                    {
                        try
                        {
                            await _connection.InvokeAsync("SendPayload", payload, _sendCts.Token);
                        }
                        // REVIEW: This is bad. We need a way to detect a closed connection when an Invocation fails!
                        catch (InvalidOperationException)
                        {
                            // The connection was closed.
                            Trace.WriteLine("Connection closed");
                            break;
                        }
                        catch (OperationCanceledException)
                        {
                            // The connection was closed.
                            Trace.WriteLine("Connection closed");
                            break;
                        }
                        catch (Exception ex)
                        {
                            // Connection failed
                            Trace.WriteLine($"Connection failed: {ex.GetType()}: {ex.Message}");
                            throw;
                        }

                        await Task.Delay(sendInterval);
                    }
                }, _sendCts.Token);
            }
        }

        public Task StopConnectionAsync()
        {
            _sendCts.Cancel();

            return _connection.StopAsync();
        }
    }
}
