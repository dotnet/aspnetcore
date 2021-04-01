// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
