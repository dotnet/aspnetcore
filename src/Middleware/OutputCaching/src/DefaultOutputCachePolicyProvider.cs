// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.OutputCaching;

/// <inheritdoc />
public class DefaultOutputCachePolicyProvider : IOutputCachePolicyProvider
{
    private readonly OutputCacheOptions _options;

    /// <summary>
    /// Creates a new instance of <see cref="DefaultOutputCachePolicyProvider"/>.
    /// </summary>
    /// <param name="options">The options configured for the application.</param>
    public DefaultOutputCachePolicyProvider(IOptions<OutputCacheOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options.Value;
    }

    /// <inheritdoc />
    public ValueTask<IOutputCachePolicy?> GetPolicyAsync(string policyName)
    {
        ArgumentNullException.ThrowIfNull(policyName);

        var policy = _options.NamedPolicies?[policyName];

        return new ValueTask<IOutputCachePolicy?>(policy);
    }
}