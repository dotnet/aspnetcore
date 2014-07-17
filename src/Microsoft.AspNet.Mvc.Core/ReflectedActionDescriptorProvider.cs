// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
#if K10
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
        public static readonly int DefaultOrder = 0;

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

            var routeGroupsByTemplate = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
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

                    var actionDescriptor = new ReflectedActionDescriptor()
                    {
                        Name = action.ActionName,
                        ControllerDescriptor = controllerDescriptor,
                        MethodInfo = action.ActionMethod,
                        Parameters = parameterDescriptors,
                        RouteConstraints = new List<RouteDataActionConstraint>(),
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
                            actionDescriptor.RouteConstraints.Add(new RouteDataActionConstraint(
                                constraintAttribute.RouteKey,
                                constraintAttribute.RouteValue));
                        }
                    }

                    var templateText = AttributeRouteTemplate.Combine(
                            controller.RouteTemplate,
                            action.RouteTemplate);

                    if (templateText != null)
                    {
                        // An attribute routed action will ignore conventional routed constraints. We still
                        // want to provide these values as ambient values.
                        foreach (var constraint in actionDescriptor.RouteConstraints)
                        {
                            actionDescriptor.RouteValueDefaults.Add(constraint.RouteKey, constraint.RouteValue);
                        }

                        // Replaces tokens like [controller]/[action] in the route template with the actual values
                        // for this action.
                        try
                        {
                            templateText = AttributeRouteTemplate.ReplaceTokens(
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

                        actionDescriptor.AttributeRouteTemplate = templateText;

                        // An attribute routed action is matched by its 'route group' which identifies all equivalent
                        // actions.
                        string routeGroup;
                        if (!routeGroupsByTemplate.TryGetValue(templateText, out routeGroup))
                        {
                            routeGroup = GetRouteGroup(templateText);
                            routeGroupsByTemplate.Add(templateText, routeGroup);
                        }

                        var routeConstraints = new List<RouteDataActionConstraint>();
                        routeConstraints.Add(new RouteDataActionConstraint(
                            AttributeRouting.RouteGroupKey,
                            routeGroup));

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

            foreach (var actionDescriptor in actions)
            {
                foreach (var key in removalConstraints)
                {
                    if (actionDescriptor.AttributeRouteTemplate == null)
                    {
                        // Any any attribute routes are in use, then non-attribute-routed ADs can't be selected
                        // when a route group returned by the route.
                        if (routeGroupsByTemplate.Any())
                        {
                            actionDescriptor.RouteConstraints.Add(new RouteDataActionConstraint(
                                AttributeRouting.RouteGroupKey,
                                RouteKeyHandling.DenyKey));
                        }

                        if (!HasConstraint(actionDescriptor.RouteConstraints, key))
                        {
                            actionDescriptor.RouteConstraints.Add(new RouteDataActionConstraint(
                                key,
                                RouteKeyHandling.DenyKey));
                        }
                    }
                    else
                    {
                        // We still want to add a 'null' for any constraint with DenyKey so that link generation
                        // works properly.
                        //
                        // Consider an action like { area = "", controller = "Home", action = "Index" }. Even if
                        // it's attribute routed, it needs to know that area must be null to generate a link.
                        if (!actionDescriptor.RouteValueDefaults.ContainsKey(key))
                        {
                            actionDescriptor.RouteValueDefaults.Add(key, null);
                        }
                    }
                }
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

        // Returns a unique, stable key per-route-template (OrdinalIgnoreCase)
        private static string GetRouteGroup(string template)
        {
            return ("__route__" + template).ToUpperInvariant();
        }
    }
}
