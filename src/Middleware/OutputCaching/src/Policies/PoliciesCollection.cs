// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Linq;

namespace Microsoft.AspNetCore.OutputCaching.Policies;

/// <summary>
/// A collection of policies.
/// </summary>
public sealed class PoliciesCollection : IReadOnlyCollection<IOutputCachePolicy>
{
    private List<IOutputCachePolicy>? _policies;

    /// <inheritdoc/>
    public int Count => _policies == null ? 0 : _policies.Count;

    /// <summary>
    /// Adds an <see cref="IOutputCachePolicy"/> instance.
    /// </summary>
    /// <param name="policy">The policy</param>
    public void AddPolicy(IOutputCachePolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        _policies ??= new();
        _policies.Add(policy);
    }

    /// <summary>
    /// Adds <see cref="IOutputCachePolicy"/> instance.
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
    public IEnumerator<IOutputCachePolicy> GetEnumerator()
    {
        return _policies == null ? Enumerable.Empty<IOutputCachePolicy>().GetEnumerator() : _policies.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
