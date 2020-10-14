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
        /// <param name="location">The location.</param>
        /// <param name="isNavigationIntercepted">A value that determines if navigation for the link was intercepted.</param>
        /// <param name="forceLoad">A value that shows if the forceLoad flag was set during a call to <see cref="NavigationManager.NavigateTo" /> </param>
        public LocationChangingEventArgs(string location, bool isNavigationIntercepted, bool forceLoad)
        {
            Location = location;
            IsNavigationIntercepted = isNavigationIntercepted;
            ForceLoad = forceLoad;
        }

        /// <summary>
        /// Gets the location we are about to change to.
        /// </summary>
        public string Location { get; }

        /// <summary>
        /// Gets a value that determines if navigation for the link was intercepted.
        /// </summary>
        public bool IsNavigationIntercepted { get; }

        /// <summary>
        /// Gets a value if the Forceload flag was set during a call to <see cref="NavigationManager.NavigateTo" /> 
        /// </summary>
        public bool ForceLoad { get; }

        /// <summary>
        /// Gets or sets a value to cancel the current navigation
        /// </summary>
        public bool Cancel { get; set; }
    }
}
