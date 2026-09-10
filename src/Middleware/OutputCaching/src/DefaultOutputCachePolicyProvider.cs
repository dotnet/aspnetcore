// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.OutputCaching;

internal sealed class DefaultOutputCachePolicyProvider : IOutputCachePolicyProvider
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
    public IReadOnlyList<IOutputCachePolicy> GetBasePolicies()
    {
        if (_options.BasePolicies is not null && _options.BasePolicies.Count > 0)
        {
            return _options.BasePolicies;
        }
        return Array.Empty<IOutputCachePolicy>();
    }

    /// <inheritdoc />
    public ValueTask<IOutputCachePolicy?> GetPolicyAsync(string policyName)
    {
        ArgumentNullException.ThrowIfNull(policyName);

        IOutputCachePolicy? policy = null;

        if (_options.NamedPolicies is not null && _options.NamedPolicies.TryGetValue(policyName, out var value))
        {
            policy = value;
        }

        return ValueTask.FromResult(policy);
    }
}
