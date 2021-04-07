// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace Microsoft.AspNetCore.SignalR.Internal
{
    internal class DefaultHubMessageSerializer
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
}
