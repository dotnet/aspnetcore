// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// A filter that finds another filter in an <see cref="IServiceProvider"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Primarily used in <see cref="M:FilterCollection.AddService"/> calls.
    /// </para>
    /// <para>
    /// Similar to the <see cref="TypeFilterAttribute"/> in that both use constructor injection. Use
    /// <see cref="TypeFilterAttribute"/> instead if the filter is not itself a service.
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    [DebuggerDisplay("ServiceFilter: Type={ServiceType} Order={Order}")]
    public class ServiceFilterAttribute : Attribute, IFilterFactory, IOrderedFilter
    {
        /// <summary>
        /// Instantiates a new <see cref="ServiceFilterAttribute"/> instance.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> of filter to find.</param>
        public ServiceFilterAttribute(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            ServiceType = type;
        }

        /// <inheritdoc />
        public int Order { get; set; }

        /// <summary>
        /// Gets the <see cref="Type"/> of filter to find.
        /// </summary>
        public Type ServiceType { get; }

        /// <inheritdoc />
        public bool IsReusable { get; set; }

        /// <inheritdoc />
        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            var filter = (IFilterMetadata)serviceProvider.GetRequiredService(ServiceType);
            if (filter is IFilterFactory filterFactory)
            {
                // Unwrap filter factories
                filter = filterFactory.CreateInstance(serviceProvider);
            }

            return filter;
        }
    }
}
