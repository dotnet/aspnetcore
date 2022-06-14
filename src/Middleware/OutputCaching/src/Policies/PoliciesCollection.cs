// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.OutputCaching.Policies;

/// <summary>
/// A collection of policies.
/// </summary>
public sealed class PoliciesCollection : IReadOnlyCollection<IOutputCachingPolicy>
{
    private const DynamicallyAccessedMemberTypes ActivatorAccessibility = DynamicallyAccessedMemberTypes.PublicConstructors;

    private readonly OutputCachingOptions _options;
    private List<IOutputCachingPolicy>? _policies;

    internal PoliciesCollection(OutputCachingOptions options)
    {
        _options = options;
    }

    /// <inheritdoc/>
    public int Count => _policies == null ? 0 : _policies.Count;

    /// <summary>
    /// Adds an <see cref="IOutputCachingPolicy"/> instance.
    /// </summary>
    /// <param name="policy">The policy</param>
    public void AddPolicy(IOutputCachingPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        _policies ??= new();
        _policies.Add(policy);
    }

    /// <summary>
    /// Adds a dynamically resolved <see cref="IOutputCachingPolicy"/> instance.
    /// </summary>
    /// <param name="policyType">The type of policy to add.</param>
    public void AddPolicy([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type policyType)
    {
        if (ActivatorUtilities.GetServiceOrCreateInstance(_options.ApplicationServices, policyType) is not IOutputCachingPolicy policy)
        {
            throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.Policy_InvalidType));
        }

        AddPolicy(policy);
    }

    /// <summary>
    /// Adds a dynamically resolved <see cref="IOutputCachingPolicy"/> instance.
    /// </summary>
    /// <typeparam name="T">The type of policy to add.</typeparam>
    public void AddPolicy<[DynamicallyAccessedMembers(ActivatorAccessibility)] T>(string name) where T : IOutputCachingPolicy
    {
        AddPolicy(typeof(T));
    }

    /// <summary>
    /// Adds <see cref="IOutputCachingPolicy"/> instance.
    /// </summary>
    public void AddPolicy(Action<OutputCachePolicyBuilder> build)
    {
        var builder = new OutputCachePolicyBuilder();
        build(builder);
        AddPolicy(builder.Build());
    }

    /// <summary>
    /// Clears the collection.
    /// </summary>
    public void Clear()
    {
        _policies = null;
    }

    /// <inheritdoc/>
    public IEnumerator<IOutputCachingPolicy> GetEnumerator()
    {
        return _policies == null ? Enumerable.Empty<IOutputCachingPolicy>().GetEnumerator() : _policies.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
