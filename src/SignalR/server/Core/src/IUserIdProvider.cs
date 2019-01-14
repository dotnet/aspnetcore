// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.SignalR
{
    /// <summary>
    /// A provider abstraction for configuring the "User ID" for a connection.
    /// </summary>
    /// <remarks><see cref="IUserIdProvider"/> is used by <see cref="IHubClients{T}.User(string)"/> to invoke connections associated with a user.</remarks>
    public interface IUserIdProvider
    {
        /// <summary>
        /// Gets the user ID for the specified connection.
        /// </summary>
        /// <param name="connection">The connection to get the user ID for.</param>
        /// <returns>The user ID for the specified connection.</returns>
        string GetUserId(HubConnectionContext connection);
    }
}