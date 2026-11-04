// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Cors.Infrastructure;

/// <summary>
/// Provides programmatic configuration for Cors.
/// </summary>
public class CorsOptions
{
    private string _defaultPolicyName = "__DefaultCorsPolicy";

    // DefaultCorsPolicyProvider returns a Task<CorsPolicy>. We'll cache the value to be returned alongside
    // the actual policy instance to have a separate lookup.
    internal IDictionary<string, (CorsPolicy policy, Task<CorsPolicy> policyTask)> PolicyMap { get; }
        = new Dictionary<string, (CorsPolicy, Task<CorsPolicy>)>(StringComparer.Ordinal);

    /// <summary>
    /// Gets or sets the default policy name.
    /// </summary>
    public string DefaultPolicyName
    {
        get => _defaultPolicyName;
        set
        {
            _defaultPolicyName = value ?? throw new ArgumentNullException(nameof(value));
        }
    }

    /// <summary>
    /// Adds a new policy and sets it as the default.
    /// </summary>
    /// <param name="policy">The <see cref="CorsPolicy"/> policy to be added.</param>
    public void AddDefaultPolicy(CorsPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        AddPolicy(DefaultPolicyName, policy);
    }

    /// <summary>
    /// Adds a new policy and sets it as the default.
    /// </summary>
    /// <param name="configurePolicy">A delegate which can use a policy builder to build a policy.</param>
    public void AddDefaultPolicy(Action<CorsPolicyBuilder> configurePolicy)
    {
        ArgumentNullException.ThrowIfNull(configurePolicy);

        AddPolicy(DefaultPolicyName, configurePolicy);
    }

    /// <summary>
    /// Adds a new policy.
    /// </summary>
    /// <param name="name">The name of the policy.</param>
    /// <param name="policy">The <see cref="CorsPolicy"/> policy to be added.</param>
    public void AddPolicy(string name, CorsPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(policy);

        PolicyMap[name] = (policy, Task.FromResult(policy));
    }

    /// <summary>
    /// Adds a new policy.
    /// </summary>
    /// <param name="name">The name of the policy.</param>
    /// <param name="configurePolicy">A delegate which can use a policy builder to build a policy.</param>
    public void AddPolicy(string name, Action<CorsPolicyBuilder> configurePolicy)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(configurePolicy);

        var policyBuilder = new CorsPolicyBuilder();
        configurePolicy(policyBuilder);
        var policy = policyBuilder.Build();

        PolicyMap[name] = (policy, Task.FromResult(policy));
    }

    /// <summary>
    /// Gets the policy based on the <paramref name="name"/>
    /// </summary>
    /// <param name="name">The name of the policy to lookup.</param>
    /// <returns>The <see cref="CorsPolicy"/> if the policy was added.<c>null</c> otherwise.</returns>
    public CorsPolicy? GetPolicy(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        if (PolicyMap.TryGetValue(name, out var result))
        {
            return result.policy;
        }

        return null;
    }
}
