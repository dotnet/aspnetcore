// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    /// <summary>
    /// A factory for creating <see cref="IValueProvider"/> instances.
    /// </summary>
    public interface IValueProviderFactory
    {
        /// <summary>
        /// Creates a <see cref="IValueProvider"/> with values from the current request
        /// and adds it to <see cref="ValueProviderFactoryContext.ValueProviders"/> list.
        /// </summary>
        /// <param name="context">The <see cref="ValueProviderFactoryContext"/>.</param>
        /// <returns>A <see cref="Task"/> that when completed will add an <see cref="IValueProvider"/> instance
        /// to <see cref="ValueProviderFactoryContext.ValueProviders"/> list if applicable.</returns>
        Task CreateValueProviderAsync(ValueProviderFactoryContext context);
    }
}
