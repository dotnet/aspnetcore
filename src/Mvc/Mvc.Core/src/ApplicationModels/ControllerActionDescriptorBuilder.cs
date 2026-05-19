// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;
using Resources = Microsoft.AspNetCore.Mvc.Core.Resources;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

/// <summary>
/// Creates instances of <see cref="ControllerActionDescriptor"/> from application model
/// types.
/// </summary>
internal static class ControllerActionDescriptorBuilder
{
    public static IList<ControllerActionDescriptor> Build(ApplicationModel application)
    {
        return ApplicationModelFactory.Flatten(application, CreateActionDescriptor);
    }

    private static ControllerActionDescriptor CreateActionDescriptor(
        ApplicationModel application,
        ControllerModel controller,
        ActionModel action,
        SelectorModel selector)
    {
        var actionDescriptor = new ControllerActionDescriptor
        {
            ActionName = action.ActionName,
            MethodInfo = action.ActionMethod,
        };

        actionDescriptor.ControllerName = controller.ControllerName;
        actionDescriptor.ControllerTypeInfo = controller.ControllerType;
        AddControllerPropertyDescriptors(actionDescriptor, controller);

        AddActionConstraints(actionDescriptor, selector);
        AddEndpointMetadata(actionDescriptor, selector);
        AddAttributeRoute(actionDescriptor, selector);
        AddParameterDescriptors(actionDescriptor, action);
        AddActionFilters(actionDescriptor, action.Filters, controller.Filters, application.Filters);
        AddApiExplorerInfo(actionDescriptor, application, controller, action);
        AddRouteValues(actionDescriptor, controller, action);
        AddProperties(actionDescriptor, action, controller, application);

        return actionDescriptor;
    }

    private static void AddControllerPropertyDescriptors(ActionDescriptor actionDescriptor, ControllerModel controller)
    {
        actionDescriptor.BoundProperties = controller.ControllerProperties
            .Where(p => p.BindingInfo != null)
            .Select(CreateParameterDescriptor)
            .ToList();
    }

    private static void AddParameterDescriptors(ActionDescriptor actionDescriptor, ActionModel action)
    {
        var parameterDescriptors = new List<ParameterDescriptor>(action.Parameters.Count);
        foreach (var parameter in action.Parameters)
        {
            var parameterDescriptor = CreateParameterDescriptor(parameter);
            parameterDescriptors.Add(parameterDescriptor);
        }

        actionDescriptor.Parameters = parameterDescriptors;
    }

    private static ParameterDescriptor CreateParameterDescriptor(ParameterModel parameterModel)
    {
        var parameterDescriptor = new ControllerParameterDescriptor()
        {
            Name = parameterModel.ParameterName,
            ParameterType = parameterModel.ParameterInfo.ParameterType,
            BindingInfo = parameterModel.BindingInfo,
            ParameterInfo = parameterModel.ParameterInfo,
        };

        return parameterDescriptor;
    }

    private static ParameterDescriptor CreateParameterDescriptor(PropertyModel propertyModel)
    {
        var parameterDescriptor = new ControllerBoundPropertyDescriptor()
        {
            BindingInfo = propertyModel.BindingInfo,
            Name = propertyModel.PropertyName,
            ParameterType = propertyModel.PropertyInfo.PropertyType,
            PropertyInfo = propertyModel.PropertyInfo,
        };

        return parameterDescriptor;
    }

    private static void AddApiExplorerInfo(
        ControllerActionDescriptor actionDescriptor,
        ApplicationModel application,
        ControllerModel controller,
        ActionModel action)
    {
        var isVisible =
            action.ApiExplorer?.IsVisible ??
            controller.ApiExplorer?.IsVisible ??
            application.ApiExplorer?.IsVisible ??
            false;

        var isVisibleSetOnActionOrController =
            action.ApiExplorer?.IsVisible ??
            controller.ApiExplorer?.IsVisible ??
            false;

        // ApiExplorer isn't supported on conventional-routed actions, but we still allow you to configure
        // it at the application level when you have a mix of controller types. We'll just skip over enabling
        // ApiExplorer for conventional-routed controllers when this happens.
        var isVisibleSetOnApplication = application.ApiExplorer?.IsVisible ?? false;

        if (isVisibleSetOnActionOrController && !IsAttributeRouted(actionDescriptor))
        {
            // ApiExplorer is only supported on attribute routed actions.
            throw new InvalidOperationException(Resources.FormatApiExplorer_UnsupportedAction(
                actionDescriptor.DisplayName));
        }
        else if (isVisibleSetOnApplication && !IsAttributeRouted(actionDescriptor))
        {
            // This is the case where we're going to be lenient, just ignore it.
        }
        else if (isVisible)
        {
            Debug.Assert(IsAttributeRouted(actionDescriptor));

            var apiExplorerActionData = new ApiDescriptionActionData()
            {
                GroupName = action.ApiExplorer?.GroupName ?? controller.ApiExplorer?.GroupName,
            };

            actionDescriptor.SetProperty(apiExplorerActionData);
        }
    }

    private static void AddProperties(
        ControllerActionDescriptor actionDescriptor,
        ActionModel action,
        ControllerModel controller,
        ApplicationModel application)
    {
        foreach (var item in application.Properties)
        {
            actionDescriptor.Properties[item.Key] = item.Value;
        }

        foreach (var item in controller.Properties)
        {
            actionDescriptor.Properties[item.Key] = item.Value;
        }

        foreach (var item in action.Properties)
        {
            actionDescriptor.Properties[item.Key] = item.Value;
        }
    }

    private static void AddActionFilters(
        ControllerActionDescriptor actionDescriptor,
        IEnumerable<IFilterMetadata> actionFilters,
        IEnumerable<IFilterMetadata> controllerFilters,
        IEnumerable<IFilterMetadata> globalFilters)
    {
        actionDescriptor.FilterDescriptors =
            actionFilters.Select(f => new FilterDescriptor(f, FilterScope.Action))
            .Concat(controllerFilters.Select(f => new FilterDescriptor(f, FilterScope.Controller)))
            .Concat(globalFilters.Select(f => new FilterDescriptor(f, FilterScope.Global)))
            .OrderBy(d => d, FilterDescriptorOrderComparer.Comparer)
            .ToList();
    }

    private static void AddActionConstraints(ControllerActionDescriptor actionDescriptor, SelectorModel selectorModel)
    {
        if (selectorModel.ActionConstraints?.Count > 0)
        {
            actionDescriptor.ActionConstraints = new List<IActionConstraintMetadata>(selectorModel.ActionConstraints);
        }
    }

    private static void AddEndpointMetadata(ControllerActionDescriptor actionDescriptor, SelectorModel selectorModel)
    {
        if (selectorModel.EndpointMetadata?.Count > 0)
        {
            actionDescriptor.EndpointMetadata = new List<object>(selectorModel.EndpointMetadata);
        }
    }

    private static void AddAttributeRoute(ControllerActionDescriptor actionDescriptor, SelectorModel selectorModel)
    {
        if (selectorModel.AttributeRouteModel != null)
        {
            actionDescriptor.AttributeRouteInfo = new AttributeRouteInfo
            {
                Template = selectorModel.AttributeRouteModel.Template,
                Order = selectorModel.AttributeRouteModel.Order ?? 0,
                Name = selectorModel.AttributeRouteModel.Name,
                SuppressLinkGeneration = selectorModel.AttributeRouteModel.SuppressLinkGeneration,
                SuppressPathMatching = selectorModel.AttributeRouteModel.SuppressPathMatching,
            };
        }
    }

    public static void AddRouteValues(
        ControllerActionDescriptor actionDescriptor,
        ControllerModel controller,
        ActionModel action)
    {
        // Apply all the constraints defined on the action, then controller (for example, [Area])
        // to the actions. Also keep track of all the constraints that require preventing actions
        // without the constraint to match. For example, actions without an [Area] attribute on their
        // controller should not match when a value has been given for area when matching a url or
        // generating a link.
        foreach (var kvp in action.RouteValues)
        {
            // Skip duplicates
            if (!actionDescriptor.RouteValues.ContainsKey(kvp.Key))
            {
                actionDescriptor.RouteValues.Add(kvp.Key, kvp.Value);
            }
        }

        foreach (var kvp in controller.RouteValues)
        {
            // Skip duplicates - this also means that a value on the action will take precedence
            if (!actionDescriptor.RouteValues.ContainsKey(kvp.Key))
            {
                actionDescriptor.RouteValues.Add(kvp.Key, kvp.Value);
            }
        }

        // Lastly add the 'default' values
        if (!actionDescriptor.RouteValues.ContainsKey("action"))
        {
            actionDescriptor.RouteValues.Add("action", action.ActionName ?? string.Empty);
        }

        if (!actionDescriptor.RouteValues.ContainsKey("controller"))
        {
            actionDescriptor.RouteValues.Add("controller", controller.ControllerName);
        }
    }

    private static bool IsAttributeRouted(ActionDescriptor actionDescriptor)
    {
        return actionDescriptor.AttributeRouteInfo != null;
    }
}
