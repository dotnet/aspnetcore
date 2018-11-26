// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Routing.Template
{
    /// <summary>
    /// The values used as inputs for constraints and link generation.
    /// </summary>
    public class TemplateValuesResult
    {
        /// <summary>
        /// The set of values that will appear in the URL.
        /// </summary>
        public RouteValueDictionary AcceptedValues { get; set; }

        /// <summary>
        /// The set of values that that were supplied for URL generation.
        /// </summary>
        /// <remarks>
        /// This combines implicit (ambient) values from the <see cref="RouteData"/> of the current request
        /// (if applicable), explictly provided values, and default values for parameters that appear in
        /// the route template.
        ///
        /// Implicit (ambient) values which are invalidated due to changes in values lexically earlier in the
        /// route template are excluded from this set.
        /// </remarks>
        public RouteValueDictionary CombinedValues { get; set; }
    }
}