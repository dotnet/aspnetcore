// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.Description;
using Microsoft.AspNet.Mvc.Filters;
using Microsoft.AspNet.Mvc.ReflectedModelBuilder;
using Microsoft.AspNet.Mvc.Routing;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Mvc
{
    public class ReflectedActionDescriptorProvider : IActionDescriptorProvider
    {
        /// <summary>
        /// Represents the default order associated with this provider for dependency injection
        /// purposes.
        /// </summary>
        public static readonly int DefaultOrder = 0;

        // This is the default order for attribute routes whose order calculated from
        // the reflected model is null.
        private const int DefaultAttributeRouteOrder = 0;

        private readonly IControllerAssemblyProvider _controllerAssemblyProvider;
        private readonly IActionDiscoveryConventions _conventions;
        private readonly IReadOnlyList<IFilter> _globalFilters;
        private readonly IEnumerable<IReflectedApplicationModelConvention> _modelConventions;
        private readonly IInlineConstraintResolver _constraintResolver;

        public ReflectedActionDescriptorProvider(IControllerAssemblyProvider controllerAssemblyProvider,
                                                 IActionDiscoveryConventions conventions,
                                                 IGlobalFilterProvider globalFilters,
                                                 IOptionsAccessor<MvcOptions> optionsAccessor,
                                                 IInlineConstraintResolver constraintResolver)
        {
            _controllerAssemblyProvider = controllerAssemblyProvider;
            _conventions = conventions;
            _globalFilters = globalFilters.Filters;
            _modelConventions = optionsAccessor.Options.ApplicationModelConventions;
            _constraintResolver = constraintResolver;
        }

        public int Order
        {
            get { return DefaultOrder; }
        }

        public void Invoke(ActionDescriptorProviderContext context, Action callNext)
        {
            context.Results.AddRange(GetDescriptors());
            callNext();
        }

        public IEnumerable<ReflectedActionDescriptor> GetDescriptors()
        {
            var model = BuildModel();

            foreach (var convention in _modelConventions)
            {
                convention.OnModelCreated(model);
            }

            return Build(model);
        }

        public ReflectedApplicationModel BuildModel()
        {
            var applicationModel = new ReflectedApplicationModel();
            applicationModel.Filters.AddRange(_globalFilters);

            var assemblies = _controllerAssemblyProvider.CandidateAssemblies;
            var types = assemblies.SelectMany(a => a.DefinedTypes);
            var controllerTypes = types.Where(_conventions.IsController);

            foreach (var controllerType in controllerTypes)
            {
                var controllerModel = new ReflectedControllerModel(controllerType);
                applicationModel.Controllers.Add(controllerModel);

                foreach (var methodInfo in controllerType.AsType().GetMethods())
                {
                    var actionInfos = _conventions.GetActions(methodInfo, controllerType);
                    if (actionInfos == null)
                    {
                        continue;
                    }

                    foreach (var actionInfo in actionInfos)
                    {
                        var actionModel = new ReflectedActionModel(methodInfo);

                        actionModel.ActionName = actionInfo.ActionName;
                        actionModel.IsActionNameMatchRequired = actionInfo.RequireActionNameMatch;
                        actionModel.HttpMethods.AddRange(actionInfo.HttpMethods ?? Enumerable.Empty<string>());

                        if (actionInfo.AttributeRoute != null)
                        {
                            actionModel.AttributeRouteModel = new ReflectedAttributeRouteModel(
                                actionInfo.AttributeRoute);
                        }

                        foreach (var parameter in methodInfo.GetParameters())
                        {
                            actionModel.Parameters.Add(new ReflectedParameterModel(parameter));
                        }

                        controllerModel.Actions.Add(actionModel);
                    }
                }
            }

            return applicationModel;
        }

        public List<ReflectedActionDescriptor> Build(ReflectedApplicationModel model)
        {
            var actions = new List<ReflectedActionDescriptor>();

            var hasAttributeRoutes = false;
            var removalConstraints = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var methodInfoMap = new MethodToActionMap();

            var routeTemplateErrors = new List<string>();
            var attributeRoutingConfigurationErrors = new Dictionary<MethodInfo, string>();

            foreach (var controller in model.Controllers)
            {
                var controllerDescriptor = new ControllerDescriptor(controller.ControllerType);
                foreach (var action in controller.Actions)
                {
                    // Controllers with multiple [Route] attributes (or user defined implementation of
                    // IRouteTemplateProvider) will generate one action descriptor per IRouteTemplateProvider
                    // instance.
                    // Actions with multiple [Http*] attributes or other (IRouteTemplateProvider implementations
                    // have already been identified as different actions during action discovery.
                    var actionDescriptors = CreateActionDescriptors(action, controller, controllerDescriptor);

                    foreach (var actionDescriptor in actionDescriptors)
                    {
                        AddApiExplorerInfo(actionDescriptor, action, controller);
                        AddActionFilters(actionDescriptor, action.Filters, controller.Filters, model.Filters);
                        AddActionConstraints(actionDescriptor, action, controller);
                        AddControllerRouteConstraints(
                            actionDescriptor,
                            controller.RouteConstraints,
                            removalConstraints);

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
                            RouteKeyHandling.DenyKey));
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
                            actionDescriptor.RouteValueDefaults.Add(key, null);
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

        private static IList<ReflectedActionDescriptor> CreateActionDescriptors(
            ReflectedActionModel action,
            ReflectedControllerModel controller,
            ControllerDescriptor controllerDescriptor)
        {
            var actionDescriptors = new List<ReflectedActionDescriptor>();

            // We check the action to see if the template allows combination behavior
            // (It doesn't start with / or ~/) so that in the case where we have multiple
            // [Route] attributes on the controller we don't end up creating multiple
            // attribute identical attribute routes.
            if (controller.AttributeRoutes != null &&
                controller.AttributeRoutes.Count > 0 &&
                (action.AttributeRouteModel == null ||
                !action.AttributeRouteModel.IsAbsoluteTemplate))
            {
                foreach (var controllerAttributeRoute in controller.AttributeRoutes)
                {
                    var actionDescriptor = CreateActionDescriptor(
                        action,
                        controllerAttributeRoute,
                        controllerDescriptor);

                    actionDescriptors.Add(actionDescriptor);
                }
            }
            else
            {
                actionDescriptors.Add(CreateActionDescriptor(
                    action,
                    controllerAttributeRoute: null,
                    controllerDescriptor: controllerDescriptor));
            }

            return actionDescriptors;
        }

        private static ReflectedActionDescriptor CreateActionDescriptor(
            ReflectedActionModel action,
            ReflectedAttributeRouteModel controllerAttributeRoute,
            ControllerDescriptor controllerDescriptor)
        {
            var parameterDescriptors = new List<ParameterDescriptor>();
            foreach (var parameter in action.Parameters)
            {
                var isFromBody = parameter.Attributes.OfType<FromBodyAttribute>().Any();
                var paramDescriptor = new ParameterDescriptor()
                {
                    Name = parameter.ParameterName,
                    IsOptional = parameter.IsOptional
                };

                if (isFromBody)
                {
                    paramDescriptor.BodyParameterInfo = new BodyParameterInfo(
                        parameter.ParameterInfo.ParameterType);
                }
                else
                {
                    paramDescriptor.ParameterBindingInfo = new ParameterBindingInfo(
                            parameter.ParameterName,
                            parameter.ParameterInfo.ParameterType);
                }

                parameterDescriptors.Add(paramDescriptor);
            }

            var attributeRouteInfo = CreateAttributeRouteInfo(
                action.AttributeRouteModel,
                controllerAttributeRoute);

            var actionDescriptor = new ReflectedActionDescriptor()
            {
                Name = action.ActionName,
                ControllerDescriptor = controllerDescriptor,
                MethodInfo = action.ActionMethod,
                Parameters = parameterDescriptors,
                RouteConstraints = new List<RouteDataActionConstraint>(),
                AttributeRouteInfo = attributeRouteInfo
            };

            actionDescriptor.DisplayName = string.Format(
                "{0}.{1}",
                action.ActionMethod.DeclaringType.FullName,
                action.ActionMethod.Name);

            return actionDescriptor;
        }

        private static void AddApiExplorerInfo(
            ReflectedActionDescriptor actionDescriptor,
            ReflectedActionModel action, 
            ReflectedControllerModel controller)
        {
            var apiExplorerIsVisible = action.ApiExplorerIsVisible ?? controller.ApiExplorerIsVisible ?? false;
            if (apiExplorerIsVisible)
            {
                var apiExplorerActionData = new ApiDescriptionActionData()
                {
                    GroupName = action.ApiExplorerGroupName ?? controller.ApiExplorerGroupName,
                };  

                actionDescriptor.SetProperty(apiExplorerActionData);
            }
        }

        private static void AddActionFilters(
            ReflectedActionDescriptor actionDescriptor,
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
            ReflectedAttributeRouteModel action,
            ReflectedAttributeRouteModel controller)
        {
            var combinedRoute = ReflectedAttributeRouteModel.CombineReflectedAttributeRouteModel(
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
            ReflectedActionDescriptor actionDescriptor,
            ReflectedActionModel action,
            ReflectedControllerModel controller)
        {
            var httpMethods = action.HttpMethods;
            if (httpMethods != null && httpMethods.Count > 0)
            {
                actionDescriptor.MethodConstraints = new List<HttpMethodConstraint>()
                {
                    new HttpMethodConstraint(httpMethods)
                };
            }

            actionDescriptor.RouteConstraints.Add(new RouteDataActionConstraint(
                "controller",
                controller.ControllerName));

            if (action.IsActionNameMatchRequired)
            {
                actionDescriptor.RouteConstraints.Add(new RouteDataActionConstraint(
                    "action",
                    action.ActionName));
            }
            else
            {
                actionDescriptor.RouteConstraints.Add(new RouteDataActionConstraint(
                    "action",
                    RouteKeyHandling.DenyKey));
            }
        }

        private static void AddControllerRouteConstraints(
            ReflectedActionDescriptor actionDescriptor,
            IList<RouteConstraintAttribute> routeconstraints,
            ISet<string> removalConstraints)
        {
            // Apply all the constraints defined on the controller (for example, [Area]) to the actions
            // in that controller. Also keep track of all the constraints that require preventing actions
            // without the constraint to match. For example, actions without an [Area] attribute on their
            // controller should not match when a value has been given for area when matching a url or
            // generating a link.
            foreach (var constraintAttribute in routeconstraints)
            {
                if (constraintAttribute.BlockNonAttributedActions)
                {
                    removalConstraints.Add(constraintAttribute.RouteKey);
                }

                // Skip duplicates
                if (!HasConstraint(actionDescriptor.RouteConstraints, constraintAttribute.RouteKey))
                {
                    if (constraintAttribute.RouteValue == null)
                    {
                        actionDescriptor.RouteConstraints.Add(new RouteDataActionConstraint(
                            constraintAttribute.RouteKey,
                            constraintAttribute.RouteKeyHandling));
                    }
                    else
                    {
                        actionDescriptor.RouteConstraints.Add(new RouteDataActionConstraint(
                            constraintAttribute.RouteKey,
                            constraintAttribute.RouteValue));
                    }
                }
            }
        }

        private static bool HasConstraint(List<RouteDataActionConstraint> constraints, string routeKey)
        {
            return constraints.Any(
                rc => string.Equals(rc.RouteKey, routeKey, StringComparison.OrdinalIgnoreCase));
        }

        private static void ReplaceRouteConstraints(ReflectedActionDescriptor actionDescriptor)
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
            ReflectedActionDescriptor actionDescriptor,
            IList<string> routeTemplateErrors)
        {
            try
            {
                actionDescriptor.AttributeRouteInfo.Template = ReflectedAttributeRouteModel.ReplaceTokens(
                    actionDescriptor.AttributeRouteInfo.Template,
                    actionDescriptor.RouteValueDefaults);
            }
            catch (InvalidOperationException ex)
            {
                var message = Resources.FormatAttributeRoute_IndividualErrorMessage(
                    actionDescriptor.DisplayName,
                    Environment.NewLine,
                    ex.Message);

                routeTemplateErrors.Add(message);
            }
        }

        private static void AddConstraintsAsDefaultRouteValues(ReflectedActionDescriptor actionDescriptor)
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
            ReflectedActionDescriptor actionDescriptor,
            ISet<string> removalConstraints)
        {
            foreach (var key in removalConstraints)
            {
                if (!HasConstraint(actionDescriptor.RouteConstraints, key))
                {
                    actionDescriptor.RouteConstraints.Add(new RouteDataActionConstraint(
                        key,
                        RouteKeyHandling.DenyKey));
                }
            }
        }

        private static void AddActionToNamedGroup(
            IDictionary<string, IList<ActionDescriptor>> actionsByRouteName,
            string routeName,
            ReflectedActionDescriptor actionDescriptor)
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

        private static bool IsAttributeRoutedAction(ReflectedActionDescriptor actionDescriptor)
        {
            return actionDescriptor.AttributeRouteInfo != null &&
                actionDescriptor.AttributeRouteInfo.Template != null;
        }

        private static IList<string> AddErrorNumbers(
            IEnumerable<string> namedRoutedErrors)
        {
            return namedRoutedErrors
                .Select((nre, i) =>
                            Resources.FormatAttributeRoute_AggregateErrorMessage_ErrorNumber(
                                i + 1,
                                Environment.NewLine,
                                nre))
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

        private void ValidateActionGroupConfiguration(
            IDictionary<MethodInfo, IDictionary<ReflectedActionModel, IList<ReflectedActionDescriptor>>> methodMap,
            ReflectedActionDescriptor actionDescriptor,
            IDictionary<MethodInfo, string> routingConfigurationErrors)
        {
            string combinedErrorMessage = null;

            var hasAttributeRoutedActions = false;
            var hasConventionallyRoutedActions = false;

            var invalidHttpMethodActions = new Dictionary<ReflectedActionModel, IEnumerable<string>>();

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

                // Keep a list of actions with possible invalid IHttpActionMethodProvider attributes
                // to generate an error in case the method generates attribute routed actions.
                ValidateActionHttpMethodProviders(reflectedAction.Key, invalidHttpMethodActions);
            }

            // Validate that no method result in attribute and non attribute actions at the same time.
            // By design, mixing attribute and conventionally actions in the same method is not allowed.
            // This is for example the case when someone uses[HttpGet("Products")] and[HttpPost]
            // on the same  method.
            if (hasAttributeRoutedActions && hasConventionallyRoutedActions)
            {
                combinedErrorMessage = CreateMixedRoutedActionDescriptorsErrorMessage(
                    actionDescriptor,
                    actionsForMethod);
            }

            // Validate that no method that creates attribute routed actions and
            // also uses attributes that only constrain the set of HTTP methods. For example,
            // if an attribute that implements IActionHttpMethodProvider but does not implement
            // IRouteTemplateProvider is used with an attribute that implements IRouteTemplateProvider on
            // the same action, the HTTP methods provided by the attribute that only implements
            // IActionHttpMethodProvider would be silently ignored, so we choose to throw to
            // inform the user of the invalid configuration.
            if (hasAttributeRoutedActions && invalidHttpMethodActions.Any())
            {
                var errorMessage = CreateInvalidActionHttpMethodProviderErrorMessage(
                    actionDescriptor,
                    invalidHttpMethodActions,
                    actionsForMethod);

                combinedErrorMessage = CombineErrorMessage(combinedErrorMessage, errorMessage);
            }

            if (combinedErrorMessage != null)
            {
                routingConfigurationErrors.Add(actionDescriptor.MethodInfo, combinedErrorMessage);
            }
        }

        private static void ValidateActionHttpMethodProviders(
            ReflectedActionModel reflectedAction,
            IDictionary<ReflectedActionModel, IEnumerable<string>> invalidHttpMethodActions)
        {
            var invalidHttpMethodProviderAttributes = reflectedAction.Attributes
                .Where(attr => attr is IActionHttpMethodProvider &&
                       !(attr is IRouteTemplateProvider))
                .Select(attr => attr.GetType().FullName);

            if (invalidHttpMethodProviderAttributes.Any())
            {
                invalidHttpMethodActions.Add(
                    reflectedAction,
                    invalidHttpMethodProviderAttributes);
            }
        }

        private static string CombineErrorMessage(string combinedErrorMessage, string errorMessage)
        {
            if (combinedErrorMessage == null)
            {
                combinedErrorMessage = errorMessage;
            }
            else
            {
                combinedErrorMessage = string.Join(
                    Environment.NewLine,
                    combinedErrorMessage,
                    errorMessage);
            }

            return combinedErrorMessage;
        }

        private static string CreateInvalidActionHttpMethodProviderErrorMessage(
            ReflectedActionDescriptor actionDescriptor,
            IDictionary<ReflectedActionModel, IEnumerable<string>> invalidHttpMethodActions,
            IDictionary<ReflectedActionModel, IList<ReflectedActionDescriptor>> actionsForMethod)
        {
            var messagesForMethodInfo = new List<string>();
            foreach (var invalidAction in invalidHttpMethodActions)
            {
                var invalidAttributesList = string.Join(", ", invalidAction.Value);

                foreach (var descriptor in actionsForMethod[invalidAction.Key])
                {
                    // We only report errors in attribute routed actions. For example, an action
                    // that contains [HttpGet("Products")], [HttpPost] and [HttpHead], where [HttpHead]
                    // only implements IHttpActionMethodProvider and restricts the action to only allow
                    // the head method, will report that the action contains invalid IActionHttpMethodProvider
                    // attributes only for the action generated by [HttpGet("Products")].
                    // [HttpPost] will be treated as an action that produces a conventionally routed action
                    // and the fact that the method generates attribute and non attributed actions will be
                    // reported as a different error.
                    if (IsAttributeRoutedAction(descriptor))
                    {
                        var messageItem = Resources.FormatAttributeRoute_InvalidHttpConstraints_Item(
                            descriptor.DisplayName,
                            descriptor.AttributeRouteInfo.Template,
                            invalidAttributesList,
                            typeof(IActionHttpMethodProvider).FullName);

                        messagesForMethodInfo.Add(messageItem);
                    }
                }
            }

            var methodFullName = string.Format("{0}.{1}",
                actionDescriptor.MethodInfo.DeclaringType.FullName,
                actionDescriptor.MethodInfo.Name);

            // Sample message:
            // A method 'MyApplication.CustomerController.Index' that defines attribute routed actions must
            // not have attributes that implement 'Microsoft.AspNet.Mvc.IActionHttpMethodProvider'
            // and do not implement 'Microsoft.AspNet.Mvc.Routing.IRouteTemplateProvider':
            // Action 'MyApplication.CustomerController.Index' has 'Namespace.CustomHttpMethodAttribute'
            // invalid 'Microsoft.AspNet.Mvc.IActionHttpMethodProvider' attributes.
            return
                Resources.FormatAttributeRoute_InvalidHttpConstraints(
                    methodFullName,
                    typeof(IActionHttpMethodProvider).FullName,
                    typeof(IRouteTemplateProvider).FullName,
                    Environment.NewLine,
                    string.Join(Environment.NewLine, messagesForMethodInfo));
        }

        private static string CreateMixedRoutedActionDescriptorsErrorMessage(
            ReflectedActionDescriptor actionDescriptor,
            IDictionary<ReflectedActionModel, IList<ReflectedActionDescriptor>> actionsForMethod)
        {
            // Text to show as the attribute route template for conventionally routed actions.
            var nullTemplate = Resources.AttributeRoute_NullTemplateRepresentation;

            var actionDescriptions = actionsForMethod
                .SelectMany(a => a.Value)
                .Select(ad =>
                    Resources.FormatAttributeRoute_MixedAttributeAndConventionallyRoutedActions_ForMethod_Item(
                        ad.DisplayName,
                        ad.AttributeRouteInfo != null ? ad.AttributeRouteInfo.Template : nullTemplate));

            var methodFullName = string.Format("{0}.{1}",
                    actionDescriptor.MethodInfo.DeclaringType.FullName,
                    actionDescriptor.MethodInfo.Name);

            // Sample error message:
            // A method 'MyApplication.CustomerController.Index' must not define attributed actions and
            // non attributed actions at the same time:
            // Action: 'MyApplication.CustomerController.Index' - Template: 'Products'
            // Action: 'MyApplication.CustomerController.Index' - Template: '(none)'
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
            var group = string.Format("{0}-{1}", order, template);
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
            Dictionary<MethodInfo, IDictionary<ReflectedActionModel, IList<ReflectedActionDescriptor>>>
        {
            public void AddToMethodInfo(ReflectedActionModel action,
                IList<ReflectedActionDescriptor> actionDescriptors)
            {
                IDictionary<ReflectedActionModel, IList<ReflectedActionDescriptor>> actionsForMethod = null;
                if (TryGetValue(action.ActionMethod, out actionsForMethod))
                {
                    actionsForMethod.Add(action, actionDescriptors);
                }
                else
                {
                    var reflectedActionMap =
                        new Dictionary<ReflectedActionModel, IList<ReflectedActionDescriptor>>();
                    reflectedActionMap.Add(action, actionDescriptors);
                    Add(action.ActionMethod, reflectedActionMap);
                }
            }
        }
    }
}
