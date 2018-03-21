// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.SignalR
{
    public abstract class HubCallerContext
    {
        public abstract string ConnectionId { get; }

        public abstract string UserIdentifier { get; }

        public abstract ClaimsPrincipal User { get; }

        public abstract IDictionary<object, object> Items { get; }

        public abstract IFeatureCollection Features { get; }

        public abstract CancellationToken ConnectionAborted { get; }

        public abstract void Abort();
    }
}
