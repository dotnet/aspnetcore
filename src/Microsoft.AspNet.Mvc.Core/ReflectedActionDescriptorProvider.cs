// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
#if ASPNETCORE50
using System.Reflection;
#endif
using Microsoft.AspNet.Mvc.Core;
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
        private readonly IEnumerable<IFilter> _globalFilters;
        private readonly IEnumerable<IReflectedApplicationModelConvention> _modelConventions;
        private readonly IInlineConstraintResolver _constraintResolver;

        public ReflectedActionDescriptorProvider(IControllerAssemblyProvider controllerAssemblyProvider,
                                                 IActionDiscoveryConventions conventions,
                                                 IEnumerable<IFilter> globalFilters,
                                                 IOptionsAccessor<MvcOptions> optionsAccessor,
                                                 IInlineConstraintResolver constraintResolver)
        {
            _controllerAssemblyProvider = controllerAssemblyProvider;
            _conventions = conventions;
            _globalFilters = globalFilters ?? Enumerable.Empty<IFilter>();
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

        private bool HasConstraint(List<RouteDataActionConstraint> constraints, string routeKey)
        {
            return constraints.Any(
                rc => string.Equals(rc.RouteKey, routeKey, StringComparison.OrdinalIgnoreCase));
        }

        public List<ReflectedActionDescriptor> Build(ReflectedApplicationModel model)
        {
            var actions = new List<ReflectedActionDescriptor>();

            var hasAttributeRoutes = false;
            var removalConstraints = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var routeTemplateErrors = new List<string>();

            foreach (var controller in model.Controllers)
            {
                var controllerDescriptor = new ControllerDescriptor(controller.ControllerType);
                foreach (var action in controller.Actions)
                {
                    var parameterDescriptors = new List<ParameterDescriptor>();
                    foreach (var parameter in action.Parameters)
                    {
                        var isFromBody = parameter.Attributes.OfType<FromBodyAttribute>().Any();

                        parameterDescriptors.Add(new ParameterDescriptor()
                        {
                            Name = parameter.ParameterName,
                            IsOptional = parameter.IsOptional,

                            ParameterBindingInfo = isFromBody
                                ? null
                                : new ParameterBindingInfo(
                                    parameter.ParameterName,
                                    parameter.ParameterInfo.ParameterType),

                            BodyParameterInfo = isFromBody
                                ? new BodyParameterInfo(parameter.ParameterInfo.ParameterType)
                                : null
                        });
                    }

                    var combinedRoute = ReflectedAttributeRouteModel.CombineReflectedAttributeRouteModel(
                            controller.AttributeRouteModel,
                            action.AttributeRouteModel);

                    var attributeRouteInfo = combinedRoute == null ? null : new AttributeRouteInfo()
                    {
                        Template = combinedRoute.Template,
                        Order = combinedRoute.Order ?? DefaultAttributeRouteOrder,
                        Name = combinedRoute.Name,
                    };

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

                    foreach (var constraintAttribute in controller.RouteConstraints)
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

                    if (actionDescriptor.AttributeRouteInfo != null &&
                        actionDescriptor.AttributeRouteInfo.Template != null)
                    {
                        hasAttributeRoutes = true;
                        // An attribute routed action will ignore conventional routed constraints. We still
                        // want to provide these values as ambient values.
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

                        // Replaces tokens like [controller]/[action] in the route template with the actual values
                        // for this action.
                        var templateText = actionDescriptor.AttributeRouteInfo.Template;
                        try
                        {
                            templateText = ReflectedAttributeRouteModel.ReplaceTokens(
                                templateText,
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

                        actionDescriptor.AttributeRouteInfo.Template = templateText;

                        var routeGroupValue = GetRouteGroupValue(
                            actionDescriptor.AttributeRouteInfo.Order,
                            templateText);

                        var routeConstraints = new List<RouteDataActionConstraint>();
                        routeConstraints.Add(new RouteDataActionConstraint(
                            AttributeRouting.RouteGroupKey,
                            routeGroupValue));

                        actionDescriptor.RouteConstraints = routeConstraints;
                    }

                    actionDescriptor.FilterDescriptors =
                        action.Filters.Select(f => new FilterDescriptor(f, FilterScope.Action))
                        .Concat(controller.Filters.Select(f => new FilterDescriptor(f, FilterScope.Controller)))
                        .Concat(model.Filters.Select(f => new FilterDescriptor(f, FilterScope.Global)))
                        .OrderBy(d => d, FilterDescriptorOrderComparer.Comparer)
                        .ToList();

                    actions.Add(actionDescriptor);
                }
            }

            var actionsByRouteName = new Dictionary<string, IList<ActionDescriptor>>(
                StringComparer.OrdinalIgnoreCase);

            foreach (var actionDescriptor in actions)
            {
                if (actionDescriptor.AttributeRouteInfo == null ||
                    actionDescriptor.AttributeRouteInfo.Template == null)
                {
                    // Any any attribute routes are in use, then non-attribute-routed ADs can't be selected
                    // when a route group returned by the route.
                    if (hasAttributeRoutes)
                    {
                        actionDescriptor.RouteConstraints.Add(new RouteDataActionConstraint(
                            AttributeRouting.RouteGroupKey,
                            RouteKeyHandling.DenyKey));
                    }

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
                else
                {
                    var attributeRouteInfo = actionDescriptor.AttributeRouteInfo;
                    if (attributeRouteInfo.Name != null)
                    {
                        // Build a map of attribute route name to action descriptors to ensure that all
                        // attribute routes with a given name have the same template.
                        IList<ActionDescriptor> namedActionGroup;

                        if (actionsByRouteName.TryGetValue(attributeRouteInfo.Name, out namedActionGroup))
                        {
                            namedActionGroup.Add(actionDescriptor);
                        }
                        else
                        {
                            namedActionGroup = new List<ActionDescriptor>();
                            namedActionGroup.Add(actionDescriptor);
                            actionsByRouteName.Add(attributeRouteInfo.Name, namedActionGroup);
                        }
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

            var namedRoutedErrors = ValidateNamedAttributeRoutedActions(actionsByRouteName);
            if (namedRoutedErrors.Any())
            {
                namedRoutedErrors = AddErrorNumbers(namedRoutedErrors);

                var message = Resources.FormatAttributeRoute_AggregateErrorMessage(
                    Environment.NewLine,
                    string.Join(Environment.NewLine + Environment.NewLine, namedRoutedErrors));

                throw new InvalidOperationException(message);
            }

            if (routeTemplateErrors.Any())
            {
                var message = Resources.FormatAttributeRoute_AggregateErrorMessage(
                    Environment.NewLine,
                    string.Join(Environment.NewLine + Environment.NewLine, routeTemplateErrors));

                throw new InvalidOperationException(message);
            }

            return actions;
        }

        private static IList<string> AddErrorNumbers(IList<string> namedRoutedErrors)
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

        private static string GetRouteGroupValue(int order, string template)
        {
            var group = string.Format("{0}-{1}", order, template);
            return ("__route__" + group).ToUpperInvariant();
        }
    }
}
