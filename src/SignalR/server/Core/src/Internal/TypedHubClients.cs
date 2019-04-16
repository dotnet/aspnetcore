// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.SignalR.Internal
{
    internal class TypedHubClients<T> : HubCallerClientsBase<T>
    {
        private readonly HubCallerClientsBase _hubClients;

        public TypedHubClients(HubCallerClientsBase dynamicContext)
        {
            _hubClients = dynamicContext;
        }

        public override T All => TypedClientBuilder<T>.Build(_hubClients.All);

        public override T Caller => TypedClientBuilder<T>.Build(_hubClients.Caller);

        public override T Others => TypedClientBuilder<T>.Build(_hubClients.Others);

        public override T AllExcept(IReadOnlyList<string> excludedConnectionIds) => TypedClientBuilder<T>.Build(_hubClients.AllExcept(excludedConnectionIds));

        public override T Client(string connectionId)
        {
            return TypedClientBuilder<T>.Build(_hubClients.Client(connectionId));
        }

        public override T Group(string groupName)
        {
            return TypedClientBuilder<T>.Build(_hubClients.Group(groupName));
        }

        public override T GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds)
        {
            return TypedClientBuilder<T>.Build(_hubClients.GroupExcept(groupName, excludedConnectionIds));
        }

        public override T Clients(IReadOnlyList<string> connectionIds)
        {
            return TypedClientBuilder<T>.Build(_hubClients.Clients(connectionIds));
        }

        public override T Groups(IReadOnlyList<string> groupNames)
        {
            return TypedClientBuilder<T>.Build(_hubClients.Groups(groupNames));
        }

        public override T OthersInGroup(string groupName)
        {
            return TypedClientBuilder<T>.Build(_hubClients.OthersInGroup(groupName));
        }

        public override T User(string userId)
        {
            return TypedClientBuilder<T>.Build(_hubClients.User(userId));
        }

        public override T Users(IReadOnlyList<string> userIds)
        {
            return TypedClientBuilder<T>.Build(_hubClients.Users(userIds));
        }
    }
}
