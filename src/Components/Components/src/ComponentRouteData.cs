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
    public sealed class ComponentRouteData
    {
        /// <summary>
        /// Constructs an instance of <see cref="ComponentRouteData"/>.
        /// </summary>
        /// <param name="pageComponentType">The type of the page component matching the route.</param>
        /// <param name="pageParameters">The parameters for the page component matching the route.</param>
        public ComponentRouteData(Type pageComponentType, IReadOnlyDictionary<string, object> pageParameters)
        {
            PageComponentType = pageComponentType ?? throw new ArgumentNullException(nameof(pageComponentType));
            PageParameters = pageParameters ?? throw new ArgumentNullException(nameof(pageParameters));
        }

        /// <summary>
        /// Gets the type of the page component matching the route.
        /// </summary>
        public Type PageComponentType { get; }

        /// <summary>
        /// Gets the parameters for the page component matching the route.
        /// </summary>
        public IReadOnlyDictionary<string, object> PageParameters { get; }
    }
}
