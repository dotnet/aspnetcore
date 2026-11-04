// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Microsoft.AspNetCore.Mvc.ViewComponents;

/// <summary>
/// A default implementation of <see cref="IViewComponentActivator"/>.
/// </summary>
/// <remarks>
/// The <see cref="DefaultViewComponentActivator"/> can provide the current instance of
/// <see cref="ViewComponentContext"/> to a public property of a view component marked
/// with <see cref="ViewComponentContextAttribute"/>.
/// </remarks>
internal sealed class DefaultViewComponentActivator : IViewComponentActivator
{
    private readonly ITypeActivatorCache _typeActivatorCache;

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultViewComponentActivator"/> class.
    /// </summary>
    /// <param name="typeActivatorCache">
    /// The <see cref="ITypeActivatorCache"/> used to create new view component instances.
    /// </param>
    public DefaultViewComponentActivator(ITypeActivatorCache typeActivatorCache)
    {
        ArgumentNullException.ThrowIfNull(typeActivatorCache);

        _typeActivatorCache = typeActivatorCache;
    }

    /// <inheritdoc />
    public object Create(ViewComponentContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var componentType = context.ViewComponentDescriptor.TypeInfo;

        if (componentType == null)
        {
            throw new ArgumentException(Resources.FormatPropertyOfTypeCannotBeNull(
                nameof(context.ViewComponentDescriptor.TypeInfo),
                nameof(context.ViewComponentDescriptor)));
        }

        var viewComponent = _typeActivatorCache.CreateInstance<object>(
            context.ViewContext.HttpContext.RequestServices,
            context.ViewComponentDescriptor.TypeInfo.AsType());

        return viewComponent;
    }

    /// <inheritdoc />
    public void Release(ViewComponentContext context, object viewComponent)
    {
        if (context == null)
        {
            throw new InvalidOperationException(nameof(context));
        }

        if (viewComponent == null)
        {
            throw new InvalidOperationException(nameof(viewComponent));
        }

        if (viewComponent is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    public ValueTask ReleaseAsync(ViewComponentContext context, object viewComponent)
    {
        if (context == null)
        {
            throw new InvalidOperationException(nameof(context));
        }

        if (viewComponent == null)
        {
            throw new InvalidOperationException(nameof(viewComponent));
        }

        if (viewComponent is IAsyncDisposable disposable)
        {
            return disposable.DisposeAsync();
        }

        Release(context, viewComponent);
        return default;
    }
}
