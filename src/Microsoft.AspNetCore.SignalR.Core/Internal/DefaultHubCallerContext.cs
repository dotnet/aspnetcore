// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.SignalR.Internal
{
    public class DefaultHubCallerContext : HubCallerContext
    {
        private readonly HubConnectionContext _connection;

        public DefaultHubCallerContext(HubConnectionContext connection)
        {
            _connection = connection;
        }

        public override string ConnectionId => _connection.ConnectionId;

        public override string UserIdentifier => _connection.UserIdentifier;

        public override ClaimsPrincipal User => _connection.User;

        public override IDictionary<object, object> Items => _connection.Items;

        public override IFeatureCollection Features => _connection.Features;

        public override CancellationToken ConnectionAborted => _connection.ConnectionAborted;

        public override void Abort() => _connection.Abort();
    }
}
