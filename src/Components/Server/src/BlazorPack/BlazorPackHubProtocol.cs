// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace Microsoft.AspNetCore.Components.Server.BlazorPack;

/// <summary>
/// Implements the SignalR Hub Protocol using MessagePack with limited type support.
/// </summary>
[NonDefaultHubProtocol]
internal sealed class BlazorPackHubProtocol : IHubProtocol
{
    internal const string ProtocolName = "blazorpack";
    private const int ProtocolVersion = 2;

    private readonly BlazorPackHubProtocolWorker _worker = new BlazorPackHubProtocolWorker();

    /// <inheritdoc />
    public string Name => ProtocolName;

    /// <inheritdoc />
    public int Version => ProtocolVersion;

    /// <inheritdoc />
    public TransferFormat TransferFormat => TransferFormat.Binary;

    /// <inheritdoc />
    public bool IsVersionSupported(int version)
    {
        return version <= Version;
    }

    /// <inheritdoc />
    public bool TryParseMessage(ref ReadOnlySequence<byte> input, IInvocationBinder binder, out HubMessage message)
        => _worker.TryParseMessage(ref input, binder, out message);

    /// <inheritdoc />
    public void WriteMessage(HubMessage message, IBufferWriter<byte> output)
        => _worker.WriteMessage(message, output);

    /// <inheritdoc />
    public ReadOnlyMemory<byte> GetMessageBytes(HubMessage message)
        => _worker.GetMessageBytes(message);
}
