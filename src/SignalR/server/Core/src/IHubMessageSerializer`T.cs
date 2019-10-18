// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.SignalR.Protocol;

namespace Microsoft.AspNetCore.SignalR
{
    /// <summary>
    /// An abstraction that provides serialized <see cref="HubMessage"/>s for relevant <see cref="IHubProtocol"/>s.
    /// </summary>
    public interface IHubMessageSerializer<THub> where THub : Hub
    {
        /// <summary>
        /// Serializes the provided <see cref="HubMessage"/> for all <see cref="IHubProtocol"/>s registered for this Hub.
        /// </summary>
        /// <param name="message">The <see cref="HubMessage"/> to serialize.</param>
        /// <returns>Contains the serialized <see cref="HubMessage"/> for all protocols registered for this Hub.</returns>
        SerializedHubMessage SerializeMessage(HubMessage message);
    }
}
