// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

/// <summary>
/// A value provider which can filter its contents based on <see cref="BindingSource"/>.
/// </summary>
/// <remarks>
/// Value providers are by-default included. If a model does not specify a <see cref="BindingSource"/>
/// then all value providers are valid.
/// </remarks>
public interface IBindingSourceValueProvider : IValueProvider
{
    /// <summary>
    /// Filters the value provider based on <paramref name="bindingSource"/>.
    /// </summary>
    /// <param name="bindingSource">The <see cref="BindingSource"/> associated with a model.</param>
    /// <returns>
    /// The filtered value provider, or <c>null</c> if the value provider does not match
    /// <paramref name="bindingSource"/>.
    /// </returns>
    IValueProvider? Filter(BindingSource bindingSource);
}
