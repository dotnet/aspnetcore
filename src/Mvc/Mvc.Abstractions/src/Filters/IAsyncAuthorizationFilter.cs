// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc.Filters
{
    /// <summary>
    /// A filter that asynchronously confirms request authorization.
    /// </summary>
    public interface IAsyncAuthorizationFilter : IFilterMetadata
    {
        /// <summary>
        /// Called early in the filter pipeline to confirm request is authorized.
        /// </summary>
        /// <param name="context">The <see cref="AuthorizationFilterContext"/>.</param>
        /// <returns>
        /// A <see cref="Task"/> that on completion indicates the filter has executed.
        /// </returns>
        Task OnAuthorizationAsync(AuthorizationFilterContext context);
    }
}
