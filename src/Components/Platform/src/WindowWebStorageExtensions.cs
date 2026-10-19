// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Provides access to the Web Storage API from a <see cref="Window"/>.
/// </summary>
public static class WindowWebStorageExtensions
{
    private static readonly EventType<Window, StorageEvent> _storageEvent = EventTypes.Create<Window, StorageEventPayload, StorageEvent>(
        "storage",
        """[{"key":null,"oldValue":null,"newValue":null,"url":null,"storageArea":{"$known":["localStorage","sessionStorage"]}}]""",
        static (window, payload) => payload.ToStorageEvent(window));

    extension(Window)
    {
        /// <summary>
        /// Gets the event that fires when another page of the same origin changes local or session storage.
        /// It does not fire for changes that the current page makes.
        /// </summary>
        public static EventType<Window, StorageEvent> StorageEvent => _storageEvent;
    }

    extension(Window window)
    {
        /// <summary>
        /// Gets storage shared by pages from the same origin.
        /// </summary>
        public Storage LocalStorage
            => window.GetOrAddFeature("localStorage", static jsRuntime => new Storage(jsRuntime, "localStorage"));

        /// <summary>
        /// Gets storage scoped to the current browser tab.
        /// </summary>
        public Storage SessionStorage
            => window.GetOrAddFeature("sessionStorage", static jsRuntime => new Storage(jsRuntime, "sessionStorage"));
    }
}
