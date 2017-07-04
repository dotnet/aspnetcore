// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading.Tasks.Channels;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.AspNetCore.Sockets;

namespace Microsoft.AspNetCore.SignalR
{
    public class HubConnectionContext
    {
        private readonly WritableChannel<byte[]> _output;
        private readonly ConnectionContext _connectionContext;

        public HubConnectionContext(WritableChannel<byte[]> output, ConnectionContext connectionContext)
        {
            _output = output;
            _connectionContext = connectionContext;
        }

        // Used by the HubEndPoint only
        internal ReadableChannel<byte[]> Input => _connectionContext.Transport;

        public virtual string ConnectionId => _connectionContext.ConnectionId;

        public virtual ClaimsPrincipal User => _connectionContext.User;

        public virtual ConnectionMetadata Metadata => _connectionContext.Metadata;

        public virtual IHubProtocol Protocol => _connectionContext.Metadata.Get<IHubProtocol>(HubConnectionMetadataNames.HubProtocol);

        public virtual WritableChannel<byte[]> Output => _output;
    }
}
