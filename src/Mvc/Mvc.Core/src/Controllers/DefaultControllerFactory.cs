// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Mvc.Core;

namespace Microsoft.AspNetCore.Mvc.Controllers;

/// <summary>
/// Default implementation for <see cref="IControllerFactory"/>.
/// </summary>
internal sealed class DefaultControllerFactory : IControllerFactory
{
    private readonly IControllerActivator _controllerActivator;
    private readonly IControllerPropertyActivator[] _propertyActivators;

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultControllerFactory"/>.
    /// </summary>
    /// <param name="controllerActivator">
    /// <see cref="IControllerActivator"/> used to create controller instances.
    /// </param>
    /// <param name="propertyActivators">
    /// A set of <see cref="IControllerPropertyActivator"/> instances used to initialize controller
    /// properties.
    /// </param>
    public DefaultControllerFactory(
        IControllerActivator controllerActivator,
        IEnumerable<IControllerPropertyActivator> propertyActivators)
    {
        ArgumentNullException.ThrowIfNull(controllerActivator);
        ArgumentNullException.ThrowIfNull(propertyActivators);

        _controllerActivator = controllerActivator;
        _propertyActivators = propertyActivators.ToArray();
    }

    /// <inheritdoc />
    public object CreateController(ControllerContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.ActionDescriptor == null)
        {
            throw new ArgumentException(Resources.FormatPropertyOfTypeCannotBeNull(
                nameof(ControllerContext.ActionDescriptor),
                nameof(ControllerContext)));
        }

        var controller = _controllerActivator.Create(context);
        foreach (var propertyActivator in _propertyActivators)
        {
            propertyActivator.Activate(context, controller);
        }

        return controller;
    }

    /// <inheritdoc />
    public void ReleaseController(ControllerContext context, object controller)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(controller);

        _controllerActivator.Release(context, controller);
    }

    public ValueTask ReleaseControllerAsync(ControllerContext context, object controller)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(controller);

        return _controllerActivator.ReleaseAsync(context, controller);
    }
}
