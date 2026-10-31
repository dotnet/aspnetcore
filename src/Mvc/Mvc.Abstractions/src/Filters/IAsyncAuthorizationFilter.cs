// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Filters;

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
