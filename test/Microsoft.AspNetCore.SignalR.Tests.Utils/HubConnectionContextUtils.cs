// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Internal.Encoders;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.AspNetCore.Sockets;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    public static class HubConnectionContextUtils
    {
        public static HubConnectionContext Create(DefaultConnectionContext connection, Channel<HubMessage> replacementOutput = null)
        {
            HubConnectionContext context = null;
            if (replacementOutput != null)
            {
                context = new HubConnectionContext(connection, TimeSpan.FromSeconds(15), NullLoggerFactory.Instance, replacementOutput);
            }
            else
            {
                context = new HubConnectionContext(connection, TimeSpan.FromSeconds(15), NullLoggerFactory.Instance);
            }

            context.ProtocolReaderWriter = new HubProtocolReaderWriter(new JsonHubProtocol(), new PassThroughEncoder());

            _ = context.StartAsync();

            return context;
        }
    }
}
