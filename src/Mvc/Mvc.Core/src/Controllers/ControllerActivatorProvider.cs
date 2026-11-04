// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.Controllers;

/// <summary>
/// Provides methods to create an MVC controller.
/// </summary>
public class ControllerActivatorProvider : IControllerActivatorProvider
{
    private static readonly Action<ControllerContext, object> _dispose = Dispose;
    private static readonly Func<ControllerContext, object, ValueTask> _disposeAsync = DisposeAsync;
    private static readonly Func<ControllerContext, object, ValueTask> _syncDisposeAsync = SyncDisposeAsync;
    private readonly Func<ControllerContext, object>? _controllerActivatorCreate;
    private readonly Action<ControllerContext, object>? _controllerActivatorRelease;
    private readonly Func<ControllerContext, object, ValueTask>? _controllerActivatorReleaseAsync;

    /// <summary>
    /// Initializes a new instance of <see cref="ControllerActivatorProvider"/>.
    /// </summary>
    /// <param name="controllerActivator">A <see cref="IControllerActivator"/> which is delegated to when not the default implementation.</param>
    public ControllerActivatorProvider(IControllerActivator controllerActivator)
    {
        ArgumentNullException.ThrowIfNull(controllerActivator);

        // Compat: Delegate to controllerActivator if it's not the default implementation.
        if (controllerActivator.GetType() != typeof(DefaultControllerActivator))
        {
            _controllerActivatorCreate = controllerActivator.Create;
            _controllerActivatorRelease = controllerActivator.Release;
            _controllerActivatorReleaseAsync = controllerActivator.ReleaseAsync;
        }
    }

    /// <inheritdoc/>
    public Func<ControllerContext, object> CreateActivator(ControllerActionDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        var controllerType = descriptor.ControllerTypeInfo?.AsType();
        if (controllerType == null)
        {
            throw new ArgumentException(Resources.FormatPropertyOfTypeCannotBeNull(
                nameof(descriptor.ControllerTypeInfo),
                nameof(descriptor)),
                nameof(descriptor));
        }

        if (_controllerActivatorCreate != null)
        {
            return _controllerActivatorCreate;
        }

        var typeActivator = ActivatorUtilities.CreateFactory(controllerType, Type.EmptyTypes);
        return controllerContext => typeActivator(controllerContext.HttpContext.RequestServices, arguments: null);
    }

    /// <inheritdoc/>
    public Action<ControllerContext, object>? CreateReleaser(ControllerActionDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        if (_controllerActivatorRelease != null)
        {
            return _controllerActivatorRelease;
        }

        if (typeof(IDisposable).GetTypeInfo().IsAssignableFrom(descriptor.ControllerTypeInfo))
        {
            return _dispose;
        }

        return null;
    }

    /// <inheritdoc/>
    public Func<ControllerContext, object, ValueTask>? CreateAsyncReleaser(ControllerActionDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        if (_controllerActivatorReleaseAsync != null)
        {
            return _controllerActivatorReleaseAsync;
        }

        if (typeof(IAsyncDisposable).GetTypeInfo().IsAssignableFrom(descriptor.ControllerTypeInfo))
        {
            return _disposeAsync;
        }

        if (typeof(IDisposable).GetTypeInfo().IsAssignableFrom(descriptor.ControllerTypeInfo))
        {
            return _syncDisposeAsync;
        }

        return null;
    }

    private static void Dispose(ControllerContext context, object controller)
    {
        ArgumentNullException.ThrowIfNull(controller);

        ((IDisposable)controller).Dispose();
    }

    private static ValueTask DisposeAsync(ControllerContext context, object controller)
    {
        ArgumentNullException.ThrowIfNull(controller);

        return ((IAsyncDisposable)controller).DisposeAsync();
    }

    private static ValueTask SyncDisposeAsync(ControllerContext context, object controller)
    {
        Dispose(context, controller);
        return default;
    }
}
