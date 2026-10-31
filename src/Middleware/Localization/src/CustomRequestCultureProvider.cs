// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Localization;

/// <summary>
/// Determines the culture information for a request via the configured delegate.
/// </summary>
public class CustomRequestCultureProvider : RequestCultureProvider
{
    private readonly Func<HttpContext, Task<ProviderCultureResult?>> _provider;

    /// <summary>
    /// Creates a new <see cref="CustomRequestCultureProvider"/> using the specified delegate.
    /// </summary>
    /// <param name="provider">The provider delegate.</param>
    public CustomRequestCultureProvider(Func<HttpContext, Task<ProviderCultureResult?>> provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        _provider = provider;
    }

    /// <inheritdoc />
    public override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        return _provider(httpContext);
    }
}
