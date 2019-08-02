// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Describes information determined during routing that specifies
    /// the page to be displayed.
    /// </summary>
    public sealed class RouteData
    {
        /// <summary>
        /// Constructs an instance of <see cref="RouteData"/>.
        /// </summary>
        /// <param name="pageType">The type of the page matching the route, which must implement <see cref="IComponent"/>.</param>
        /// <param name="pageParameters">The parameters for the page component matching the route.</param>
        public RouteData(Type pageType, IReadOnlyDictionary<string, object> pageParameters)
        {
            if (pageType == null)
            {
                throw new ArgumentNullException(nameof(pageType));
            }

            if (!typeof(IComponent).IsAssignableFrom(pageType))
            {
                throw new ArgumentException($"The value must implement {nameof(IComponent)}.", nameof(pageType));
            }

            PageType = pageType;
            PageParameters = pageParameters ?? throw new ArgumentNullException(nameof(pageParameters));
        }

        /// <summary>
        /// Gets the type of the page matching the route.
        /// </summary>
        public Type PageType { get; }

        /// <summary>
        /// Gets the parameters for the page component matching the route.
        /// </summary>
        public IReadOnlyDictionary<string, object> PageParameters { get; }
    }
}
