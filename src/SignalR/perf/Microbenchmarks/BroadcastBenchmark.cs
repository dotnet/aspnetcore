// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.SignalR.Microbenchmarks
{
    public class BroadcastBenchmark
    {
        private const string TestGroupName = "TestGroup";
        private DefaultHubLifetimeManager<Hub> _hubLifetimeManager;
        private HubContext<Hub> _hubContext;

        [Params(1, 10, 1000)]
        public int Connections;

        [Params("json", "msgpack")]
        public string Protocol;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _hubLifetimeManager = new DefaultHubLifetimeManager<Hub>(NullLogger<DefaultHubLifetimeManager<Hub>>.Instance);

            IHubProtocol protocol;

            if (Protocol == "json")
            {
                protocol = new NewtonsoftJsonHubProtocol();
            }
            else
            {
                protocol = new MessagePackHubProtocol();
            }

            var options = new PipeOptions();
            for (var i = 0; i < Connections; ++i)
            {
                var pair = DuplexPipe.CreateConnectionPair(options, options);
                var connection = new DefaultConnectionContext(Guid.NewGuid().ToString(), pair.Application, pair.Transport);
                var contextOptions = new HubConnectionContextOptions()
                {
                    KeepAliveInterval = Timeout.InfiniteTimeSpan,
                };
                var hubConnection = new HubConnectionContext(connection, contextOptions, NullLoggerFactory.Instance);
                hubConnection.Protocol = protocol;
                _hubLifetimeManager.OnConnectedAsync(hubConnection).GetAwaiter().GetResult();
                _hubLifetimeManager.AddToGroupAsync(connection.ConnectionId, TestGroupName).GetAwaiter().GetResult();

                _ = ConsumeAsync(connection.Application);
            }

            _hubContext = new HubContext<Hub>(_hubLifetimeManager);
        }

        [Benchmark]
        public Task SendAsyncGroup()
        {
            return _hubContext.Clients.Group(TestGroupName).SendAsync("Method");
        }

        [Benchmark]
        public Task SendAsyncAll()
        {
            return _hubContext.Clients.All.SendAsync("Method");
        }

        // Consume the data written to the transport
        private static async Task ConsumeAsync(IDuplexPipe application)
        {
            while (true)
            {
                var result = await application.Input.ReadAsync();
                var buffer = result.Buffer;

                if (!buffer.IsEmpty)
                {
                    application.Input.AdvanceTo(buffer.End);
                }
                else if (result.IsCompleted)
                {
                    break;
                }
            }

            application.Input.Complete();
        }
    }
}
