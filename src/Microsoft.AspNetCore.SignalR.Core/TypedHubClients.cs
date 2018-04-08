// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.SignalR
{
    internal class TypedHubClients<T> : IHubCallerClients<T>
    {
        private readonly IHubCallerClients _hubClients;

        public TypedHubClients(IHubCallerClients dynamicContext)
        {
            _hubClients = dynamicContext;
        }

        public T All => TypedClientBuilder<T>.Build(_hubClients.All);

        public T Caller => TypedClientBuilder<T>.Build(_hubClients.Caller);

        public T Others => TypedClientBuilder<T>.Build(_hubClients.Others);

        public T AllExcept(IReadOnlyList<string> excludedIds) => TypedClientBuilder<T>.Build(_hubClients.AllExcept(excludedIds));

        public T Client(string connectionId)
        {
            return TypedClientBuilder<T>.Build(_hubClients.Client(connectionId));
        }

        public T Group(string groupName)
        {
            return TypedClientBuilder<T>.Build(_hubClients.Group(groupName));
        }

        public T GroupExcept(string groupName, IReadOnlyList<string> excludeIds)
        {
            return TypedClientBuilder<T>.Build(_hubClients.GroupExcept(groupName, excludeIds));
        }

        public T Clients(IReadOnlyList<string> connectionIds)
        {
            return TypedClientBuilder<T>.Build(_hubClients.Clients(connectionIds));
        }

        public T Groups(IReadOnlyList<string> groupNames)
        {
            return TypedClientBuilder<T>.Build(_hubClients.Groups(groupNames));
        }

        public T OthersInGroup(string groupName)
        {
            return TypedClientBuilder<T>.Build(_hubClients.OthersInGroup(groupName));
        }

        public T User(string userId)
        {
            return TypedClientBuilder<T>.Build(_hubClients.User(userId));
        }

        public T Users(IReadOnlyList<string> userIds)
        {
            return TypedClientBuilder<T>.Build(_hubClients.Users(userIds));
        }
    }
}
