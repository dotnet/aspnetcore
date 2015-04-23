// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Mvc.ApiExplorer;
using Microsoft.AspNet.Mvc.ApplicationModels;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.Routing;
using Microsoft.AspNet.Mvc.ModelBinding;

namespace Microsoft.AspNet.Mvc
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

            var hasAttributeRoutes = false;
            var removalConstraints = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var methodInfoMap = new MethodToActionMap();

            var routeTemplateErrors = new List<string>();
            var attributeRoutingConfigurationErrors = new Dictionary<MethodInfo, string>();

            foreach (var controller in application.Controllers)
            {
                // Only add properties which are explictly marked to bind.
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
                        AddRouteConstraints(removalConstraints, actionDescriptor, controller, action);
                        AddProperties(actionDescriptor, action, controller, application);

                        actionDescriptor.BoundProperties = controllerPropertyDescriptors;
                        if (IsAttributeRoutedAction(actionDescriptor))
                        {
                            hasAttributeRoutes = true;

                            // An attribute routed action will ignore conventional routed constraints. We still
                            // want to provide these values as ambient values for link generation.
                            AddConstraintsAsDefaultRouteValues(actionDescriptor);

                            // Replaces tokens like [controller]/[action] in the route template with the actual values
                            // for this action.
                            ReplaceAttributeRouteTokens(actionDescriptor, routeTemplateErrors);

                            // Attribute routed actions will ignore conventional routed constraints. Instead they have
                            // a single route constraint "RouteGroup" associated with it.
                            ReplaceRouteConstraints(actionDescriptor);
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

                if (!IsAttributeRoutedAction(actionDescriptor))
                {
                    // Any attribute routes are in use, then non-attribute-routed action descriptors can't be
                    // selected when a route group returned by the route.
                    if (hasAttributeRoutes)
                    {
                        actionDescriptor.RouteConstraints.Add(new RouteDataActionConstraint(
                            AttributeRouting.RouteGroupKey,
                            string.Empty));
                    }

                    // Add a route constraint with DenyKey for each constraint in the set to all the
                    // actions that don't have that constraint. For example, if a controller defines
                    // an area constraint, all actions that don't belong to an area must have a route
                    // constraint that prevents them from matching an incomming request.
                    AddRemovalConstraints(actionDescriptor, removalConstraints);
                }
                else
                {
                    var attributeRouteInfo = actionDescriptor.AttributeRouteInfo;
                    if (attributeRouteInfo.Name != null)
                    {
                        // Build a map of attribute route name to action descriptors to ensure that all
                        // attribute routes with a given name have the same template.
                        AddActionToNamedGroup(actionsByRouteName, attributeRouteInfo.Name, actionDescriptor);
                    }

                    // We still want to add a 'null' for any constraint with DenyKey so that link generation
                    // works properly.
                    //
                    // Consider an action like { area = "", controller = "Home", action = "Index" }. Even if
                    // it's attribute routed, it needs to know that area must be null to generate a link.
                    foreach (var key in removalConstraints)
                    {
                        if (!actionDescriptor.RouteValueDefaults.ContainsKey(key))
                        {
                            actionDescriptor.RouteValueDefaults.Add(key, value: null);
                        }
                    }
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
            var actionDescriptors = new List<ControllerActionDescriptor>();

            // We check the action to see if the template allows combination behavior
            // (It doesn't start with / or ~/) so that in the case where we have multiple
            // [Route] attributes on the controller we don't end up creating multiple
            if (action.AttributeRouteModel != null &&
                action.AttributeRouteModel.IsAbsoluteTemplate)
            {
                // We're overriding the attribute routes on the controller, so filter out any metadata
                // from controller level routes.
                var actionDescriptor = CreateActionDescriptor(
                    action,
                    controllerAttributeRoute: null);

                actionDescriptors.Add(actionDescriptor);

                // If we're using an attribute route on the controller, then filter out any additional
                // metadata from the 'other' attribute routes.
                var controllerFilters = controller.Filters
                    .Where(c => !(c is IRouteTemplateProvider));
                AddActionFilters(actionDescriptor, action.Filters, controllerFilters, application.Filters);

                var controllerConstraints = controller.ActionConstraints
                    .Where(c => !(c is IRouteTemplateProvider));
                AddActionConstraints(actionDescriptor, action, controllerConstraints);
            }
            else if (controller.AttributeRoutes != null &&
                controller.AttributeRoutes.Count > 0)
            {
                // We're using the attribute routes from the controller
                foreach (var controllerAttributeRoute in controller.AttributeRoutes)
                {
                    var actionDescriptor = CreateActionDescriptor(
                        action,
                        controllerAttributeRoute);

                    actionDescriptors.Add(actionDescriptor);

                    // If we're using an attribute route on the controller, then filter out any additional
                    // metadata from the 'other' attribute routes.
                    var controllerFilters = controller.Filters
                        .Where(c => c == controllerAttributeRoute?.Attribute || !(c is IRouteTemplateProvider));
                    AddActionFilters(actionDescriptor, action.Filters, controllerFilters, application.Filters);

                    var controllerConstraints = controller.ActionConstraints
                        .Where(c => c == controllerAttributeRoute?.Attribute || !(c is IRouteTemplateProvider));
                    AddActionConstraints(actionDescriptor, action, controllerConstraints);
                }
            }
            else
            {
                // No attribute routes on the controller
                var actionDescriptor = CreateActionDescriptor(
                    action,
                    controllerAttributeRoute: null);
                actionDescriptors.Add(actionDescriptor);

                // If there's no attribute route on the controller, then we can use all of the filters/constraints
                // on the controller.
                AddActionFilters(actionDescriptor, action.Filters, controller.Filters, application.Filters);
                AddActionConstraints(actionDescriptor, action, controller.ActionConstraints);
            }

            return actionDescriptors;
        }

        private static ControllerActionDescriptor CreateActionDescriptor(
            ActionModel action,
            AttributeRouteModel controllerAttributeRoute)
        {
            var parameterDescriptors = new List<ParameterDescriptor>();
            foreach (var parameter in action.Parameters)
            {
                var parameterDescriptor = CreateParameterDescriptor(parameter);
                parameterDescriptors.Add(parameterDescriptor);
            }

            var attributeRouteInfo = CreateAttributeRouteInfo(
                action.AttributeRouteModel,
                controllerAttributeRoute);

            var actionDescriptor = new ControllerActionDescriptor()
            {
                Name = action.ActionName,
                MethodInfo = action.ActionMethod,
                Parameters = parameterDescriptors,
                RouteConstraints = new List<RouteDataActionConstraint>(),
                AttributeRouteInfo = attributeRouteInfo,
            };

            actionDescriptor.DisplayName = string.Format(
                CultureInfo.InvariantCulture,
                "{0}.{1}",
                action.ActionMethod.DeclaringType.FullName,
                action.ActionMethod.Name);

            return actionDescriptor;
        }

        private static ParameterDescriptor CreateParameterDescriptor(ParameterModel parameterModel)
        {
            var parameterDescriptor = new ParameterDescriptor()
            {
                Name = parameterModel.ParameterName,
                ParameterType = parameterModel.ParameterInfo.ParameterType,
                BindingInfo = parameterModel.BindingInfo
            };

            return parameterDescriptor;
        }

        private static ParameterDescriptor CreateParameterDescriptor(PropertyModel propertyModel)
        {
            var parameterDescriptor = new ParameterDescriptor()
            {
                BindingInfo = propertyModel.BindingInfo,
                Name = propertyModel.PropertyName,
                ParameterType = propertyModel.PropertyInfo.PropertyType,
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
            IEnumerable<IFilter> actionFilters,
            IEnumerable<IFilter> controllerFilters,
            IEnumerable<IFilter> globalFilters)
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
            var combinedRoute = AttributeRouteModel.CombineAttributeRouteModel(
                                controller,
                                action);

            if (combinedRoute == null)
            {
                return null;
            }
            else
            {
                return new AttributeRouteInfo()
                {
                    Template = combinedRoute.Template,
                    Order = combinedRoute.Order ?? DefaultAttributeRouteOrder,
                    Name = combinedRoute.Name,
                };
            }
        }

        private static void AddActionConstraints(
            ControllerActionDescriptor actionDescriptor,
            ActionModel action,
            IEnumerable<IActionConstraintMetadata> controllerConstraints)
        {
            var constraints = new List<IActionConstraintMetadata>();

            var httpMethods = action.HttpMethods;
            if (httpMethods != null && httpMethods.Count > 0)
            {
                constraints.Add(new HttpMethodConstraint(httpMethods));
            }

            if (action.ActionConstraints != null)
            {
                constraints.AddRange(action.ActionConstraints);
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

        public static void AddRouteConstraints(
            ISet<string> removalConstraints,
            ControllerActionDescriptor actionDescriptor,
            ControllerModel controller,
            ActionModel action)
        {
            // Apply all the constraints defined on the action, then controller (for example, [Area]) 
            // to the actions. Also keep track of all the constraints that require preventing actions
            // without the constraint to match. For example, actions without an [Area] attribute on their
            // controller should not match when a value has been given for area when matching a url or
            // generating a link.
            foreach (var constraintAttribute in action.RouteConstraints)
            {
                if (constraintAttribute.BlockNonAttributedActions)
                {
                    removalConstraints.Add(constraintAttribute.RouteKey);
                }

                // Skip duplicates
                if (!HasConstraint(actionDescriptor.RouteConstraints, constraintAttribute.RouteKey))
                {
                    if (constraintAttribute.RouteKeyHandling == RouteKeyHandling.CatchAll)
                    {
                        actionDescriptor.RouteConstraints.Add(
                            RouteDataActionConstraint.CreateCatchAll(
                            constraintAttribute.RouteKey));
                    }
                    else
                    {
                        actionDescriptor.RouteConstraints.Add(new RouteDataActionConstraint(
                            constraintAttribute.RouteKey,
                            constraintAttribute.RouteValue));
                    }
                }
            }

            foreach (var constraintAttribute in controller.RouteConstraints)
            {
                if (constraintAttribute.BlockNonAttributedActions)
                {
                    removalConstraints.Add(constraintAttribute.RouteKey);
                }

                // Skip duplicates - this also means that a value on the action will take precedence
                if (!HasConstraint(actionDescriptor.RouteConstraints, constraintAttribute.RouteKey))
                {
                    if (constraintAttribute.RouteKeyHandling == RouteKeyHandling.CatchAll)
                    {
                        actionDescriptor.RouteConstraints.Add(
                            RouteDataActionConstraint.CreateCatchAll(
                            constraintAttribute.RouteKey));
                    }
                    else
                    {
                        actionDescriptor.RouteConstraints.Add(new RouteDataActionConstraint(
                            constraintAttribute.RouteKey,
                            constraintAttribute.RouteValue));
                    }
                }
            }

            // Lastly add the 'default' values
            if (!HasConstraint(actionDescriptor.RouteConstraints, "action"))
            {
                actionDescriptor.RouteConstraints.Add(new RouteDataActionConstraint(
                    "action",
                    action.ActionName ?? string.Empty));
            }

            if (!HasConstraint(actionDescriptor.RouteConstraints, "controller"))
            {
                actionDescriptor.RouteConstraints.Add(new RouteDataActionConstraint(
                    "controller",
                    controller.ControllerName));
            }
        }

        private static bool HasConstraint(IList<RouteDataActionConstraint> constraints, string routeKey)
        {
            return constraints.Any(
                rc => string.Equals(rc.RouteKey, routeKey, StringComparison.OrdinalIgnoreCase));
        }

        private static void ReplaceRouteConstraints(ControllerActionDescriptor actionDescriptor)
        {
            var routeGroupValue = GetRouteGroupValue(
                actionDescriptor.AttributeRouteInfo.Order,
                actionDescriptor.AttributeRouteInfo.Template);

            var routeConstraints = new List<RouteDataActionConstraint>();
            routeConstraints.Add(new RouteDataActionConstraint(
                AttributeRouting.RouteGroupKey,
                routeGroupValue));

            actionDescriptor.RouteConstraints = routeConstraints;
        }

        private static void ReplaceAttributeRouteTokens(
            ControllerActionDescriptor actionDescriptor,
            IList<string> routeTemplateErrors)
        {
            try
            {
                actionDescriptor.AttributeRouteInfo.Template = AttributeRouteModel.ReplaceTokens(
                    actionDescriptor.AttributeRouteInfo.Template,
                    actionDescriptor.RouteValueDefaults);

                if (actionDescriptor.AttributeRouteInfo.Name != null)
                {
                    actionDescriptor.AttributeRouteInfo.Name = AttributeRouteModel.ReplaceTokens(
                        actionDescriptor.AttributeRouteInfo.Name,
                        actionDescriptor.RouteValueDefaults);
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

        private static void AddConstraintsAsDefaultRouteValues(ControllerActionDescriptor actionDescriptor)
        {
            foreach (var constraint in actionDescriptor.RouteConstraints)
            {
                // We don't need to do anything with attribute routing for 'catch all' behavior. Order
                // and predecedence of attribute routes allow this kind of behavior.
                if (constraint.KeyHandling == RouteKeyHandling.RequireKey ||
                    constraint.KeyHandling == RouteKeyHandling.DenyKey)
                {
                    actionDescriptor.RouteValueDefaults.Add(constraint.RouteKey, constraint.RouteValue);
                }
            }
        }

        private static void AddRemovalConstraints(
            ControllerActionDescriptor actionDescriptor,
            ISet<string> removalConstraints)
        {
            foreach (var key in removalConstraints)
            {
                if (!HasConstraint(actionDescriptor.RouteConstraints, key))
                {
                    actionDescriptor.RouteConstraints.Add(new RouteDataActionConstraint(
                        key,
                        string.Empty));
                }
            }
        }

        private static void AddActionToNamedGroup(
            IDictionary<string, IList<ActionDescriptor>> actionsByRouteName,
            string routeName,
            ControllerActionDescriptor actionDescriptor)
        {
            IList<ActionDescriptor> namedActionGroup;

            if (actionsByRouteName.TryGetValue(routeName, out namedActionGroup))
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

                var verbs = action.ActionConstraints.OfType<HttpMethodConstraint>().FirstOrDefault()?.HttpMethods;
                var formattedVerbs = string.Join(", ", verbs.OrderBy(v => v, StringComparer.Ordinal));

                var description =
                    Resources.FormatAttributeRoute_MixedAttributeAndConventionallyRoutedActions_ForMethod_Item(
                        action.DisplayName,
                        routeTemplate,
                        formattedVerbs);

                actionDescriptions.Add(description);
            }

            var methodFullName = string.Format(
                CultureInfo.InvariantCulture,
                "{0}.{1}",
                actionDescriptor.MethodInfo.DeclaringType.FullName,
                actionDescriptor.MethodInfo.Name);

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
                    methodFullName,
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

        private static string GetRouteGroupValue(int order, string template)
        {
            var group = string.Format(CultureInfo.InvariantCulture, "{0}-{1}", order, template);
            return ("__route__" + group).ToUpperInvariant();
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
            public void AddToMethodInfo(ActionModel action,
                IList<ControllerActionDescriptor> actionDescriptors)
            {
                IDictionary<ActionModel, IList<ControllerActionDescriptor>> actionsForMethod = null;
                if (TryGetValue(action.ActionMethod, out actionsForMethod))
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