// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.Routing
{

    /// <summary>
    /// context used by <see cref="IHandleLocationChanging" /> to see what kind of navigation is occuring.
    /// </summary>
    public class LocationChangingContext
    {
        /// <summary>
        /// Initializes a new instance of <see cref="LocationChangingContext" />.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <param name="isNavigationIntercepted">A value that determines if navigation for the link was intercepted.</param>
        /// <param name="forceLoad">A value that shows if the forceLoad flag was set during a call to <see cref="NavigationManager.NavigateTo" /> </param>
        public LocationChangingContext(string location, bool isNavigationIntercepted, bool forceLoad)
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
    }
}
