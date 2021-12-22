// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Protocol;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.SignalR.Client.FunctionalTests;

public class VersionedJsonHubProtocol : IHubProtocol
{
    private readonly int _version;
    private readonly NewtonsoftJsonHubProtocol _innerProtocol;

    public VersionedJsonHubProtocol(int version)
    {
        _version = version;
        _innerProtocol = new NewtonsoftJsonHubProtocol();
    }

    public string Name => _innerProtocol.Name;
    public int Version => _version;
    public TransferFormat TransferFormat => _innerProtocol.TransferFormat;
    public int MinorVersion => 0; // not used in this test class, just for interface conformance

    public bool TryParseMessage(ref ReadOnlySequence<byte> input, IInvocationBinder binder, out HubMessage message)
    {
        var inputCopy = input;
        if (!TryParseMessage(ref input, out var payload))
        {
            message = null;
            return false;
        }

        // Handle "new" call
        var json = Encoding.UTF8.GetString(payload.ToArray());
        var o = JObject.Parse(json);
        if ((int)o["type"] == int.MaxValue)
        {
            message = new InvocationMessage("NewProtocolMethodServer", Array.Empty<object>());
            return true;
        }

        // Handle "old" calls
        var result = _innerProtocol.TryParseMessage(ref inputCopy, binder, out message);
        input = inputCopy;
        return result;
    }

    public static bool TryParseMessage(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> payload)
    {
        var position = buffer.PositionOf((byte)0x1e);
        if (position == null)
        {
            payload = default;
            return false;
        }

        payload = buffer.Slice(0, position.Value);

        // Skip record separator
        buffer = buffer.Slice(buffer.GetPosition(1, position.Value));

        return true;
    }

    public void WriteMessage(HubMessage message, IBufferWriter<byte> output)
    {
        _innerProtocol.WriteMessage(message, output);
    }

    public ReadOnlyMemory<byte> GetMessageBytes(HubMessage message)
    {
        return _innerProtocol.GetMessageBytes(message);
    }

    public bool IsVersionSupported(int version)
    {
        // Support older clients
        return version <= _version;
    }
}
