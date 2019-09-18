// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.SignalR.Internal
{
    internal class DefaultHubProtocolResolver : IHubProtocolResolver
    {
        private readonly ILogger<DefaultHubProtocolResolver> _logger;
        private readonly List<IHubProtocol> _hubProtocols;
        private readonly Dictionary<string, IHubProtocol> _availableProtocols;

        public IReadOnlyList<IHubProtocol> AllProtocols => _hubProtocols;

        public DefaultHubProtocolResolver(IEnumerable<IHubProtocol> availableProtocols, ILogger<DefaultHubProtocolResolver> logger)
        {
            _logger = logger ?? NullLogger<DefaultHubProtocolResolver>.Instance;
            _availableProtocols = new Dictionary<string, IHubProtocol>(StringComparer.OrdinalIgnoreCase);

            foreach (var protocol in availableProtocols)
            {
                Log.RegisteredSignalRProtocol(_logger, protocol.Name, protocol.GetType());
                _availableProtocols[protocol.Name] = protocol;
            }
            _hubProtocols = _availableProtocols.Values.ToList();
        }

        public virtual IHubProtocol GetProtocol(string protocolName, IReadOnlyList<string> supportedProtocols)
        {
            protocolName = protocolName ?? throw new ArgumentNullException(nameof(protocolName));

            if (_availableProtocols.TryGetValue(protocolName, out var protocol) && (supportedProtocols == null || supportedProtocols.Contains(protocolName, StringComparer.OrdinalIgnoreCase)))
            {
                Log.FoundImplementationForProtocol(_logger, protocolName);
                return protocol;
            }

            // null result indicates protocol is not supported
            // result will be validated by the caller
            return null;
        }

        private static class Log
        {
            // Category: DefaultHubProtocolResolver
            private static readonly Action<ILogger, string, Type, Exception> _registeredSignalRProtocol =
                LoggerMessage.Define<string, Type>(LogLevel.Debug, new EventId(1, "RegisteredSignalRProtocol"), "Registered SignalR Protocol: {ProtocolName}, implemented by {ImplementationType}.");

            private static readonly Action<ILogger, string, Exception> _foundImplementationForProtocol =
                LoggerMessage.Define<string>(LogLevel.Debug, new EventId(2, "FoundImplementationForProtocol"), "Found protocol implementation for requested protocol: {ProtocolName}.");

            public static void RegisteredSignalRProtocol(ILogger logger, string protocolName, Type implementationType)
            {
                _registeredSignalRProtocol(logger, protocolName, implementationType, null);
            }

            public static void FoundImplementationForProtocol(ILogger logger, string protocolName)
            {
                _foundImplementationForProtocol(logger, protocolName, null);
            }
        }
    }
}
