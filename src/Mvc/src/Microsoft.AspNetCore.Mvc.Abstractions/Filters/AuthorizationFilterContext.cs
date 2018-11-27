// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Mvc.Filters
{
    /// <summary>
    /// A context for authorization filters i.e. <see cref="IAuthorizationFilter"/> and
    /// <see cref="IAsyncAuthorizationFilter"/> implementations.
    /// </summary>
    public class AuthorizationFilterContext : FilterContext
    {
        /// <summary>
        /// Instantiates a new <see cref="AuthorizationFilterContext"/> instance.
        /// </summary>
        /// <param name="actionContext">The <see cref="ActionContext"/>.</param>
        /// <param name="filters">All applicable <see cref="IFilterMetadata"/> implementations.</param>
        public AuthorizationFilterContext(
            ActionContext actionContext,
            IList<IFilterMetadata> filters)
            : base(actionContext, filters)
        {
        }

        /// <summary>
        /// Gets or sets the result of the request. Setting <see cref="Result"/> to a non-<c>null</c> value inside
        /// an authorization filter will short-circuit the remainder of the filter pipeline.
        /// </summary>
        public virtual IActionResult Result { get; set; }
    }
}
