// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.Controllers;

internal sealed class DefaultControllerPropertyActivator : IControllerPropertyActivator
{
    private static readonly Func<Type, PropertyActivator<ControllerContext>[]> _getPropertiesToActivate =
        GetPropertiesToActivate;
    private readonly ConcurrentDictionary<Type, PropertyActivator<ControllerContext>[]> _activateActions = new();

    public void Activate(ControllerContext context, object controller)
    {
        var controllerType = controller.GetType();
        var propertiesToActivate = _activateActions!.GetOrAdd(
            controllerType,
            _getPropertiesToActivate);

        for (var i = 0; i < propertiesToActivate.Length; i++)
        {
            var activateInfo = propertiesToActivate[i];
            activateInfo.Activate(controller, context);
        }
    }

    public void ClearCache() => _activateActions.Clear();

    public Action<ControllerContext, object> GetActivatorDelegate(ControllerActionDescriptor actionDescriptor)
    {
        ArgumentNullException.ThrowIfNull(actionDescriptor);

        var controllerType = actionDescriptor.ControllerTypeInfo?.AsType();
        if (controllerType == null)
        {
            throw new ArgumentException(Resources.FormatPropertyOfTypeCannotBeNull(
                nameof(actionDescriptor.ControllerTypeInfo),
                nameof(actionDescriptor)),
                nameof(actionDescriptor));
        }

        var propertiesToActivate = GetPropertiesToActivate(controllerType);
        void Activate(ControllerContext controllerContext, object controller)
        {
            for (var i = 0; i < propertiesToActivate.Length; i++)
            {
                var activateInfo = propertiesToActivate[i];
                activateInfo.Activate(controller, controllerContext);
            }
        }

        return Activate;
    }

    private static PropertyActivator<ControllerContext>[] GetPropertiesToActivate(Type type)
    {
        IEnumerable<PropertyActivator<ControllerContext>> activators;
        activators = PropertyActivator<ControllerContext>.GetPropertiesToActivate(
            type,
            typeof(ActionContextAttribute),
            p => new PropertyActivator<ControllerContext>(p, c => c));

        activators = activators.Concat(PropertyActivator<ControllerContext>.GetPropertiesToActivate(
            type,
            typeof(ControllerContextAttribute),
            p => new PropertyActivator<ControllerContext>(p, c => c)));

        return activators.ToArray();
    }
}
