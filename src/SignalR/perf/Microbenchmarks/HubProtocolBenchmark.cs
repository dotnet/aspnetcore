// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace Microsoft.AspNetCore.SignalR.Microbenchmarks;

public class HubProtocolBenchmark
{
    private IHubProtocol _hubProtocol;
    private ReadOnlyMemory<byte> _binaryInput;
    private TestBinder _binder;
    private HubMessage _hubMessage;

    [Params(Message.NoArguments, Message.FewArguments, Message.ManyArguments, Message.LargeArguments)]
    public Message Input { get; set; }

    [Params(Protocol.MsgPack, Protocol.Json, Protocol.NewtonsoftJson)]
    public Protocol HubProtocol { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        switch (HubProtocol)
        {
            case Protocol.MsgPack:
                _hubProtocol = new MessagePackHubProtocol();
                break;
            case Protocol.Json:
                _hubProtocol = new JsonHubProtocol();
                break;
            case Protocol.NewtonsoftJson:
                _hubProtocol = new NewtonsoftJsonHubProtocol();
                break;
        }

        switch (Input)
        {
            case Message.NoArguments:
                _hubMessage = new InvocationMessage("Target", Array.Empty<object>());
                break;
            case Message.FewArguments:
                _hubMessage = new InvocationMessage("Target", new object[] { 1, "Foo", 2.0f });
                break;
            case Message.ManyArguments:
                _hubMessage = new InvocationMessage("Target", new object[] { 1, "string", 2.0f, true, (byte)9, new byte[] { 5, 4, 3, 2, 1 }, 'c', 123456789101112L });
                break;
            case Message.LargeArguments:
                _hubMessage = new InvocationMessage("Target", new object[] { new string('F', 10240), new byte[10240] });
                break;
        }

        _binaryInput = _hubProtocol.GetMessageBytes(_hubMessage);
        _binder = new TestBinder(_hubMessage);
    }

    [Benchmark]
    public void ReadSingleMessage()
    {
        var data = new ReadOnlySequence<byte>(_binaryInput);
        if (!_hubProtocol.TryParseMessage(ref data, _binder, out _))
        {
            throw new InvalidOperationException("Failed to read message");
        }
    }

    [Benchmark]
    public void WriteSingleMessage()
    {
        var bytes = _hubProtocol.GetMessageBytes(_hubMessage);
        if (bytes.Length != _binaryInput.Length)
        {
            throw new InvalidOperationException("Failed to write message");
        }
    }

    public enum Protocol
    {
        MsgPack = 0,
        Json = 1,
        NewtonsoftJson = 2,
    }

    public enum Message
    {
        NoArguments = 0,
        FewArguments = 1,
        ManyArguments = 2,
        LargeArguments = 3
    }
}
