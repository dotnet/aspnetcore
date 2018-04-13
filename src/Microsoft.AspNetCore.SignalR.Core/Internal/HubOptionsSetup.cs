// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.SignalR.Internal
{
    public class HubOptionsSetup : IConfigureOptions<HubOptions>
    {
        internal static TimeSpan DefaultHandshakeTimeout => TimeSpan.FromSeconds(15);

        internal static TimeSpan DefaultKeepAliveInterval => TimeSpan.FromSeconds(15);

        private readonly List<string> _protocols = new List<string>();

        public HubOptionsSetup(IEnumerable<IHubProtocol> protocols)
        {
            foreach (var hubProtocol in protocols)
            {
                _protocols.Add(hubProtocol.Name);
            }
        }

        public void Configure(HubOptions options)
        {
            if (options.SupportedProtocols == null)
            {
                options.SupportedProtocols = new List<string>();
            }

            if (options.KeepAliveInterval == null)
            {
                // The default keep - alive interval.This is set to exactly half of the default client timeout window,
                // to ensure a ping can arrive in time to satisfy the client timeout.
                options.KeepAliveInterval = DefaultKeepAliveInterval;
            }

            if (options.HandshakeTimeout == null)
            {
                options.HandshakeTimeout = DefaultHandshakeTimeout;
            }

            foreach (var protocol in _protocols)
            {
                options.SupportedProtocols.Add(protocol);
            }
        }
    }
}

