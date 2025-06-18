// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Base class for filtering component state persistence based on specific persistence reasons.
/// </summary>
/// <typeparam name="TReason">The type of persistence reason this filter handles.</typeparam>
public abstract class PersistReasonFilter<TReason> : Attribute, IPersistenceReasonFilter
    where TReason : IPersistenceReason
{
    private readonly bool _persist;

    /// <summary>
    /// Initializes a new instance of the <see cref="PersistReasonFilter{TReason}"/> class.
    /// </summary>
    /// <param name="persist">Whether to persist state for the specified reason type.</param>
    protected PersistReasonFilter(bool persist)
    {
        _persist = persist;
    }

    /// <inheritdoc />
    public bool? ShouldPersist(IPersistenceReason reason)
    {
        if (reason is TReason)
        {
            return _persist;
        }

        return null;
    }
}