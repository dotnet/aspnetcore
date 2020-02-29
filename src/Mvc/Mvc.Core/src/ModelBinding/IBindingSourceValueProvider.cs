// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
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
        IValueProvider Filter(BindingSource bindingSource);
    }
}
