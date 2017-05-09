// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.AspNetCore.Sockets;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.SignalR.Internal
{
    public class DefaultHubProtocolResolver : IHubProtocolResolver
    {
        public IHubProtocol GetProtocol(Connection connection)
        {
            // TODO: Allow customization of this serializer!
            return new JsonHubProtocol(new JsonSerializer());
        }
    }
}
