// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

/// <summary>
/// Represents a <see cref="IControllerActivatorProvider"/> for a view data dictionary controller.
/// </summary>
public class ViewDataDictionaryControllerPropertyActivator : IControllerPropertyActivator
{
    private readonly Func<Type, PropertyActivator<ControllerContext>[]> _getPropertiesToActivate;
    private readonly IModelMetadataProvider _modelMetadataProvider;
    private ConcurrentDictionary<Type, PropertyActivator<ControllerContext>[]> _activateActions;
    private bool _initialized;
    private object _initializeLock = new object();

    /// <summary>
    /// Initializes a new instance of <see cref="ViewDataDictionaryControllerPropertyActivator"/>.
    /// </summary>
    /// <param name="modelMetadataProvider">The <see cref="IModelMetadataProvider"/> to use.</param>
    public ViewDataDictionaryControllerPropertyActivator(IModelMetadataProvider modelMetadataProvider)
    {
        _modelMetadataProvider = modelMetadataProvider;
        _getPropertiesToActivate = GetPropertiesToActivate;
    }

    /// <inheritdoc/>
    public void Activate(ControllerContext actionContext, object controller)
    {
        LazyInitializer.EnsureInitialized(
            ref _activateActions,
            ref _initialized,
            ref _initializeLock);

        var controllerType = controller.GetType();
        var propertiesToActivate = _activateActions.GetOrAdd(
            controllerType,
            _getPropertiesToActivate);

        for (var i = 0; i < propertiesToActivate.Length; i++)
        {
            var activateInfo = propertiesToActivate[i];
            activateInfo.Activate(controller, actionContext);
        }
    }

    /// <inheritdoc/>
    public Action<ControllerContext, object> GetActivatorDelegate(ControllerActionDescriptor actionDescriptor)
    {
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

    private PropertyActivator<ControllerContext>[] GetPropertiesToActivate(Type type)
    {
        var activators = PropertyActivator<ControllerContext>.GetPropertiesToActivate(
            type,
            typeof(ViewDataDictionaryAttribute),
            p => new PropertyActivator<ControllerContext>(p, GetViewDataDictionary));

        return activators;
    }

    private ViewDataDictionary GetViewDataDictionary(ControllerContext context)
    {
        return new ViewDataDictionary(
            _modelMetadataProvider,
            context.ModelState);
    }
}
