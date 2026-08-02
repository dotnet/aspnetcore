// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Mvc.Filters;

/// <summary>
/// An abstract context for filters.
/// </summary>
public abstract class FilterContext : ActionContext
{
    /// <summary>
    /// Instantiates a new <see cref="FilterContext"/> instance.
    /// </summary>
    /// <param name="actionContext">The <see cref="ActionContext"/>.</param>
    /// <param name="filters">All applicable <see cref="IFilterMetadata"/> implementations.</param>
    public FilterContext(
        ActionContext actionContext,
        IList<IFilterMetadata> filters)
        : base(actionContext)
    {
        ArgumentNullException.ThrowIfNull(filters);

        Filters = filters;
    }

    /// <summary>
    /// Gets all applicable <see cref="IFilterMetadata"/> implementations.
    /// </summary>
    public virtual IList<IFilterMetadata> Filters { get; }

    /// <summary>
    /// Returns a value indicating whether the provided <see cref="IFilterMetadata"/> is the most effective
    /// policy (most specific) applied to the action associated with the <see cref="FilterContext"/>.
    /// </summary>
    /// <typeparam name="TMetadata">The type of the filter policy.</typeparam>
    /// <param name="policy">The filter policy instance.</param>
    /// <returns>
    /// <c>true</c> if the provided <see cref="IFilterMetadata"/> is the most effective policy, otherwise <c>false</c>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The <see cref="IsEffectivePolicy{TMetadata}(TMetadata)"/> method is used to implement a common convention
    /// for filters that define an overriding behavior. When multiple filters may apply to the same
    /// cross-cutting concern, define a common interface for the filters (<typeparamref name="TMetadata"/>) and
    /// implement the filters such that all of the implementations call this method to determine if they should
    /// take action.
    /// </para>
    /// <para>
    /// For instance, a global filter might be overridden by placing a filter attribute on an action method.
    /// The policy applied directly to the action method could be considered more specific.
    /// </para>
    /// <para>
    /// This mechanism for overriding relies on the rules of order and scope that the filter system
    /// provides to control ordering of filters. It is up to the implementor of filters to implement this
    /// protocol cooperatively. The filter system has no innate notion of overrides, this is a recommended
    /// convention.
    /// </para>
    /// </remarks>
    public bool IsEffectivePolicy<TMetadata>(TMetadata policy) where TMetadata : IFilterMetadata
    {
        if (policy == null)
        {
            throw new ArgumentNullException(nameof(policy));
        }

        var effective = FindEffectivePolicy<TMetadata>();
        return ReferenceEquals(policy, effective);
    }

    /// <summary>
    /// Returns the most effective (most specific) policy of type <typeparamref name="TMetadata"/> applied to
    /// the action associated with the <see cref="FilterContext"/>.
    /// </summary>
    /// <typeparam name="TMetadata">The type of the filter policy.</typeparam>
    /// <returns>The implementation of <typeparamref name="TMetadata"/> applied to the action associated with
    /// the <see cref="FilterContext"/>
    /// </returns>
    [return: MaybeNull]
    public TMetadata FindEffectivePolicy<TMetadata>() where TMetadata : IFilterMetadata
    {
        // The most specific policy is the one closest to the action (nearest the end of the list).
        for (var i = Filters.Count - 1; i >= 0; i--)
        {
            var filter = Filters[i];
            if (filter is TMetadata match)
            {
                return match;
            }
        }

        return default;
    }
}
