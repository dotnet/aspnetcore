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
using Microsoft.AspNetCore.SignalR.Microbenchmarks.Shared;
using Microsoft.AspNetCore.SignalR.Tests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace Microsoft.AspNetCore.SignalR.Microbenchmarks
{
    public class HubConnectionSendBenchmark
    {
        private HubConnection _hubConnection;
        private TestDuplexPipe _pipe;
        private TaskCompletionSource<ReadResult> _tcs;
        private object[] _arguments;

        [GlobalSetup]
        public void GlobalSetup()
        {
            var writer = MemoryBufferWriter.Get();
            try
            {
                HandshakeProtocol.WriteResponseMessage(HandshakeResponseMessage.Empty, writer);
                var handshakeResponseResult = new ReadResult(new ReadOnlySequence<byte>(writer.ToArray()), false, false);

                _pipe = new TestDuplexPipe();
                _pipe.AddReadResult(new ValueTask<ReadResult>(handshakeResponseResult));
            }
            finally
            {
                MemoryBufferWriter.Return(writer);
            }

            _tcs = new TaskCompletionSource<ReadResult>();
            _pipe.AddReadResult(new ValueTask<ReadResult>(_tcs.Task));

            var hubConnectionBuilder = new HubConnectionBuilder();
            if (Protocol == "json")
            {
                // JSON protocol added by default
            }
            else
            {
                hubConnectionBuilder.AddMessagePackProtocol();
            }

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
            _hubConnection.StartAsync().GetAwaiter().GetResult();

            _arguments = new object[ArgumentCount];
            for (var i = 0; i < _arguments.Length; i++)
            {
                _arguments[i] = "Hello world!";
            }
        }

        [Params(0, 1, 10, 100)]
        public int ArgumentCount;

        [Params("json", "messagepack")]
        public string Protocol;

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            _tcs.SetResult(new ReadResult(default, false, true));
            _hubConnection.StopAsync().GetAwaiter().GetResult();
        }

        [Benchmark]
        public Task SendAsync()
        {
            return _hubConnection.SendCoreAsync("Dummy", _arguments);
        }
    }
}
