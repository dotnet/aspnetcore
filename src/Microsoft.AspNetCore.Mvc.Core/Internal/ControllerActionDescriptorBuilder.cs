// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    /// <summary>
    /// Creates instances of <see cref="ControllerActionDescriptor"/> from <see cref="ApplicationModel"/>.
    /// </summary>
    public static class ControllerActionDescriptorBuilder
    {
        // This is the default order for attribute routes whose order calculated from
        // the controller model is null.
        private const int DefaultAttributeRouteOrder = 0;

        /// <summary>
        /// Creates instances of <see cref="ControllerActionDescriptor"/> from <see cref="ApplicationModel"/>.
        /// </summary>
        /// <param name="application">The <see cref="ApplicationModel"/>.</param>
        /// <returns>The list of <see cref="ControllerActionDescriptor"/>.</returns>
        public static IList<ControllerActionDescriptor> Build(ApplicationModel application)
        {
            var actions = new List<ControllerActionDescriptor>();

            var methodInfoMap = new MethodToActionMap();

            var routeTemplateErrors = new List<string>();
            var attributeRoutingConfigurationErrors = new Dictionary<MethodInfo, string>();

            foreach (var controller in application.Controllers)
            {
                // Only add properties which are explicitly marked to bind.
                // The attribute check is required for ModelBinder attribute.
                var controllerPropertyDescriptors = controller.ControllerProperties
                    .Where(p => p.BindingInfo != null)
                    .Select(CreateParameterDescriptor)
                    .ToList();
                foreach (var action in controller.Actions)
                {
                    // Controllers with multiple [Route] attributes (or user defined implementation of
                    // IRouteTemplateProvider) will generate one action descriptor per IRouteTemplateProvider
                    // instance.
                    // Actions with multiple [Http*] attributes or other (IRouteTemplateProvider implementations
                    // have already been identified as different actions during action discovery.
                    var actionDescriptors = CreateActionDescriptors(application, controller, action);

                    foreach (var actionDescriptor in actionDescriptors)
                    {
                        actionDescriptor.ControllerName = controller.ControllerName;
                        actionDescriptor.ControllerTypeInfo = controller.ControllerType;

                        AddApiExplorerInfo(actionDescriptor, application, controller, action);
                        AddRouteValues(actionDescriptor, controller, action);
                        AddProperties(actionDescriptor, action, controller, application);

                        actionDescriptor.BoundProperties = controllerPropertyDescriptors;

                        if (IsAttributeRoutedAction(actionDescriptor))
                        {
                            // Replaces tokens like [controller]/[action] in the route template with the actual values
                            // for this action.
                            ReplaceAttributeRouteTokens(actionDescriptor, routeTemplateErrors);
                        }
                    }

                    methodInfoMap.AddToMethodInfo(action, actionDescriptors);
                    actions.AddRange(actionDescriptors);
                }
            }

            var actionsByRouteName = new Dictionary<string, IList<ActionDescriptor>>(
                StringComparer.OrdinalIgnoreCase);

            // Keeps track of all the methods that we've validated to avoid visiting each action group
            // more than once.
            var validatedMethods = new HashSet<MethodInfo>();

            foreach (var actionDescriptor in actions)
            {
                if (!validatedMethods.Contains(actionDescriptor.MethodInfo))
                {
                    ValidateActionGroupConfiguration(
                        methodInfoMap,
                        actionDescriptor,
                        attributeRoutingConfigurationErrors);

                    validatedMethods.Add(actionDescriptor.MethodInfo);
                }

                var attributeRouteInfo = actionDescriptor.AttributeRouteInfo;
                if (attributeRouteInfo?.Name != null)
                {
                    // Build a map of attribute route name to action descriptors to ensure that all
                    // attribute routes with a given name have the same template.
                    AddActionToNamedGroup(actionsByRouteName, attributeRouteInfo.Name, actionDescriptor);
                }
            }

            if (attributeRoutingConfigurationErrors.Any())
            {
                var message = CreateAttributeRoutingAggregateErrorMessage(
                    attributeRoutingConfigurationErrors.Values);

                throw new InvalidOperationException(message);
            }

            var namedRoutedErrors = ValidateNamedAttributeRoutedActions(actionsByRouteName);
            if (namedRoutedErrors.Any())
            {
                var message = CreateAttributeRoutingAggregateErrorMessage(namedRoutedErrors);
                throw new InvalidOperationException(message);
            }

            if (routeTemplateErrors.Any())
            {
                var message = CreateAttributeRoutingAggregateErrorMessage(routeTemplateErrors);
                throw new InvalidOperationException(message);
            }

            return actions;
        }

        private static IList<ControllerActionDescriptor> CreateActionDescriptors(
            ApplicationModel application,
            ControllerModel controller,
            ActionModel action)
        {
            var controllerAttributeRoutes = controller.Selectors
                .Where(sm => sm.AttributeRouteModel != null)
                .Select(sm => sm.AttributeRouteModel)
                .ToList();

            var actionDescriptors = new List<ControllerActionDescriptor>();

            foreach (var actionSelectorModel in action.Selectors)
            {
                var actionAttributeRoute = actionSelectorModel.AttributeRouteModel;

                // We check the action to see if the template allows combination behavior
                // (It doesn't start with / or ~/) so that in the case where we have multiple
                // [Route] attributes on the controller we don't end up creating multiple
                if (actionAttributeRoute != null && actionAttributeRoute.IsAbsoluteTemplate)
                {
                    // We're overriding the attribute routes on the controller, so filter out any metadata
                    // from controller level routes.
                    var actionDescriptor = CreateActionDescriptor(
                        action,
                        actionAttributeRoute,
                        controllerAttributeRoute: null);

                    actionDescriptors.Add(actionDescriptor);

                    AddActionFilters(actionDescriptor, action.Filters, controller.Filters, application.Filters);

                    // If we're using an attribute route on the controller, then filter out any additional
                    // metadata from the 'other' attribute routes.
                    IList<IActionConstraintMetadata> controllerConstraints = null;
                    if (controller.Selectors.Count > 0)
                    {
                        controllerConstraints = controller.Selectors[0].ActionConstraints
                            .Where(constraint => !(constraint is IRouteTemplateProvider)).ToList();
                    }

                    AddActionConstraints(actionDescriptor, actionSelectorModel, controllerConstraints);
                }
                else if (controllerAttributeRoutes.Count > 0)
                {
                    // We're using the attribute routes from the controller
                    foreach (var controllerSelectorModel in controller.Selectors)
                    {
                        var controllerAttributeRoute = controllerSelectorModel.AttributeRouteModel;

                        var actionDescriptor = CreateActionDescriptor(
                            action,
                            actionAttributeRoute,
                            controllerAttributeRoute);

                        actionDescriptors.Add(actionDescriptor);

                        AddActionFilters(actionDescriptor, action.Filters, controller.Filters, application.Filters);

                        // If we're using an attribute route on the controller, then filter out any additional
                        // metadata from the 'other' attribute routes.
                        var controllerConstraints = controllerSelectorModel.ActionConstraints
                            .Where(c => c == controllerAttributeRoute?.Attribute || !(c is IRouteTemplateProvider));
                        AddActionConstraints(actionDescriptor, actionSelectorModel, controllerConstraints);
                    }
                }
                else
                {
                    // No attribute routes on the controller
                    var actionDescriptor = CreateActionDescriptor(
                        action,
                        actionAttributeRoute,
                        controllerAttributeRoute: null);
                    actionDescriptors.Add(actionDescriptor);

                    IList<IActionConstraintMetadata> controllerConstraints = null;
                    if (controller.Selectors.Count > 0)
                    {
                        controllerConstraints = controller.Selectors[0].ActionConstraints;
                    }

                    // If there's no attribute route on the controller, then we use all of the filters/constraints
                    // on the controller regardless.
                    AddActionFilters(actionDescriptor, action.Filters, controller.Filters, application.Filters);
                    AddActionConstraints(actionDescriptor, actionSelectorModel, controllerConstraints);
                }
            }

            return actionDescriptors;
        }

        private static ControllerActionDescriptor CreateActionDescriptor(
            ActionModel action,
            AttributeRouteModel actionAttributeRoute,
            AttributeRouteModel controllerAttributeRoute)
        {
            var parameterDescriptors = new List<ParameterDescriptor>();
            foreach (var parameter in action.Parameters)
            {
                var parameterDescriptor = CreateParameterDescriptor(parameter);
                parameterDescriptors.Add(parameterDescriptor);
            }

            var actionDescriptor = new ControllerActionDescriptor()
            {
                ActionName = action.ActionName,
                MethodInfo = action.ActionMethod,
                Parameters = parameterDescriptors,
                AttributeRouteInfo = CreateAttributeRouteInfo(actionAttributeRoute, controllerAttributeRoute)
            };

            return actionDescriptor;
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

            if (isVisibleSetOnActionOrController && !IsAttributeRoutedAction(actionDescriptor))
            {
                // ApiExplorer is only supported on attribute routed actions.
                throw new InvalidOperationException(Resources.FormatApiExplorer_UnsupportedAction(
                    actionDescriptor.DisplayName));
            }
            else if (isVisibleSetOnApplication && !IsAttributeRoutedAction(actionDescriptor))
            {
                // This is the case where we're going to be lenient, just ignore it.
            }
            else if (isVisible)
            {
                Debug.Assert(IsAttributeRoutedAction(actionDescriptor));

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

        private static AttributeRouteInfo CreateAttributeRouteInfo(
            AttributeRouteModel action,
            AttributeRouteModel controller)
        {
            var combinedRoute = AttributeRouteModel.CombineAttributeRouteModel(controller, action);

            if (combinedRoute == null)
            {
                return null;
            }
            else
            {
                return new AttributeRouteInfo
                {
                    Template = combinedRoute.Template,
                    Order = combinedRoute.Order ?? DefaultAttributeRouteOrder,
                    Name = combinedRoute.Name,
                    SuppressLinkGeneration = combinedRoute.SuppressLinkGeneration,
                    SuppressPathMatching = combinedRoute.SuppressPathMatching,
                };
            }
        }

        private static void AddActionConstraints(
            ControllerActionDescriptor actionDescriptor,
            SelectorModel selectorModel,
            IEnumerable<IActionConstraintMetadata> controllerConstraints)
        {
            var constraints = new List<IActionConstraintMetadata>();

            if (selectorModel.ActionConstraints != null)
            {
                constraints.AddRange(selectorModel.ActionConstraints);
            }

            if (controllerConstraints != null)
            {
                constraints.AddRange(controllerConstraints);
            }

            if (constraints.Count > 0)
            {
                actionDescriptor.ActionConstraints = constraints;
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

        private static void ReplaceAttributeRouteTokens(
            ControllerActionDescriptor actionDescriptor,
            IList<string> routeTemplateErrors)
        {
            try
            {
                actionDescriptor.AttributeRouteInfo.Template = AttributeRouteModel.ReplaceTokens(
                    actionDescriptor.AttributeRouteInfo.Template,
                    actionDescriptor.RouteValues);

                if (actionDescriptor.AttributeRouteInfo.Name != null)
                {
                    actionDescriptor.AttributeRouteInfo.Name = AttributeRouteModel.ReplaceTokens(
                        actionDescriptor.AttributeRouteInfo.Name,
                        actionDescriptor.RouteValues);
                }
            }
            catch (InvalidOperationException ex)
            {
                // Routing will throw an InvalidOperationException here if we can't parse/replace tokens
                // in the template.
                var message = Resources.FormatAttributeRoute_IndividualErrorMessage(
                    actionDescriptor.DisplayName,
                    Environment.NewLine,
                    ex.Message);

                routeTemplateErrors.Add(message);
            }
        }

        private static void AddActionToNamedGroup(
            IDictionary<string, IList<ActionDescriptor>> actionsByRouteName,
            string routeName,
            ControllerActionDescriptor actionDescriptor)
        {
            if (actionsByRouteName.TryGetValue(routeName, out var namedActionGroup))
            {
                namedActionGroup.Add(actionDescriptor);
            }
            else
            {
                namedActionGroup = new List<ActionDescriptor>();
                namedActionGroup.Add(actionDescriptor);
                actionsByRouteName.Add(routeName, namedActionGroup);
            }
        }

        private static bool IsAttributeRoutedAction(ControllerActionDescriptor actionDescriptor)
        {
            return actionDescriptor.AttributeRouteInfo?.Template != null;
        }

        private static IList<string> AddErrorNumbers(
            IEnumerable<string> namedRoutedErrors)
        {
            return namedRoutedErrors
                .Select((error, i) =>
                            Resources.FormatAttributeRoute_AggregateErrorMessage_ErrorNumber(
                                i + 1,
                                Environment.NewLine,
                                error))
                .ToList();
        }

        private static IList<string> ValidateNamedAttributeRoutedActions(
            IDictionary<string,
            IList<ActionDescriptor>> actionsGroupedByRouteName)
        {
            var namedRouteErrors = new List<string>();

            foreach (var kvp in actionsGroupedByRouteName)
            {
                // We are looking for attribute routed actions that have the same name but
                // different route templates. We pick the first template of the group and
                // we compare it against the rest of the templates that have that same name
                // associated.
                // The moment we find one that is different we report the whole group to the
                // user in the error message so that he can see the different actions and the
                // different templates for a given named attribute route.
                var firstActionDescriptor = kvp.Value[0];
                var firstTemplate = firstActionDescriptor.AttributeRouteInfo.Template;

                for (var i = 1; i < kvp.Value.Count; i++)
                {
                    var otherActionDescriptor = kvp.Value[i];
                    var otherActionTemplate = otherActionDescriptor.AttributeRouteInfo.Template;

                    if (!firstTemplate.Equals(otherActionTemplate, StringComparison.OrdinalIgnoreCase))
                    {
                        var descriptions = kvp.Value.Select(ad =>
                            Resources.FormatAttributeRoute_DuplicateNames_Item(
                                ad.DisplayName,
                                ad.AttributeRouteInfo.Template));

                        var errorDescription = string.Join(Environment.NewLine, descriptions);
                        var message = Resources.FormatAttributeRoute_DuplicateNames(
                            kvp.Key,
                            Environment.NewLine,
                            errorDescription);

                        namedRouteErrors.Add(message);
                        break;
                    }
                }
            }

            return namedRouteErrors;
        }

        private static void ValidateActionGroupConfiguration(
            IDictionary<MethodInfo, IDictionary<ActionModel, IList<ControllerActionDescriptor>>> methodMap,
            ControllerActionDescriptor actionDescriptor,
            IDictionary<MethodInfo, string> routingConfigurationErrors)
        {
            var hasAttributeRoutedActions = false;
            var hasConventionallyRoutedActions = false;

            var actionsForMethod = methodMap[actionDescriptor.MethodInfo];
            foreach (var reflectedAction in actionsForMethod)
            {
                foreach (var action in reflectedAction.Value)
                {
                    if (IsAttributeRoutedAction(action))
                    {
                        hasAttributeRoutedActions = true;
                    }
                    else
                    {
                        hasConventionallyRoutedActions = true;
                    }
                }
            }

            // Validate that no method result in attribute and non attribute actions at the same time.
            // By design, mixing attribute and conventionally actions in the same method is not allowed.
            //
            // This for example:
            //
            // [HttpGet]
            // [HttpPost("Foo")]
            // public void Foo() { }
            if (hasAttributeRoutedActions && hasConventionallyRoutedActions)
            {
                var message = CreateMixedRoutedActionDescriptorsErrorMessage(
                    actionDescriptor,
                    actionsForMethod);

                routingConfigurationErrors.Add(actionDescriptor.MethodInfo, message);
            }
        }

        private static string CreateMixedRoutedActionDescriptorsErrorMessage(
            ControllerActionDescriptor actionDescriptor,
            IDictionary<ActionModel, IList<ControllerActionDescriptor>> actionsForMethod)
        {
            // Text to show as the attribute route template for conventionally routed actions.
            var nullTemplate = Resources.AttributeRoute_NullTemplateRepresentation;

            var actionDescriptions = new List<string>();
            foreach (var action in actionsForMethod.SelectMany(kvp => kvp.Value))
            {
                var routeTemplate = action.AttributeRouteInfo?.Template ?? nullTemplate;

                var verbs = action.ActionConstraints?.OfType<HttpMethodActionConstraint>()
                    .FirstOrDefault()?.HttpMethods;

                var formattedVerbs = string.Empty;
                if (verbs != null)
                {
                    formattedVerbs = string.Join(", ", verbs.OrderBy(v => v, StringComparer.OrdinalIgnoreCase));
                }

                var description =
                    Resources.FormatAttributeRoute_MixedAttributeAndConventionallyRoutedActions_ForMethod_Item(
                        action.DisplayName,
                        routeTemplate,
                        formattedVerbs);

                actionDescriptions.Add(description);
            }

            // Sample error message:
            //
            // A method 'MyApplication.CustomerController.Index' must not define attributed actions and
            // non attributed actions at the same time:
            // Action: 'MyApplication.CustomerController.Index' - Route Template: 'Products' - HTTP Verbs: 'PUT'
            // Action: 'MyApplication.CustomerController.Index' - Route Template: '(none)' - HTTP Verbs: 'POST'
            //
            // Use 'AcceptVerbsAttribute' to create a single route that allows multiple HTTP verbs and defines a route,
            // or set a route template in all attributes that constrain HTTP verbs.
            return
                Resources.FormatAttributeRoute_MixedAttributeAndConventionallyRoutedActions_ForMethod(
                    actionDescriptor.DisplayName,
                    Environment.NewLine,
                    string.Join(Environment.NewLine, actionDescriptions));
        }

        private static string CreateAttributeRoutingAggregateErrorMessage(
            IEnumerable<string> individualErrors)
        {
            var errorMessages = AddErrorNumbers(individualErrors);

            var message = Resources.FormatAttributeRoute_AggregateErrorMessage(
                Environment.NewLine,
                string.Join(Environment.NewLine + Environment.NewLine, errorMessages));
            return message;
        }

        // We need to build a map of methods to reflected actions and reflected actions to
        // action descriptors so that we can validate later that no method produced attribute
        // and non attributed actions at the same time, and that no method that produced attribute
        // routed actions has no attributes that implement IActionHttpMethodProvider and do not
        // implement IRouteTemplateProvider. For example:
        //
        // public class ProductsController
        // {
        //    [HttpGet("Products")]
        //    [HttpPost]
        //    public ActionResult Items(){ ... }
        //
        //    [HttpGet("Products")]
        //    [CustomHttpMethods("POST, PUT")]
        //    public ActionResult List(){ ... }
        // }
        private class MethodToActionMap :
            Dictionary<MethodInfo, IDictionary<ActionModel, IList<ControllerActionDescriptor>>>
        {
            public void AddToMethodInfo(
                ActionModel action,
                IList<ControllerActionDescriptor> actionDescriptors)
            {
                if (TryGetValue(action.ActionMethod, out var actionsForMethod))
                {
                    actionsForMethod.Add(action, actionDescriptors);
                }
                else
                {
                    var reflectedActionMap =
                        new Dictionary<ActionModel, IList<ControllerActionDescriptor>>();
                    reflectedActionMap.Add(action, actionDescriptors);
                    Add(action.ActionMethod, reflectedActionMap);
                }
            }
        }
    }
}