// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Sockets
{
    public class DefaultConnectionContext : ConnectionContext
    {
        public DefaultConnectionContext(string id, IChannelConnection<Message> transport)
        {
            Transport = transport;
            ConnectionId = id;
        }

        public override string ConnectionId { get; }

        public override IFeatureCollection Features { get; } = new FeatureCollection();

        public override ClaimsPrincipal User { get; set; }

        public override ConnectionMetadata Metadata { get; } = new ConnectionMetadata();

        public override IChannelConnection<Message> Transport { get; set; }
    }
}
