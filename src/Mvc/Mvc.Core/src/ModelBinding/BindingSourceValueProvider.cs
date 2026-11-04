// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Mvc.Core;

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

/// <summary>
/// A value provider which provides data from a specific <see cref="BindingSource"/>.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="BindingSourceValueProvider"/> is an <see cref="IValueProvider"/> base-implementation which
/// can provide data for all parameters and model properties which specify the corresponding
/// <see cref="ModelBinding.BindingSource"/>.
/// </para>
/// <para>
/// <see cref="BindingSourceValueProvider"/> implements <see cref="IBindingSourceValueProvider"/> and will
/// include or exclude itself from the set of value providers based on the model's associated
/// <see cref="ModelBinding.BindingSource"/>. Value providers are by-default included; if a model does not
/// specify a <see cref="ModelBinding.BindingSource"/> then all value providers are valid.
/// </para>
/// </remarks>
public abstract class BindingSourceValueProvider : IBindingSourceValueProvider
{
    /// <summary>
    /// Creates a new <see cref="BindingSourceValueProvider"/>.
    /// </summary>
    /// <param name="bindingSource">
    /// The <see cref="ModelBinding.BindingSource"/>. Must be a single-source (non-composite) with
    /// <see cref="ModelBinding.BindingSource.IsGreedy"/> equal to <c>false</c>.
    /// </param>
    public BindingSourceValueProvider(BindingSource bindingSource)
    {
        ArgumentNullException.ThrowIfNull(bindingSource);

        if (bindingSource.IsGreedy)
        {
            var message = Resources.FormatBindingSource_CannotBeGreedy(
                bindingSource.DisplayName,
                nameof(BindingSourceValueProvider));
            throw new ArgumentException(message, nameof(bindingSource));
        }

        if (bindingSource is CompositeBindingSource)
        {
            var message = Resources.FormatBindingSource_CannotBeComposite(
                bindingSource.DisplayName,
                nameof(BindingSourceValueProvider));
            throw new ArgumentException(message, nameof(bindingSource));
        }

        BindingSource = bindingSource;
    }

    /// <summary>
    /// Gets the corresponding <see cref="ModelBinding.BindingSource"/>.
    /// </summary>
    protected BindingSource BindingSource { get; }

    /// <inheritdoc />
    public abstract bool ContainsPrefix(string prefix);

    /// <inheritdoc />
    public abstract ValueProviderResult GetValue(string key);

    /// <inheritdoc />
    public virtual IValueProvider? Filter(BindingSource bindingSource)
    {
        ArgumentNullException.ThrowIfNull(bindingSource);

        if (bindingSource.CanAcceptDataFrom(BindingSource))
        {
            return this;
        }
        else
        {
            return null;
        }
    }
}
