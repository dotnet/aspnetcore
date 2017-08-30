// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.SignalR
{
    internal class TypedHubClients<T> : IHubClients<T>
    {
        private IHubClients hubClients;

        public TypedHubClients(IHubClients dynamicContext)
        {
            hubClients = dynamicContext;
        }

        public T All => TypedClientBuilder<T>.Build(hubClients.All);

        public T Client(string connectionId)
        {
            return TypedClientBuilder<T>.Build(hubClients.Client(connectionId));
        }

        public T Group(string groupName)
        {
            return TypedClientBuilder<T>.Build(hubClients.Group(groupName));
        }

        public T User(string userId)
        {
            return TypedClientBuilder<T>.Build(hubClients.User(userId));
        }
    }
}
