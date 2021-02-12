// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    /// <summary>
    /// A <see cref="IValueProviderFactory"/> for creating <see cref="RouteValueProvider"/> instances.
    /// </summary>
    public class RouteValueProviderFactory : IValueProviderFactory
    {
        /// <inheritdoc />
        public Task CreateValueProviderAsync(ValueProviderFactoryContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var valueProvider = new RouteValueProvider(
                BindingSource.Path,
                context.ActionContext.RouteData.Values);

            context.ValueProviders.Add(valueProvider);

            return Task.CompletedTask;
        }
    }
}
