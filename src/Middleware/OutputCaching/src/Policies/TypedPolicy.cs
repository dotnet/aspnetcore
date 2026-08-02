// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.OutputCaching.Policies;

/// <summary>
/// A type base policy.
/// </summary>
internal sealed class TypedPolicy : IOutputCachePolicy
{
    private IOutputCachePolicy? _instance;

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

    private IOutputCachePolicy? CreatePolicy(OutputCacheContext context)
    {
        var options = context.HttpContext.RequestServices.GetRequiredService<IOptions<OutputCacheOptions>>();
        return _instance ??= ActivatorUtilities.CreateInstance(options.Value.ApplicationServices, _policyType) as IOutputCachePolicy;
    }

    /// <inheritdoc/>
    ValueTask IOutputCachePolicy.CacheRequestAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        return CreatePolicy(context)?.CacheRequestAsync(context, cancellationToken) ?? ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    ValueTask IOutputCachePolicy.ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        return CreatePolicy(context)?.ServeFromCacheAsync(context, cancellationToken) ?? ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    ValueTask IOutputCachePolicy.ServeResponseAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        return CreatePolicy(context)?.ServeResponseAsync(context, cancellationToken) ?? ValueTask.CompletedTask;
    }
}
