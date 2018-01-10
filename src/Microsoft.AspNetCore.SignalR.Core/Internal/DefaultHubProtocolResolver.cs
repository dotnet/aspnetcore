// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.SignalR.Internal
{
    public class DefaultHubProtocolResolver : IHubProtocolResolver
    {
        private readonly IOptions<HubOptions> _options;
        private readonly ILogger<DefaultHubProtocolResolver> _logger;
        private readonly Dictionary<string, IHubProtocol> _availableProtocols;

        public DefaultHubProtocolResolver(IOptions<HubOptions> options, IEnumerable<IHubProtocol> availableProtocols, ILogger<DefaultHubProtocolResolver> logger)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? NullLogger<DefaultHubProtocolResolver>.Instance;
            _availableProtocols = new Dictionary<string, IHubProtocol>(StringComparer.OrdinalIgnoreCase);

            foreach(var protocol in availableProtocols)
            {
                if(_availableProtocols.ContainsKey(protocol.Name))
                {
                    throw new InvalidOperationException($"Multiple Hub Protocols with the name '{protocol.Name}' were registered.");
                }
                _logger.RegisteredSignalRProtocol(protocol.Name, protocol.GetType());
                _availableProtocols.Add(protocol.Name, protocol);
            }
        }

        public IHubProtocol GetProtocol(string protocolName, HubConnectionContext connection)
        {
            protocolName = protocolName ?? throw new ArgumentNullException(nameof(protocolName));

            if (_availableProtocols.TryGetValue(protocolName, out var protocol))
            {
                _logger.FoundImplementationForProtocol(protocolName);
                return protocol;
            }

            throw new NotSupportedException($"The protocol '{protocolName ?? "(null)"}' is not supported.");
        }
    }
}
