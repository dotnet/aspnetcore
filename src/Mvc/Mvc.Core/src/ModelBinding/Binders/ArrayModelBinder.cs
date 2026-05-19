// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

/// <summary>
/// <see cref="IModelBinder"/> implementation for binding array values.
/// </summary>
/// <typeparam name="TElement">Type of elements in the array.</typeparam>
public class ArrayModelBinder<TElement> : CollectionModelBinder<TElement>
{
    /// <summary>
    /// Creates a new <see cref="ArrayModelBinder{TElement}"/>.
    /// </summary>
    /// <param name="elementBinder">
    /// The <see cref="IModelBinder"/> for binding <typeparamref name="TElement"/>.
    /// </param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    public ArrayModelBinder(IModelBinder elementBinder, ILoggerFactory loggerFactory)
        : base(elementBinder, loggerFactory)
    {
    }

    /// <summary>
    /// Creates a new <see cref="ArrayModelBinder{TElement}"/>.
    /// </summary>
    /// <param name="elementBinder">
    /// The <see cref="IModelBinder"/> for binding <typeparamref name="TElement"/>.
    /// </param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    /// <param name="allowValidatingTopLevelNodes">
    /// Indication that validation of top-level models is enabled. If <see langword="true"/> and
    /// <see cref="ModelMetadata.IsBindingRequired"/> is <see langword="true"/> for a top-level model, the binder
    /// adds a <see cref="ModelStateDictionary"/> error when the model is not bound.
    /// </param>
    /// <remarks>
    /// The <paramref name="allowValidatingTopLevelNodes"/> parameter is currently ignored.
    /// <see cref="CollectionModelBinder{TElement}.AllowValidatingTopLevelNodes"/> is always <see langword="true"/>
    /// in <see cref="ArrayModelBinder{TElement}"/>.
    /// </remarks>
    public ArrayModelBinder(
        IModelBinder elementBinder,
        ILoggerFactory loggerFactory,
        bool allowValidatingTopLevelNodes)
        : base(elementBinder, loggerFactory, allowValidatingTopLevelNodes: true)
    {
    }

    /// <summary>
    /// Creates a new <see cref="ArrayModelBinder{TElement}"/>.
    /// </summary>
    /// <param name="elementBinder">
    /// The <see cref="IModelBinder"/> for binding <typeparamref name="TElement"/>.
    /// </param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    /// <param name="allowValidatingTopLevelNodes">
    /// Indication that validation of top-level models is enabled. If <see langword="true"/> and
    /// <see cref="ModelMetadata.IsBindingRequired"/> is <see langword="true"/> for a top-level model, the binder
    /// adds a <see cref="ModelStateDictionary"/> error when the model is not bound.
    /// </param>
    /// <param name="mvcOptions">The <see cref="MvcOptions"/>.</param>
    /// <remarks>
    /// <para>This is the preferred <see cref="ArrayModelBinder{TElement}"/> constructor.</para>
    /// <para>
    /// The <paramref name="allowValidatingTopLevelNodes"/> parameter is currently ignored.
    /// <see cref="CollectionModelBinder{TElement}.AllowValidatingTopLevelNodes"/> is always <see langword="true"/>
    /// in <see cref="ArrayModelBinder{TElement}"/>.
    /// </para>
    /// </remarks>
    public ArrayModelBinder(
        IModelBinder elementBinder,
        ILoggerFactory loggerFactory,
        bool allowValidatingTopLevelNodes,
        MvcOptions mvcOptions)
        : base(elementBinder, loggerFactory, allowValidatingTopLevelNodes: true, mvcOptions)
    {
    }

    /// <inheritdoc />
    public override bool CanCreateInstance(Type targetType)
    {
        return targetType == typeof(TElement[]);
    }

    /// <inheritdoc />
    protected override object CreateEmptyCollection(Type targetType)
    {
        Debug.Assert(targetType == typeof(TElement[]), "GenericModelBinder only creates this binder for arrays.");

        return Array.Empty<TElement>();
    }

    /// <inheritdoc />
    protected override object? ConvertToCollectionType(Type targetType, IEnumerable<TElement?> collection)
    {
        Debug.Assert(targetType == typeof(TElement?[]), "GenericModelBinder only creates this binder for arrays.");

        // If non-null, collection is a List<TElement>, never already a TElement[].
        return collection?.ToArray();
    }

    /// <inheritdoc />
    protected override void CopyToModel(object target, IEnumerable<TElement?> sourceCollection)
    {
        ArgumentNullException.ThrowIfNull(target);

        // Do not attempt to copy values into an array because an array's length is immutable. This choice is also
        // consistent with our handling of a read-only array property.
    }
}
