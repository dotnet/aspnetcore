// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Cors.Infrastructure;

/// <inheritdoc />
public class DefaultCorsPolicyProvider : ICorsPolicyProvider
{
    private static readonly Task<CorsPolicy?> NullResult = Task.FromResult<CorsPolicy?>(null);
    private readonly CorsOptions _options;

    /// <summary>
    /// Creates a new instance of <see cref="DefaultCorsPolicyProvider"/>.
    /// </summary>
    /// <param name="options">The options configured for the application.</param>
    public DefaultCorsPolicyProvider(IOptions<CorsOptions> options)
    {
        _options = options.Value;
    }

    /// <inheritdoc />
    public Task<CorsPolicy?> GetPolicyAsync(HttpContext context, string? policyName)
    {
        ArgumentNullException.ThrowIfNull(context);

        policyName ??= _options.DefaultPolicyName;
        if (_options.PolicyMap.TryGetValue(policyName, out var result))
        {
            return result.policyTask!;
        }

        return NullResult;
    }
}
