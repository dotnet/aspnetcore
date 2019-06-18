// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.SignalR
{
    public class HubOptionsSetup<THub> : IConfigureOptions<HubOptions<THub>> where THub : Hub
    {
        private readonly HubOptions _hubOptions;
        public HubOptionsSetup(IOptions<HubOptions> options)
        {
            _hubOptions = options.Value;
        }

        public void Configure(HubOptions<THub> options)
        {
            options.SupportedProtocols = new List<string>(_hubOptions.SupportedProtocols.Count);
            foreach (var protocol in _hubOptions.SupportedProtocols)
            {
                options.SupportedProtocols.Add(protocol);
            }
            options.KeepAliveInterval = _hubOptions.KeepAliveInterval;
            options.HandshakeTimeout = _hubOptions.HandshakeTimeout;
        }
    }
}
