// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.AspNetCore.SignalR.Microbenchmarks.Shared;
using Microsoft.AspNetCore.SignalR.Tests;
using Microsoft.Extensions.DependencyInjection;

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
            var writer = MemoryBufferWriter.Get();
            try
            {
                HandshakeProtocol.WriteResponseMessage(HandshakeResponseMessage.Empty, writer);
                _handshakeResponseResult = new ReadResult(new ReadOnlySequence<byte>(writer.ToArray()), false, false);
            }
            finally
            {
                MemoryBufferWriter.Return(writer);
            }

            _pipe = new TestDuplexPipe();

            var hubConnectionBuilder = new HubConnectionBuilder();
            var delegateConnectionFactory = new DelegateConnectionFactory(endPoint =>
            {
                var connection = new DefaultConnectionContext();
                // prevents keep alive time being activated
                connection.Features.Set<IConnectionInherentKeepAliveFeature>(new TestConnectionInherentKeepAliveFeature());
                connection.Transport = _pipe;
                return new ValueTask<ConnectionContext>(connection);
            });
            hubConnectionBuilder.Services.AddSingleton<IConnectionFactory>(delegateConnectionFactory);

            _hubConnection = hubConnectionBuilder.Build();
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
        public bool HasInherentKeepAlive { get; } = true;
    }
}
