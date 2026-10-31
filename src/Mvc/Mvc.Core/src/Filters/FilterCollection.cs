// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Mvc.Core;

namespace Microsoft.AspNetCore.Mvc.Filters;

/// <summary>
/// A collection of <see cref="IFilterMetadata"/>.
/// </summary>
public class FilterCollection : Collection<IFilterMetadata>
{
    /// <summary>
    /// Adds a type representing a <see cref="IFilterMetadata"/>.
    /// </summary>
    /// <typeparam name="TFilterType">Type representing a <see cref="IFilterMetadata"/>.</typeparam>
    /// <returns>A <see cref="IFilterMetadata"/> representing the added type.</returns>
    /// <remarks>
    /// Filter instances will be created using
    /// <see cref="Microsoft.Extensions.DependencyInjection.ActivatorUtilities"/>.
    /// Use <see cref="AddService(Type)"/> to register a service as a filter.
    /// The added filter will be assigned an order of 0.
    /// </remarks>
    public IFilterMetadata Add<TFilterType>() where TFilterType : IFilterMetadata
    {
        return Add(typeof(TFilterType));
    }

    /// <summary>
    /// Adds a type representing a <see cref="IFilterMetadata"/>.
    /// </summary>
    /// <param name="filterType">Type representing a <see cref="IFilterMetadata"/>.</param>
    /// <returns>A <see cref="IFilterMetadata"/> representing the added type.</returns>
    /// <remarks>
    /// Filter instances will be created using
    /// <see cref="Microsoft.Extensions.DependencyInjection.ActivatorUtilities"/>.
    /// Use <see cref="AddService(Type)"/> to register a service as a filter.
    /// The added filter will be assigned an order of 0.
    /// </remarks>
    public IFilterMetadata Add(Type filterType)
    {
        ArgumentNullException.ThrowIfNull(filterType);

        return Add(filterType, order: 0);
    }

    /// <summary>
    /// Adds a type representing a <see cref="IFilterMetadata"/>.
    /// </summary>
    /// <typeparam name="TFilterType">Type representing a <see cref="IFilterMetadata"/>.</typeparam>
    /// <param name="order">The order of the added filter.</param>
    /// <returns>A <see cref="IFilterMetadata"/> representing the added type.</returns>
    /// <remarks>
    /// Filter instances will be created using
    /// <see cref="Microsoft.Extensions.DependencyInjection.ActivatorUtilities"/>.
    /// Use <see cref="AddService(Type)"/> to register a service as a filter.
    /// </remarks>
    public IFilterMetadata Add<TFilterType>(int order) where TFilterType : IFilterMetadata
    {
        return Add(typeof(TFilterType), order);
    }

    /// <summary>
    /// Adds a type representing a <see cref="IFilterMetadata"/>.
    /// </summary>
    /// <param name="filterType">Type representing a <see cref="IFilterMetadata"/>.</param>
    /// <param name="order">The order of the added filter.</param>
    /// <returns>A <see cref="IFilterMetadata"/> representing the added type.</returns>
    /// <remarks>
    /// Filter instances will be created using
    /// <see cref="Microsoft.Extensions.DependencyInjection.ActivatorUtilities"/>.
    /// Use <see cref="AddService(Type)"/> to register a service as a filter.
    /// </remarks>
    public IFilterMetadata Add(Type filterType, int order)
    {
        ArgumentNullException.ThrowIfNull(filterType);

        if (!typeof(IFilterMetadata).IsAssignableFrom(filterType))
        {
            var message = Resources.FormatTypeMustDeriveFromType(
                filterType.FullName,
                typeof(IFilterMetadata).FullName);
            throw new ArgumentException(message, nameof(filterType));
        }

        var filter = new TypeFilterAttribute(filterType) { Order = order };
        Add(filter);
        return filter;
    }

    /// <summary>
    /// Adds a type representing a <see cref="IFilterMetadata"/>.
    /// </summary>
    /// <typeparam name="TFilterType">Type representing a <see cref="IFilterMetadata"/>.</typeparam>
    /// <returns>A <see cref="IFilterMetadata"/> representing the added service type.</returns>
    /// <remarks>
    /// Filter instances will be created through dependency injection. Use
    /// <see cref="Add(Type)"/> to register a service that will be created via
    /// type activation.
    /// The added filter will be assigned an order of 0.
    /// </remarks>
    public IFilterMetadata AddService<TFilterType>() where TFilterType : IFilterMetadata
    {
        return AddService(typeof(TFilterType));
    }

    /// <summary>
    /// Adds a type representing a <see cref="IFilterMetadata"/>.
    /// </summary>
    /// <param name="filterType">Type representing a <see cref="IFilterMetadata"/>.</param>
    /// <returns>A <see cref="IFilterMetadata"/> representing the added service type.</returns>
    /// <remarks>
    /// Filter instances will be created through dependency injection. Use
    /// <see cref="Add(Type)"/> to register a service that will be created via
    /// type activation.
    /// The added filter will be assigned an order of 0.
    /// </remarks>
    public IFilterMetadata AddService(Type filterType)
    {
        ArgumentNullException.ThrowIfNull(filterType);

        return AddService(filterType, order: 0);
    }

    /// <summary>
    /// Adds a type representing a <see cref="IFilterMetadata"/>.
    /// </summary>
    /// <typeparam name="TFilterType">Type representing a <see cref="IFilterMetadata"/>.</typeparam>
    /// <param name="order">The order of the added filter.</param>
    /// <returns>A <see cref="IFilterMetadata"/> representing the added service type.</returns>
    /// <remarks>
    /// Filter instances will be created through dependency injection. Use
    /// <see cref="Add(Type)"/> to register a service that will be created via
    /// type activation.
    /// </remarks>
    public IFilterMetadata AddService<TFilterType>(int order) where TFilterType : IFilterMetadata
    {
        return AddService(typeof(TFilterType), order);
    }

    /// <summary>
    /// Adds a type representing a <see cref="IFilterMetadata"/>.
    /// </summary>
    /// <param name="filterType">Type representing a <see cref="IFilterMetadata"/>.</param>
    /// <param name="order">The order of the added filter.</param>
    /// <returns>A <see cref="IFilterMetadata"/> representing the added service type.</returns>
    /// <remarks>
    /// Filter instances will be created through dependency injection. Use
    /// <see cref="Add(Type)"/> to register a service that will be created via
    /// type activation.
    /// </remarks>
    public IFilterMetadata AddService(Type filterType, int order)
    {
        ArgumentNullException.ThrowIfNull(filterType);

        if (!typeof(IFilterMetadata).IsAssignableFrom(filterType))
        {
            var message = Resources.FormatTypeMustDeriveFromType(
                filterType.FullName,
                typeof(IFilterMetadata).FullName);
            throw new ArgumentException(message, nameof(filterType));
        }

        var filter = new ServiceFilterAttribute(filterType) { Order = order };
        Add(filter);
        return filter;
    }
}
