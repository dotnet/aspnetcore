// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.SignalR
{
    public class HubOptionsSetup : IConfigureOptions<HubOptions>
    {
        internal static TimeSpan DefaultHandshakeTimeout => TimeSpan.FromSeconds(15);

        internal static TimeSpan DefaultKeepAliveInterval => TimeSpan.FromSeconds(15);

        internal static TimeSpan DefaultClientTimeoutInterval => TimeSpan.FromSeconds(30);

        internal const int DefaultMaximumMessageSize = 32 * 1024;

        internal const int DefaultStreamBufferCapacity = 10;

        private readonly List<string> _defaultProtocols = new List<string>();

        public HubOptionsSetup(IEnumerable<IHubProtocol> protocols)
        {
            foreach (var hubProtocol in protocols)
            {
                if (hubProtocol.GetType().CustomAttributes.Where(a => a.AttributeType.FullName == "Microsoft.AspNetCore.SignalR.Internal.NonDefaultHubProtocolAttribute").Any())
                {
                    continue;
                }
                _defaultProtocols.Add(hubProtocol.Name);
            }
        }

        public void Configure(HubOptions options)
        {
            if (options.KeepAliveInterval == null)
            {
                // The default keep - alive interval. This is set to exactly half of the default client timeout window,
                // to ensure a ping can arrive in time to satisfy the client timeout.
                options.KeepAliveInterval = DefaultKeepAliveInterval;
            }

            if (options.HandshakeTimeout == null)
            {
                options.HandshakeTimeout = DefaultHandshakeTimeout;
            }

            if (options.MaximumReceiveMessageSize == null)
            {
                options.MaximumReceiveMessageSize = DefaultMaximumMessageSize;
            }

            if (options.SupportedProtocols == null)
            {
                options.SupportedProtocols = new List<string>();
            }

            if (options.StreamBufferCapacity == null)
            {
                options.StreamBufferCapacity = DefaultStreamBufferCapacity;
            }

            foreach (var protocol in _defaultProtocols)
            {
                options.SupportedProtocols.Add(protocol);
            }
        }
    }
}

