// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace Microsoft.AspNetCore.SignalR.Tests;

public class DummyHubProtocol : IHubProtocol
{
    private readonly Action _onWrite;
    private readonly object _lock = new object();
    private readonly List<HubMessage> _writtenMessages = new List<HubMessage>();

    public static readonly byte[] DummySerialization = new byte[] { 0x2A };

    public string Name { get; }
    public int Version => 1;
    public int MinorVersion => 0;
    public TransferFormat TransferFormat => TransferFormat.Text;

    public DummyHubProtocol(string name, Action onWrite = null)
    {
        _onWrite = onWrite ?? (() => { });
        Name = name;
    }

    public IReadOnlyList<HubMessage> GetWrittenMessages()
    {
        lock (_lock)
        {
            return _writtenMessages.ToArray();
        }
    }

    public bool TryParseMessage(ref ReadOnlySequence<byte> input, IInvocationBinder binder, out HubMessage message)
    {
        throw new NotSupportedException();
    }

    public void WriteMessage(HubMessage message, IBufferWriter<byte> output)
    {
        output.Write(GetMessageBytes(message).Span);
    }

    public ReadOnlyMemory<byte> GetMessageBytes(HubMessage message)
    {
        _onWrite();
        lock (_lock)
        {
            _writtenMessages.Add(message);
        }

        return DummySerialization;
    }

    public bool IsVersionSupported(int version)
    {
        throw new NotSupportedException();
    }
}
