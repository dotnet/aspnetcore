// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Connections;

namespace Microsoft.AspNetCore.SignalR
{
    public static class SignalRConnectionBuilderExtensions
    {
        public static IConnectionBuilder UseHub<THub>(this IConnectionBuilder connectionBuilder) where THub : Hub
        {
            return connectionBuilder.UseConnectionHandler<HubConnectionHandler<THub>>();
        }
    }
}
