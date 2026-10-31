// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.Razor;

/// <summary>
/// Default implementation for <see cref="ITagHelperFactory"/>.
/// </summary>
internal sealed class DefaultTagHelperFactory : ITagHelperFactory
{
    private readonly ITagHelperActivator _activator;
    private readonly ConcurrentDictionary<Type, PropertyActivator<ViewContext>[]> _injectActions;
    private readonly Func<Type, PropertyActivator<ViewContext>[]> _getPropertiesToActivate;
    private static readonly Func<PropertyInfo, PropertyActivator<ViewContext>> _createActivateInfo = CreateActivateInfo;

    /// <summary>
    /// Initializes a new <see cref="DefaultTagHelperFactory"/> instance.
    /// </summary>
    /// <param name="activator">
    /// The <see cref="ITagHelperActivator"/> used to create tag helper instances.
    /// </param>
    public DefaultTagHelperFactory(ITagHelperActivator activator)
    {
        ArgumentNullException.ThrowIfNull(activator);

        _activator = activator;
        _injectActions = new ConcurrentDictionary<Type, PropertyActivator<ViewContext>[]>();
        _getPropertiesToActivate = type =>
            PropertyActivator<ViewContext>.GetPropertiesToActivate(
                type,
                typeof(ViewContextAttribute),
                _createActivateInfo);
    }

    internal void ClearCache()
    {
        _injectActions.Clear();
    }

    /// <inheritdoc />
    public TTagHelper CreateTagHelper<TTagHelper>(ViewContext context)
        where TTagHelper : ITagHelper
    {
        ArgumentNullException.ThrowIfNull(context);

        var tagHelper = _activator.Create<TTagHelper>(context);

        var propertiesToActivate = _injectActions.GetOrAdd(
            tagHelper.GetType(),
            _getPropertiesToActivate);

        for (var i = 0; i < propertiesToActivate.Length; i++)
        {
            var activateInfo = propertiesToActivate[i];
            activateInfo.Activate(tagHelper, context);
        }

        InitializeTagHelper(tagHelper, context);

        return tagHelper;
    }

    private static void InitializeTagHelper<TTagHelper>(TTagHelper tagHelper, ViewContext context)
        where TTagHelper : ITagHelper
    {
        // Run any tag helper initializers in the container
        var serviceProvider = context.HttpContext.RequestServices;
        var initializers = serviceProvider.GetService<IEnumerable<ITagHelperInitializer<TTagHelper>>>()!;

        foreach (var initializer in initializers)
        {
            initializer.Initialize(tagHelper, context);
        }
    }

    private static PropertyActivator<ViewContext> CreateActivateInfo(PropertyInfo property)
    {
        return new PropertyActivator<ViewContext>(property, viewContext => viewContext);
    }
}
