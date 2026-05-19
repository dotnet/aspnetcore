// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace Microsoft.AspNetCore.SignalR.Internal;

internal sealed class DefaultHubMessageSerializer
{
    private readonly List<IHubProtocol> _hubProtocols;

    public DefaultHubMessageSerializer(IHubProtocolResolver hubProtocolResolver, IList<string>? globalSupportedProtocols, IList<string>? hubSupportedProtocols)
    {
        var supportedProtocols = hubSupportedProtocols ?? globalSupportedProtocols ?? Array.Empty<string>();
        _hubProtocols = new List<IHubProtocol>(supportedProtocols.Count);
        foreach (var protocolName in supportedProtocols)
        {
            var protocol = hubProtocolResolver.GetProtocol(protocolName, (supportedProtocols as IReadOnlyList<string>) ?? supportedProtocols.ToList());
            if (protocol != null)
            {
                _hubProtocols.Add(protocol);
            }
        }
    }

    public IReadOnlyList<SerializedMessage> SerializeMessage(HubMessage message)
    {
        var list = new List<SerializedMessage>(_hubProtocols.Count);
        foreach (var protocol in _hubProtocols)
        {
            list.Add(new SerializedMessage(protocol.Name, protocol.GetMessageBytes(message)));
        }

        return list;
    }
}
