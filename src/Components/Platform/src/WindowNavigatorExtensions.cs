// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Provides access to the navigator from a <see cref="Window"/>.
/// </summary>
public static class WindowNavigatorExtensions
{
    private static readonly EventType<Window, Event> _onlineEvent = EventTypes.Create<Window, Event>("online", "[{}]");
    private static readonly EventType<Window, Event> _offlineEvent = EventTypes.Create<Window, Event>("offline", "[{}]");

    extension(Window)
    {
        /// <summary>
        /// Gets the event that fires when the browser gains access to the network,
        /// so that <see cref="Navigator.GetOnLineAsync"/> returns <see langword="true"/>.
        /// </summary>
        public static EventType<Window, Event> OnlineEvent => _onlineEvent;

        /// <summary>
        /// Gets the event that fires when the browser loses access to the network,
        /// so that <see cref="Navigator.GetOnLineAsync"/> returns <see langword="false"/>.
        /// </summary>
        public static EventType<Window, Event> OfflineEvent => _offlineEvent;
    }

    extension(Window window)
    {
        /// <summary>
        /// Gets the state and identity of the browser.
        /// </summary>
        public Navigator Navigator
            => window.GetOrAddFeature(static (context, window) => new Navigator(context.JSRuntime, window), window);
    }
}
