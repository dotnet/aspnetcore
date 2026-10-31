// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// A filter that creates another filter of type <see cref="ImplementationType"/>, retrieving missing constructor
/// arguments from dependency injection if available there.
/// </summary>
/// <remarks>
/// <para>
/// Primarily used in <see cref="M:FilterCollection.Add"/> calls.
/// </para>
/// <para>
/// Similar to the <see cref="ServiceFilterAttribute"/> in that both use constructor injection. Use
/// <see cref="ServiceFilterAttribute"/> instead if the filter is itself a service.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
[DebuggerDisplay("Type = {ImplementationType}, Order = {Order}")]
public class TypeFilterAttribute : Attribute, IFilterFactory, IOrderedFilter
{
    private ObjectFactory? _factory;

    /// <summary>
    /// Instantiates a new <see cref="TypeFilterAttribute"/> instance.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> of filter to create.</param>
    public TypeFilterAttribute(Type type)
    {
        ImplementationType = type ?? throw new ArgumentNullException(nameof(type));
    }

    /// <summary>
    /// Gets or sets the non-service arguments to pass to the <see cref="ImplementationType"/> constructor.
    /// </summary>
    /// <remarks>
    /// Service arguments are found in the dependency injection container i.e. this filter supports constructor
    /// injection in addition to passing the given <see cref="Arguments"/>.
    /// </remarks>
    public object[]? Arguments { get; set; }

    /// <summary>
    /// Gets the <see cref="Type"/> of filter to create.
    /// </summary>
    public Type ImplementationType { get; }

    /// <inheritdoc />
    public int Order { get; set; }

    /// <inheritdoc />
    public bool IsReusable { get; set; }

    /// <inheritdoc />
    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        if (_factory == null)
        {
            var argumentTypes = Arguments?.Select(a => a.GetType())?.ToArray();
            _factory = ActivatorUtilities.CreateFactory(ImplementationType, argumentTypes ?? Type.EmptyTypes);
        }

        var filter = (IFilterMetadata)_factory(serviceProvider, Arguments);
        if (filter is IFilterFactory filterFactory)
        {
            // Unwrap filter factories
            filter = filterFactory.CreateInstance(serviceProvider);
        }

        return filter;
    }
}
