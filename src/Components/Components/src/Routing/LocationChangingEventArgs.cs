// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Components.Routing
{
    /// <summary>
    /// <see cref="EventArgs" /> for <see cref="NavigationManager.LocationChanging" />.
    /// </summary>
    public class LocationChangingEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of <see cref="LocationChangingEventArgs" />.
        /// </summary>
        /// <param name="location">The location of the navigation request.</param>
        /// <param name="forceLoad">If true, the requested navigation will bypass client-side routing and force the browser to load the new page from the server, whether or not the URI would normally be handled by the client-side router.</param>
        public LocationChangingEventArgs(string location, bool forceLoad)
        {
            Location = location;
            ForceLoad = forceLoad;
        }

        /// <summary>
        /// Gets the changed location.
        /// </summary>
        public string Location { get; }

        /// <summary>
        /// Gets a value that determines if navigation should be prevented.
        /// </summary>
        public bool IsNavigationPrevented { get; private set; }

        /// <summary>
        /// If true, the requested navigation will bypass client-side routing and force the browser to load the new page from the server, whether or not the URI would normally be handled by the client-side router.
        /// </summary>
        public bool ForceLoad { get; }

        /// <summary>
        /// Indicates to the <see cref="Microsoft.AspNetCore.Components.NavigationManager"/>
        /// that the navigation should be prevented.
        /// </summary>
        public void PreventNavigation()
        {
            IsNavigationPrevented = true;
        }
    }
}
