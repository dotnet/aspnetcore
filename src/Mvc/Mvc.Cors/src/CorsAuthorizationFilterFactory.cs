// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.Cors;

/// <summary>
/// A filter factory which creates a new instance of <see cref="CorsAuthorizationFilter"/>.
/// </summary>
internal sealed class CorsAuthorizationFilterFactory : IFilterFactory, IOrderedFilter
{
    private readonly string? _policyName;

    /// <summary>
    /// Creates a new instance of <see cref="CorsAuthorizationFilterFactory"/>.
    /// </summary>
    /// <param name="policyName">Name used to fetch a CORS policy.</param>
    public CorsAuthorizationFilterFactory(string? policyName)
    {
        _policyName = policyName;
    }

    /// <inheritdoc />
    // Since clients' preflight requests would not have data to authenticate requests, this
    // filter must run before any other authorization filters.
    public int Order => int.MinValue + 100;

    /// <inheritdoc />
    public bool IsReusable => true;

    /// <inheritdoc />
    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        var filter = serviceProvider.GetRequiredService<CorsAuthorizationFilter>();
        filter.PolicyName = _policyName;
        return filter;
    }
}
