// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

/// <summary>
/// <see cref="IPageActivatorProvider"/> that uses type activation to create Razor Page instances.
/// </summary>
internal sealed class DefaultPageModelActivatorProvider : IPageModelActivatorProvider
{
    private readonly Action<PageContext, object> _disposer = Dispose;
    private readonly Func<PageContext, object, ValueTask> _asyncDisposer = DisposeAsync;
    private readonly Func<PageContext, object, ValueTask> _syncAsyncDisposer = SyncDisposeAsync;

    /// <inheritdoc />
    public Func<PageContext, object> CreateActivator(CompiledPageActionDescriptor actionDescriptor)
    {
        ArgumentNullException.ThrowIfNull(actionDescriptor);

        var modelTypeInfo = actionDescriptor.ModelTypeInfo?.AsType();
        if (modelTypeInfo == null)
        {
            throw new ArgumentException(Resources.FormatPropertyOfTypeCannotBeNull(
                nameof(actionDescriptor.ModelTypeInfo),
                nameof(actionDescriptor)),
                nameof(actionDescriptor));
        }

        var factory = ActivatorUtilities.CreateFactory(modelTypeInfo, Type.EmptyTypes);
        return (context) => factory(context.HttpContext.RequestServices, Array.Empty<object>());
    }

    public Action<PageContext, object>? CreateReleaser(CompiledPageActionDescriptor actionDescriptor)
    {
        ArgumentNullException.ThrowIfNull(actionDescriptor);

        if (typeof(IDisposable).GetTypeInfo().IsAssignableFrom(actionDescriptor.ModelTypeInfo))
        {
            return _disposer;
        }

        return null;
    }

    public Func<PageContext, object, ValueTask>? CreateAsyncReleaser(CompiledPageActionDescriptor actionDescriptor)
    {
        ArgumentNullException.ThrowIfNull(actionDescriptor);

        if (typeof(IAsyncDisposable).GetTypeInfo().IsAssignableFrom(actionDescriptor.ModelTypeInfo))
        {
            return _asyncDisposer;
        }

        if (typeof(IDisposable).GetTypeInfo().IsAssignableFrom(actionDescriptor.ModelTypeInfo))
        {
            return _syncAsyncDisposer;
        }

        return null;
    }

    private static void Dispose(PageContext context, object page)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(page);

        ((IDisposable)page).Dispose();
    }

    private static ValueTask DisposeAsync(PageContext context, object page)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(page);

        return ((IAsyncDisposable)page).DisposeAsync();
    }

    private static ValueTask SyncDisposeAsync(PageContext context, object page)
    {
        Dispose(context, page);
        return default;
    }
}
