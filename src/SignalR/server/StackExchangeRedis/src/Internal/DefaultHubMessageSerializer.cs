// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.SignalR.Internal
{
    internal class DefaultHubMessageSerializer<THub> where THub : Hub
    {
        private readonly List<IHubProtocol> _hubProtocols = new List<IHubProtocol>();

        public DefaultHubMessageSerializer(IHubProtocolResolver hubProtocolResolver, IOptions<HubOptions> globalHubOptions, IOptions<HubOptions<THub>> hubOptions)
        {
            var supportedProtocols = hubOptions.Value.SupportedProtocols ?? globalHubOptions.Value.SupportedProtocols ?? Array.Empty<string>();
            foreach (var protocolName in supportedProtocols)
            {
                var protocol = hubProtocolResolver.GetProtocol(protocolName, (supportedProtocols as IReadOnlyList<string>) ?? supportedProtocols.ToList());
                if (protocol != null)
                {
                    _hubProtocols.Add(protocol);
                }
            }
        }

        public SerializedHubMessage SerializeMessage(HubMessage message)
        {
            var list = new List<SerializedMessage>(_hubProtocols.Count);
            foreach (var protocol in _hubProtocols)
            {
                list.Add(new SerializedMessage(protocol.Name, protocol.GetMessageBytes(message)));
            }

            return new SerializedHubMessage(list);
        }
    }
}
