// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.AspNetCore.Sockets;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.SignalR.Internal
{
    public class DefaultHubProtocolResolver : IHubProtocolResolver
    {
        public IHubProtocol GetProtocol(string protocolName, ConnectionContext connection)
        {
            switch (protocolName?.ToLowerInvariant())
            {
                case "json":
                    return new JsonHubProtocol(new JsonSerializer());
                case "messagepack":
                    return new MessagePackHubProtocol();
                default:
                    throw new NotSupportedException($"The protocol '{protocolName ?? "(null)"}' is not supported.");
            }
        }
    }
}
