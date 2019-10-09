// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Components.Routing
{
    /// <summary>
    /// <see cref="EventArgs" /> for <see cref="NavigationManager.LocationChanged" />.
    /// </summary>
    public class LocationChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of <see cref="LocationChangedEventArgs" />.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <param name="isNavigationIntercepted">A value that determines if navigation for the link was intercepted.</param>
        public LocationChangedEventArgs(string location, bool isNavigationIntercepted)
        {
            Location = location;
            IsNavigationIntercepted = isNavigationIntercepted;
        }

        /// <summary>
        /// Gets the changed location.
        /// </summary>
        public string Location { get; }

        /// <summary>
        /// Gets a value that determines if navigation for the link was intercepted.
        /// </summary>
        public bool IsNavigationIntercepted { get; }
    }
}
