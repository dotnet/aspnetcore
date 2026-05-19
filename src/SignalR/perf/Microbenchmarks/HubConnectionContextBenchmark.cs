// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Pipelines;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.SignalR.Microbenchmarks.Shared;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.SignalR.Microbenchmarks;

public class HubConnectionContextBenchmark
{
    private HubConnectionContext _hubConnectionContext;
    private TestHubProtocolResolver _successHubProtocolResolver;
    private TestHubProtocolResolver _failureHubProtocolResolver;
    private TestUserIdProvider _userIdProvider;
    private List<string> _supportedProtocols;
    private TestDuplexPipe _pipe;
    private ReadResult _handshakeRequestResult;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var memoryBufferWriter = MemoryBufferWriter.Get();
        try
        {
            HandshakeProtocol.WriteRequestMessage(new HandshakeRequestMessage("json", 1), memoryBufferWriter);
            _handshakeRequestResult = new ReadResult(new ReadOnlySequence<byte>(memoryBufferWriter.ToArray()), false, false);
        }
        finally
        {
            MemoryBufferWriter.Return(memoryBufferWriter);
        }

        _pipe = new TestDuplexPipe();

        var connection = new DefaultConnectionContext(Guid.NewGuid().ToString(), _pipe, _pipe);
        var contextOptions = new HubConnectionContextOptions()
        {
            KeepAliveInterval = Timeout.InfiniteTimeSpan,
        };
        _hubConnectionContext = new HubConnectionContext(connection, contextOptions, NullLoggerFactory.Instance);

        _successHubProtocolResolver = new TestHubProtocolResolver(new NewtonsoftJsonHubProtocol());
        _failureHubProtocolResolver = new TestHubProtocolResolver(null);
        _userIdProvider = new TestUserIdProvider();
        _supportedProtocols = new List<string> { "json" };
    }

    [Benchmark]
    public async Task SuccessHandshakeAsync()
    {
        _pipe.AddReadResult(new ValueTask<ReadResult>(_handshakeRequestResult));

        await _hubConnectionContext.HandshakeAsync(TimeSpan.FromSeconds(5), _supportedProtocols, _successHubProtocolResolver,
            _userIdProvider, enableDetailedErrors: true);
    }

    [Benchmark]
    public async Task ErrorHandshakeAsync()
    {
        _pipe.AddReadResult(new ValueTask<ReadResult>(_handshakeRequestResult));

        await _hubConnectionContext.HandshakeAsync(TimeSpan.FromSeconds(5), _supportedProtocols, _failureHubProtocolResolver,
            _userIdProvider, enableDetailedErrors: true);
    }
}

public class TestUserIdProvider : IUserIdProvider
{
    public string GetUserId(HubConnectionContext connection)
    {
        return "UserId!";
    }
}

public class TestHubProtocolResolver : IHubProtocolResolver
{
    private readonly IHubProtocol _instance;

    public IReadOnlyList<IHubProtocol> AllProtocols { get; }

    public TestHubProtocolResolver(IHubProtocol instance)
    {
        AllProtocols = new[] { instance };
        _instance = instance;
    }

    public IHubProtocol GetProtocol(string protocolName, IReadOnlyList<string> supportedProtocols)
    {
        return _instance;
    }
}
