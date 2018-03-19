// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Reactive.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Protocols;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.AspNetCore.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.SignalR.Microbenchmarks
{
    public class DefaultHubDispatcherBenchmark
    {
        private DefaultHubDispatcher<TestHub> _dispatcher;
        private HubConnectionContext _connectionContext;

        [GlobalSetup]
        public void GlobalSetup()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSignalRCore();

            var provider = serviceCollection.BuildServiceProvider();

            var serviceScopeFactory = provider.GetService<IServiceScopeFactory>();

            _dispatcher = new DefaultHubDispatcher<TestHub>(
                serviceScopeFactory,
                new HubContext<TestHub>(new DefaultHubLifetimeManager<TestHub>(NullLogger<DefaultHubLifetimeManager<TestHub>>.Instance)),
                new Logger<DefaultHubDispatcher<TestHub>>(NullLoggerFactory.Instance));

            var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);
            var connection = new Sockets.DefaultConnectionContext(Guid.NewGuid().ToString(), pair.Application, pair.Transport);

            _connectionContext = new NoErrorHubConnectionContext(connection, TimeSpan.Zero, NullLoggerFactory.Instance);

            _connectionContext.Protocol = new FakeHubProtocol();
        }

        public class FakeHubProtocol : IHubProtocol
        {
            public string Name { get; }
            public TransferFormat TransferFormat { get; }

            public bool TryParseMessages(ReadOnlyMemory<byte> input, IInvocationBinder binder, IList<HubMessage> messages)
            {
                return false;
            }

            public void WriteMessage(HubMessage message, Stream output)
            {
            }
        }

        public class NoErrorHubConnectionContext : HubConnectionContext
        {
            public NoErrorHubConnectionContext(ConnectionContext connectionContext, TimeSpan keepAliveInterval, ILoggerFactory loggerFactory) : base(connectionContext, keepAliveInterval, loggerFactory)
            {
            }

            public override Task WriteAsync(HubMessage message)
            {
                if (message is CompletionMessage completionMessage)
                {
                    if (!string.IsNullOrEmpty(completionMessage.Error))
                    {
                        throw new Exception("Error invoking hub method: " + completionMessage.Error);
                    }
                }

                return Task.CompletedTask;
            }
        }

        public class TestHub : Hub
        {
            private static readonly IObservable<int> ObservableInstance = Observable.Empty<int>();

            public void Invocation()
            {
            }

            public Task InvocationAsync()
            {
                return Task.CompletedTask;
            }

            public int InvocationReturnValue()
            {
                return 1;
            }

            public Task<int> InvocationReturnAsync()
            {
                return Task.FromResult(1);
            }

            public ValueTask<int> InvocationValueTaskAsync()
            {
                return new ValueTask<int>(1);
            }

            public IObservable<int> StreamObservable()
            {
                return ObservableInstance;
            }

            public Task<IObservable<int>> StreamObservableAsync()
            {
                return Task.FromResult(ObservableInstance);
            }

            public ValueTask<IObservable<int>> StreamObservableValueTaskAsync()
            {
                return new ValueTask<IObservable<int>>(ObservableInstance);
            }

            public ChannelReader<int> StreamChannelReader()
            {
                var channel = Channel.CreateUnbounded<int>();
                channel.Writer.Complete();

                return channel;
            }

            public Task<ChannelReader<int>> StreamChannelReaderAsync()
            {
                var channel = Channel.CreateUnbounded<int>();
                channel.Writer.Complete();

                return Task.FromResult<ChannelReader<int>>(channel);
            }

            public ValueTask<ChannelReader<int>> StreamChannelReaderValueTaskAsync()
            {
                var channel = Channel.CreateUnbounded<int>();
                channel.Writer.Complete();

                return new ValueTask<ChannelReader<int>>(channel);
            }
        }

        [Benchmark]
        public Task Invocation()
        {
            return _dispatcher.DispatchMessageAsync(_connectionContext, new InvocationMessage("123", "Invocation", null));
        }

        [Benchmark]
        public Task InvocationAsync()
        {
            return _dispatcher.DispatchMessageAsync(_connectionContext, new InvocationMessage("123", "InvocationAsync", null));
        }

        [Benchmark]
        public Task InvocationReturnValue()
        {
            return _dispatcher.DispatchMessageAsync(_connectionContext, new InvocationMessage("123", "InvocationReturnValue", null));
        }

        [Benchmark]
        public Task InvocationReturnAsync()
        {
            return _dispatcher.DispatchMessageAsync(_connectionContext, new InvocationMessage("123", "InvocationReturnAsync", null));
        }

        [Benchmark]
        public Task InvocationValueTaskAsync()
        {
            return _dispatcher.DispatchMessageAsync(_connectionContext, new InvocationMessage("123", "InvocationValueTaskAsync", null));
        }

        [Benchmark]
        public Task StreamObservable()
        {
            return _dispatcher.DispatchMessageAsync(_connectionContext, new StreamInvocationMessage("123", "StreamObservable", null));
        }

        [Benchmark]
        public Task StreamObservableAsync()
        {
            return _dispatcher.DispatchMessageAsync(_connectionContext, new StreamInvocationMessage("123", "StreamObservableAsync", null));
        }

        [Benchmark]
        public Task StreamObservableValueTaskAsync()
        {
            return _dispatcher.DispatchMessageAsync(_connectionContext, new StreamInvocationMessage("123", "StreamObservableValueTaskAsync", null));
        }

        [Benchmark]
        public Task StreamChannelReader()
        {
            return _dispatcher.DispatchMessageAsync(_connectionContext, new StreamInvocationMessage("123", "StreamChannelReader", null));
        }

        [Benchmark]
        public Task StreamChannelReaderAsync()
        {
            return _dispatcher.DispatchMessageAsync(_connectionContext, new StreamInvocationMessage("123", "StreamChannelReaderAsync", null));
        }

        [Benchmark]
        public Task StreamChannelReaderValueTaskAsync()
        {
            return _dispatcher.DispatchMessageAsync(_connectionContext, new StreamInvocationMessage("123", "StreamChannelReaderValueTaskAsync", null));
        }
    }
}
