// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks.Channels;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR.Features;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.AspNetCore.Sockets;
using Microsoft.AspNetCore.Sockets.Features;

namespace Microsoft.AspNetCore.SignalR
{
    public class HubConnectionContext
    {
        private readonly WritableChannel<HubMessage> _output;
        private readonly ConnectionContext _connectionContext;

        public HubConnectionContext(WritableChannel<HubMessage> output, ConnectionContext connectionContext)
        {
            _output = output;
            _connectionContext = connectionContext;
        }

        private IHubFeature HubFeature => Features.Get<IHubFeature>();

        // Used by the HubEndPoint only
        internal ReadableChannel<byte[]> Input => _connectionContext.Transport;

        public virtual string ConnectionId => _connectionContext.ConnectionId;

        public virtual ClaimsPrincipal User => Features.Get<IConnectionUserFeature>()?.User;

        public virtual IFeatureCollection Features => _connectionContext.Features;

        public virtual IDictionary<object, object> Metadata => _connectionContext.Metadata;

        public virtual HubProtocolReaderWriter ProtocolReaderWriter { get; set; }

        public virtual WritableChannel<HubMessage> Output => _output;
    }
}
