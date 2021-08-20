// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.SignalR.Internal;

namespace Microsoft.AspNetCore.SignalR
{
    public class TypedHubCallerClients<T> : TypedHubClients<T>, IHubCallerClients<T>
    {
        private readonly IHubCallerClients _hubClients;

        public TypedHubClients(IHubCallerClients dynamicContext)
            : base(dynamicContext)
        {
            _hubClients = dynamicContext;
        }

        public T Caller => TypedClientBuilder<T>.Build(_hubClients.Caller);

        public T Others => TypedClientBuilder<T>.Build(_hubClients.Others);

        public T OthersInGroup(string groupName)
        {
            return TypedClientBuilder<T>.Build(_hubClients.OthersInGroup(groupName));
        }
    }
}
