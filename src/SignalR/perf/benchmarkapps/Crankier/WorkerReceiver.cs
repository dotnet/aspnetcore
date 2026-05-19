// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Connections;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.SignalR.Crankier
{
    public class WorkerReceiver
    {
        private readonly StreamReader _reader;
        private readonly IWorker _worker;
        private CancellationTokenSource _receiveMessageCts;

        public WorkerReceiver(StreamReader reader, IWorker worker)
        {
            _reader = reader;
            _worker = worker;
        }

        public void Start()
        {
            if (_receiveMessageCts != null)
            {
                _receiveMessageCts.Cancel();
            }

            _receiveMessageCts = new CancellationTokenSource();
            Task.Run(async () =>
            {
                while (!_receiveMessageCts.Token.IsCancellationRequested)
                {
                    var messageString = await _reader.ReadLineAsync();
                    try
                    {
                        var message = JsonConvert.DeserializeObject<Message>(messageString);

                        switch (message.Command.ToLowerInvariant())
                        {
                            case "ping":
                                await _worker.PingAsync(
                                    message.Value["Value"].ToObject<int>());
                                break;
                            case "connect":
                                await _worker.ConnectAsync(
                                    message.Value["TargetAddress"].ToObject<string>(),
                                    message.Value["TransportType"].ToObject<HttpTransportType>(),
                                    message.Value["NumberOfConnections"].ToObject<int>());
                                break;
                            case "starttest":
                                await _worker.StartTestAsync(
                                    TimeSpan.FromMilliseconds(message.Value.Value<double>("SendInterval")),
                                    message.Value["SendBytes"].ToObject<int>());
                                break;
                            case "stop":
                                await _worker.StopAsync();
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(ex.Message);
                    }
                }
            });
        }

        public void Stop()
        {
            _receiveMessageCts.Cancel();
        }
    }
}
