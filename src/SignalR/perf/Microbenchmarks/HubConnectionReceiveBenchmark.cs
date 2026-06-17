// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Pipelines;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.SignalR.Microbenchmarks.Shared;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.AspNetCore.SignalR.Tests;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.AspNetCore.SignalR.Microbenchmarks;

public class HubConnectionReceiveBenchmark
{
    private const string MethodName = "TestMethodName";
    private static readonly object _invokeLock = new object();

    private HubConnection _hubConnection;
    private TestDuplexPipe _pipe;
    private ReadOnlyMemory<byte> _invocationMessageBytes;

    private int _currentInterationMessageCount;
    private TaskCompletionSource<ReadResult> _tcs;
    private TaskCompletionSource<ReadResult> _nextReadTcs;
    private TaskCompletionSource _waitTcs;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var arguments = new object[ArgumentCount];
        for (var i = 0; i < arguments.Length; i++)
        {
            arguments[i] = "Hello world!";
        }

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

        _nextReadTcs = new TaskCompletionSource<ReadResult>();
        _pipe.AddReadResult(new ValueTask<ReadResult>(_nextReadTcs.Task));

        IHubProtocol hubProtocol;

        var hubConnectionBuilder = new HubConnectionBuilder();
        if (Protocol == "json")
        {
            hubProtocol = new NewtonsoftJsonHubProtocol();
        }
        else
        {
            hubProtocol = new MessagePackHubProtocol();
        }

        hubConnectionBuilder.Services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IHubProtocol), hubProtocol));
        hubConnectionBuilder.WithUrl("http://doesntmatter");

        _invocationMessageBytes = hubProtocol.GetMessageBytes(new InvocationMessage(MethodName, arguments));

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
        _hubConnection.On(MethodName, arguments.Select(v => v.GetType()).ToArray(), OnInvoke);
        _hubConnection.StartAsync().GetAwaiter().GetResult();
    }

    private Task OnInvoke(object[] args)
    {
        // HubConnection now runs this callback serially but just in case
        // add a lock in case of future experimentation
        lock (_invokeLock)
        {
            _currentInterationMessageCount++;

            if (_currentInterationMessageCount == MessageCount)
            {
                _currentInterationMessageCount = 0;
                _waitTcs.SetResult();
            }
            else if (_currentInterationMessageCount > MessageCount)
            {
                throw new InvalidOperationException("Should never happen.");
            }
        }

        return Task.CompletedTask;
    }

    [Params(0, 1, 10, 100)]
    public int ArgumentCount;

    [Params(1, 100)]
    public int MessageCount;

    [Params("json", "messagepack")]
    public string Protocol;

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _nextReadTcs.SetResult(new ReadResult(default, false, true));
        _hubConnection.StopAsync().GetAwaiter().GetResult();
    }

    public void OperationSetup()
    {
        _tcs = _nextReadTcs;

        // Add the results for additional messages (minus 1 because 1 result has already been added)
        for (int i = 0; i < MessageCount - 1; i++)
        {
            _pipe.AddReadResult(new ValueTask<ReadResult>(new ReadResult(new ReadOnlySequence<byte>(_invocationMessageBytes), false, false)));
        }

        // The receive task that will be waited on once messages are read
        _nextReadTcs = new TaskCompletionSource<ReadResult>();
        _pipe.AddReadResult(new ValueTask<ReadResult>(_nextReadTcs.Task));

        _waitTcs = new TaskCompletionSource();
    }

    [Benchmark]
    public async Task ReceiveAsync()
    {
        // Setup messages
        OperationSetup();

        // Start receive of the next batch of messages
        _tcs.SetResult(new ReadResult(new ReadOnlySequence<byte>(_invocationMessageBytes), false, false));

        // Wait for all messages to be read and invoked
        await _waitTcs.Task.DefaultTimeout();
    }
}
