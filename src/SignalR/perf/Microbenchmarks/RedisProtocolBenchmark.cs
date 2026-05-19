// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.AspNetCore.SignalR.StackExchangeRedis.Internal;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.SignalR.Microbenchmarks;

public class RedisProtocolBenchmark
{
    private RedisProtocol _protocol;
    private RedisGroupCommand _groupCommand;
    private object[] _args;
    private string _methodName;
    private IReadOnlyList<string> _excludedConnectionIdsSmall;
    private IReadOnlyList<string> _excludedConnectionIdsLarge;
    private byte[] _writtenAck;
    private byte[] _writtenGroupCommand;
    private byte[] _writtenInvocationNoExclusions;
    private byte[] _writtenInvocationSmallExclusions;
    private byte[] _writtenInvocationLargeExclusions;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var resolver = new DefaultHubProtocolResolver(new List<IHubProtocol> { new DummyProtocol("protocol1"),
                new DummyProtocol("protocol2") }, NullLogger<DefaultHubProtocolResolver>.Instance);

        _protocol = new RedisProtocol(new DefaultHubMessageSerializer(resolver, new List<string>() { "protocol1", "protocol2" }, hubSupportedProtocols: null));

        _groupCommand = new RedisGroupCommand(id: 42, serverName: "Server", GroupAction.Add, groupName: "group", connectionId: "connection");

        // Because of the DummyProtocol, the args don't really matter
        _args = Array.Empty<object>();
        _methodName = "Method";

        _excludedConnectionIdsSmall = GenerateIds(2);
        _excludedConnectionIdsLarge = GenerateIds(20);

        _writtenAck = RedisProtocol.WriteAck(42);
        _writtenGroupCommand = RedisProtocol.WriteGroupCommand(_groupCommand);
        _writtenInvocationNoExclusions = _protocol.WriteInvocation(_methodName, _args, null);
        _writtenInvocationSmallExclusions = _protocol.WriteInvocation(_methodName, _args, excludedConnectionIds: _excludedConnectionIdsSmall);
        _writtenInvocationLargeExclusions = _protocol.WriteInvocation(_methodName, _args, excludedConnectionIds: _excludedConnectionIdsLarge);
    }

    [Benchmark]
    public void WriteAck()
    {
        RedisProtocol.WriteAck(42);
    }

    [Benchmark]
    public void WriteGroupCommand()
    {
        RedisProtocol.WriteGroupCommand(_groupCommand);
    }

    [Benchmark]
    public void WriteInvocationNoExclusions()
    {
        _protocol.WriteInvocation(_methodName, _args);
    }

    [Benchmark]
    public void WriteInvocationSmallExclusions()
    {
        _protocol.WriteInvocation(_methodName, _args, excludedConnectionIds: _excludedConnectionIdsSmall);
    }

    [Benchmark]
    public void WriteInvocationLargeExclusions()
    {
        _protocol.WriteInvocation(_methodName, _args, excludedConnectionIds: _excludedConnectionIdsLarge);
    }

    [Benchmark]
    public void ReadAck()
    {
        RedisProtocol.ReadAck(_writtenAck);
    }

    [Benchmark]
    public void ReadGroupCommand()
    {
        RedisProtocol.ReadGroupCommand(_writtenGroupCommand);
    }

    [Benchmark]
    public void ReadInvocationNoExclusions()
    {
        RedisProtocol.ReadInvocation(_writtenInvocationNoExclusions);
    }

    [Benchmark]
    public void ReadInvocationSmallExclusions()
    {
        RedisProtocol.ReadInvocation(_writtenInvocationSmallExclusions);
    }

    [Benchmark]
    public void ReadInvocationLargeExclusions()
    {
        RedisProtocol.ReadInvocation(_writtenInvocationLargeExclusions);
    }

    private static IReadOnlyList<string> GenerateIds(int count)
    {
        var ids = new string[count];
        for (var i = 0; i < count; i++)
        {
            ids[i] = Guid.NewGuid().ToString("N");
        }
        return ids;
    }

    private sealed class DummyProtocol : IHubProtocol
    {
        private static readonly byte[] _fixedOutput = new byte[] { 0x68, 0x68, 0x6C, 0x6C, 0x6F };

        public string Name { get; }

        public int Version => 1;
        public int MinorVersion => 0;

        public TransferFormat TransferFormat => TransferFormat.Text;

        public DummyProtocol(string name)
        {
            Name = name;
        }

        public bool IsVersionSupported(int version) => true;

        public bool TryParseMessage(ref ReadOnlySequence<byte> input, IInvocationBinder binder, out HubMessage message)
        {
            throw new NotSupportedException();
        }

        public void WriteMessage(HubMessage message, IBufferWriter<byte> output)
        {
            output.Write(_fixedOutput);
        }

        public ReadOnlyMemory<byte> GetMessageBytes(HubMessage message)
        {
            return HubProtocolExtensions.GetMessageBytes(this, message);
        }
    }
}
