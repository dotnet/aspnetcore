// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace Microsoft.AspNetCore.SignalR.Internal
{
    internal abstract class HubDispatcher<THub> where THub : Hub
    {
        public abstract Task OnConnectedAsync(HubConnectionContext connection);
        public abstract Task OnDisconnectedAsync(HubConnectionContext connection, Exception exception);
        public abstract Task DispatchMessageAsync(HubConnectionContext connection, HubMessage hubMessage);
        public abstract IReadOnlyList<Type> GetParameterTypes(string name);
    }
}
