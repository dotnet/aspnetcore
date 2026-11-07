// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace Microsoft.AspNetCore.SignalR.Microbenchmarks;

// Invocation messages carrying headers, e.g. the W3C trace context headers the .NET client
// adds to every invocation when distributed tracing is enabled.
public class HubProtocolHeadersBenchmark
{
    private IHubProtocol _hubProtocol;
    private ReadOnlyMemory<byte> _binaryInput;
    private TestBinder _binder;
    private HubMessage _hubMessage;

    [Params(Protocol.MsgPack, Protocol.Json)]
    public Protocol HubProtocol { get; set; }

    [Params(2, 8)]
    public int HeaderCount { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _hubProtocol = HubProtocol switch
        {
            Protocol.MsgPack => new MessagePackHubProtocol(),
            Protocol.Json => new JsonHubProtocol(),
            _ => throw new InvalidOperationException()
        };

        var headers = new Dictionary<string, string>
        {
            ["traceparent"] = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01",
            ["tracestate"] = "congo=t61rcWkgMzE",
        };
        for (var i = headers.Count; i < HeaderCount; i++)
        {
            headers["header-" + i] = "value-" + i;
        }

        _hubMessage = new InvocationMessage("123", "Target", new object[] { 1, "Foo" }) { Headers = headers };
        _binaryInput = _hubProtocol.GetMessageBytes(_hubMessage);
        _binder = new TestBinder(_hubMessage);
    }

    [Benchmark]
    public void ReadMessage()
    {
        var data = new ReadOnlySequence<byte>(_binaryInput);
        if (!_hubProtocol.TryParseMessage(ref data, _binder, out _))
        {
            throw new InvalidOperationException("Failed to read message");
        }
    }

    [Benchmark]
    public void WriteMessage()
    {
        var writer = MemoryBufferWriter.Get();
        try
        {
            _hubProtocol.WriteMessage(_hubMessage, writer);
        }
        finally
        {
            MemoryBufferWriter.Return(writer);
        }
    }

    public enum Protocol
    {
        MsgPack,
        Json,
    }
}
