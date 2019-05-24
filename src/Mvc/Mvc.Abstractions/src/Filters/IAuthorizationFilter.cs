// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Filters
{
    /// <summary>
    /// A filter that confirms request authorization.
    /// </summary>
    public interface IAuthorizationFilter : IFilterMetadata
    {
        /// <summary>
        /// Called early in the filter pipeline to confirm request is authorized.
        /// </summary>
        /// <param name="context">The <see cref="AuthorizationFilterContext"/>.</param>
        void OnAuthorization(AuthorizationFilterContext context);
    }
}
