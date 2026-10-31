// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Localization;

/// <summary>
/// Represents a provider for determining the culture information of an <see cref="HttpRequest"/>.
/// </summary>
public interface IRequestCultureProvider
{
    /// <summary>
    /// Implements the provider to determine the culture of the given request.
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext"/> for the request.</param>
    /// <returns>
    ///     The determined <see cref="ProviderCultureResult"/>.
    ///     Returns <c>null</c> if the provider couldn't determine a <see cref="ProviderCultureResult"/>.
    /// </returns>
    Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext);
}
