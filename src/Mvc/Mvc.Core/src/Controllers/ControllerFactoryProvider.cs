// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Mvc.Core;

namespace Microsoft.AspNetCore.Mvc.Controllers;

internal sealed class ControllerFactoryProvider : IControllerFactoryProvider
{
    private readonly IControllerActivatorProvider _activatorProvider;
    private readonly Func<ControllerContext, object>? _factoryCreateController;
    private readonly Action<ControllerContext, object>? _factoryReleaseController;
    private readonly Func<ControllerContext, object, ValueTask>? _factoryReleaseControllerAsync;
    private readonly IControllerPropertyActivator[] _propertyActivators;

    public ControllerFactoryProvider(
        IControllerActivatorProvider activatorProvider,
        IControllerFactory controllerFactory,
        IEnumerable<IControllerPropertyActivator> propertyActivators)
    {
        ArgumentNullException.ThrowIfNull(activatorProvider);
        ArgumentNullException.ThrowIfNull(controllerFactory);

        _activatorProvider = activatorProvider;

        // Compat: Delegate to the IControllerFactory if it's not the default implementation.
        if (controllerFactory.GetType() != typeof(DefaultControllerFactory))
        {
            _factoryCreateController = controllerFactory.CreateController;
            _factoryReleaseController = controllerFactory.ReleaseController;
            _factoryReleaseControllerAsync = controllerFactory.ReleaseControllerAsync;
        }

        _propertyActivators = propertyActivators.ToArray();
    }

    public Func<ControllerContext, object> CreateControllerFactory(ControllerActionDescriptor descriptor)
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

        if (_factoryCreateController != null)
        {
            return _factoryCreateController;
        }

        var controllerActivator = _activatorProvider.CreateActivator(descriptor);
        var propertyActivators = GetPropertiesToActivate(descriptor);
        object CreateController(ControllerContext controllerContext)
        {
            var controller = controllerActivator(controllerContext);
            for (var i = 0; i < propertyActivators.Length; i++)
            {
                var propertyActivator = propertyActivators[i];
                propertyActivator(controllerContext, controller);
            }

            return controller;
        }

        return CreateController;
    }

    public Action<ControllerContext, object>? CreateControllerReleaser(ControllerActionDescriptor descriptor)
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

        if (_factoryReleaseController != null)
        {
            return _factoryReleaseController;
        }

        return _activatorProvider.CreateReleaser(descriptor);
    }

    public Func<ControllerContext, object, ValueTask>? CreateAsyncControllerReleaser(ControllerActionDescriptor descriptor)
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

        if (_factoryReleaseControllerAsync != null)
        {
            return _factoryReleaseControllerAsync;
        }

        return _activatorProvider.CreateAsyncReleaser(descriptor);
    }

    private Action<ControllerContext, object>[] GetPropertiesToActivate(ControllerActionDescriptor actionDescriptor)
    {
        var propertyActivators = new Action<ControllerContext, object>[_propertyActivators.Length];
        for (var i = 0; i < _propertyActivators.Length; i++)
        {
            var activatorProvider = _propertyActivators[i];
            propertyActivators[i] = activatorProvider.GetActivatorDelegate(actionDescriptor);
        }

        return propertyActivators;
    }
}
