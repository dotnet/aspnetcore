// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Provides access to browser messaging constructors from a <see cref="Window"/>.
/// </summary>
public static class WindowMessagingExtensions
{
    extension(Window window)
    {
        /// <summary>
        /// Gets the constructor for message channels.
        /// </summary>
        public MessageChannelConstructor MessageChannel
            => window.GetOrAddFeature(static context => new MessageChannelConstructor(context));

        /// <summary>
        /// Gets the constructor for broadcast channels.
        /// </summary>
        public BroadcastChannelConstructor BroadcastChannel
            => window.GetOrAddFeature(static context => new BroadcastChannelConstructor(context));
    }
}
