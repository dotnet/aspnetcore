// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Routing
{
    /// <summary>
    /// Represents metadata used during link generation to find
    /// the associated endpoint using route values.
    /// </summary>
    public interface IRouteValuesAddressMetadata
    {
        /// <summary>
        /// Gets the route name. Can be null.
        /// </summary>
        string RouteName { get; }

        /// <summary>
        /// Gets the required route values.
        /// </summary>
        IReadOnlyDictionary<string, object> RequiredValues { get; }
    }
}
