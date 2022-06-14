// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.OutputCaching.Policies;

/// <summary>
/// A type base policy.
/// </summary>
internal sealed class TypedPolicy : IOutputCachingPolicy
{
    private IOutputCachingPolicy? _instance;

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    private readonly Type _policyType;

    /// <summary>
    /// Creates a new instance of <see cref="TypedPolicy"/>
    /// </summary>
    /// <param name="policyType">The type of policy.</param>
    public TypedPolicy([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type policyType)
    {
        ArgumentNullException.ThrowIfNull(policyType);

        _policyType = policyType;
    }

    private IOutputCachingPolicy? CreatePolicy(OutputCachingContext context)
    {
        return _instance ??= ActivatorUtilities.CreateInstance(context.Options.ApplicationServices, _policyType) as IOutputCachingPolicy;
    }

    /// <inheritdoc/>
    Task IOutputCachingPolicy.OnRequestAsync(OutputCachingContext context)
    {
        return CreatePolicy(context)?.OnRequestAsync(context) ?? Task.CompletedTask;
    }

    /// <inheritdoc/>
    Task IOutputCachingPolicy.OnServeFromCacheAsync(OutputCachingContext context)
    {
        return CreatePolicy(context)?.OnServeFromCacheAsync(context) ?? Task.CompletedTask;
    }

    /// <inheritdoc/>
    Task IOutputCachingPolicy.OnServeResponseAsync(OutputCachingContext context)
    {
        return CreatePolicy(context)?.OnServeResponseAsync(context) ?? Task.CompletedTask;
    }
}
