// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc.Core;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Extension methods for adding filters to the global filters collection.
    /// </summary>
    public static class FilterCollectionExtensions
    {
        /// <summary>
        /// Adds a type representing an <see cref="IFilter"/> to a filter collection.
        /// </summary>
        /// <param name="filters">A collection of <see cref="IFilter"/>.</param>
        /// <param name="filterType">Type representing an <see cref="IFilter"/>.</param>
        /// <returns>An <see cref="IFilter"/> representing the added type.</returns>
        /// <remarks>
        /// Filter instances will be created using <see cref="Microsoft.Framework.DependencyInjection.ITypeActivator"/>.
        /// Use <see cref="AddService(ICollection{IFilter}, Type)"/> to register a service as a filter.
        /// </remarks>
        public static IFilter Add(
            [NotNull] this ICollection<IFilter> filters,
            [NotNull] Type filterType)
        {
            return Add(filters, filterType, order: 0);
        }

        /// <summary>
        /// Adds a type representing an <see cref="IFilter"/> to a filter collection.
        /// </summary>
        /// <param name="filters">A collection of <see cref="IFilter"/>.</param>
        /// <param name="filterType">Type representing an <see cref="IFilter"/>.</param>
        /// <param name="order">The order of the added filter.</param>
        /// <returns>An <see cref="IFilter"/> representing the added type.</returns>
        /// <remarks>
        /// Filter instances will be created using <see cref="Microsoft.Framework.DependencyInjection.ITypeActivator"/>.
        /// Use <see cref="AddService(ICollection{IFilter}, Type)"/> to register a service as a filter.
        /// </remarks>
        public static IFilter Add(
            [NotNull] this ICollection<IFilter> filters,
            [NotNull] Type filterType,
            int order)
        {
            if (!typeof(IFilter).IsAssignableFrom(filterType))
            {
                var message = Resources.FormatTypeMustDeriveFromType(filterType.FullName, typeof(IFilter).FullName);
                throw new ArgumentException(message, nameof(filterType));
            }

            var filter = new TypeFilterAttribute(filterType) { Order = order };
            filters.Add(filter);
            return filter;
        }

        /// <summary>
        /// Adds a type representing an <see cref="IFilter"/> to a filter collection.
        /// </summary>
        /// <param name="filters">A collection of <see cref="IFilter"/>.</param>
        /// <param name="filterType">Type representing an <see cref="IFilter"/>.</param>
        /// <returns>An <see cref="IFilter"/> representing the added service type.</returns>
        /// <remarks>
        /// Filter instances will created through dependency injection. Use 
        /// <see cref="AddService(ICollection{IFilter}, Type)"/> to register a service that will be created via
        /// type activation.
        /// </remarks>
        public static IFilter AddService(
            [NotNull] this ICollection<IFilter> filters,
            [NotNull] Type filterType)
        {
            return AddService(filters, filterType, order: 0);
        }

        /// <summary>
        /// Adds a type representing an <see cref="IFilter"/> to a filter collection.
        /// </summary>
        /// <param name="filters">A collection of <see cref="IFilter"/>.</param>
        /// <param name="filterType">Type representing an <see cref="IFilter"/>.</param>
        /// <param name="order">The order of the added filter.</param>
        /// <returns>An <see cref="IFilter"/> representing the added service type.</returns>
        /// <remarks>
        /// Filter instances will created through dependency injection. Use 
        /// <see cref="AddService(ICollection{IFilter}, Type)"/> to register a service that will be created via
        /// type activation.
        /// </remarks>
        public static IFilter AddService(
            [NotNull] this ICollection<IFilter> filters,
            [NotNull] Type filterType,
            int order)
        {
            if (!typeof(IFilter).IsAssignableFrom(filterType))
            {
                var message = Resources.FormatTypeMustDeriveFromType(filterType.FullName, typeof(IFilter).FullName);
                throw new ArgumentException(message, nameof(filterType));
            }

            var filter = new ServiceFilterAttribute(filterType) { Order = order };
            filters.Add(filter);
            return filter;
        }
    }
}