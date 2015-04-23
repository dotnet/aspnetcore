// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Routing;

namespace Microsoft.AspNet.Mvc.ApiExplorer
{
    /// <summary>
    /// A metadata description of routing information for an <see cref="ApiParameterDescription"/>.
    /// </summary>
    public class ApiParameterRouteInfo
    {
        /// <summary>
        /// Gets or sets the set of <see cref="IRouteConstraint"/> objects for the parameter.
        /// </summary>
        /// <remarks>
        /// Route constraints are only applied when a value is bound from a URL's path. See
        /// <see cref="ApiParameterDescription.Source"/> for the data source considered.
        /// </remarks>
        public IEnumerable<IRouteConstraint> Constraints { get; set; }

        /// <summary>
        /// Gets or sets the default value for the parameter.
        /// </summary>
        public object DefaultValue { get; set; }

        /// <summary>
        /// Gets a value indicating whether not a parameter is considered optional by routing.
        /// </summary>
        /// <remarks>
        /// An optional parameter is considered optional by the routing system. This does not imply
        /// that the parameter is considered optional by the action.
        ///
        /// If the parameter uses <see cref="ModelBinding.BindingSource.ModelBinding"/> for the value of
        /// <see cref="ApiParameterDescription.Source"/> then the value may also come from the
        /// URL query string or form data.
        /// </remarks>
        public bool IsOptional { get; set; }
    }
}