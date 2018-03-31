// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.AspNetCore.SignalR.Microbenchmarks.Shared;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.SignalR.Microbenchmarks
{
    public class HubConnectionStartBenchmark
    {
        private HubConnection _hubConnection;
        private TestDuplexPipe _pipe;
        private ReadResult _handshakeResponseResult;

        [GlobalSetup]
        public void GlobalSetup()
        {
            using (var writer = new MemoryBufferWriter())
            {
                HandshakeProtocol.WriteResponseMessage(HandshakeResponseMessage.Empty, writer);
                _handshakeResponseResult = new ReadResult(new ReadOnlySequence<byte>(writer.ToArray()), false, false);
            }

            _pipe = new TestDuplexPipe();

            var connection = new TestConnection();
            // prevents keep alive time being activated
            connection.Features.Set<IConnectionInherentKeepAliveFeature>(new TestConnectionInherentKeepAliveFeature());
            connection.Transport = _pipe;

            _hubConnection = new HubConnection(() => connection, new JsonHubProtocol(), new NullLoggerFactory());
        }

        private void AddHandshakeResponse()
        {
            _pipe.AddReadResult(new ValueTask<ReadResult>(_handshakeResponseResult));
        }

        [Benchmark]
        public async Task StartAsync()
        {
            AddHandshakeResponse();

            await _hubConnection.StartAsync();
            await _hubConnection.StopAsync();
        }
    }

    public class TestConnectionInherentKeepAliveFeature : IConnectionInherentKeepAliveFeature
    {
        public TimeSpan KeepAliveInterval { get; } = TimeSpan.Zero;
    }

    public class TestConnection : IConnection
    {
        public Task StartAsync()
        {
            throw new NotImplementedException();
        }

        public Task StartAsync(TransferFormat transferFormat)
        {
            return Task.CompletedTask;
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        public IDuplexPipe Transport { get; set; }

        public IFeatureCollection Features { get; } = new FeatureCollection();
    }
}
