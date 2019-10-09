// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.SignalR.Client
{
    /// <summary>
    /// A builder abstraction for configuring <see cref="HubConnection"/> instances.
    /// </summary>
    public interface IHubConnectionBuilder : ISignalRBuilder
    {
        /// <summary>
        /// Creates a <see cref="HubConnection"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="HubConnection"/> built using the configured options.
        /// </returns>
        HubConnection Build();
    }
}
