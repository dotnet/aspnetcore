// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Routing.Template;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Mvc.Routing
{
    public class AttributeRoute : IRouter
    {
        private readonly IRouter _target;
        private readonly IActionDescriptorsCollectionProvider _actionDescriptorsCollectionProvider;
        private readonly IInlineConstraintResolver _constraintResolver;

        // These loggers are used by the inner route, keep them around to avoid re-creating.
        private readonly ILogger _routeLogger;
        private readonly ILogger _constraintLogger;

        private InnerAttributeRoute _inner;

        public AttributeRoute(
            [NotNull] IRouter target,
            [NotNull] IActionDescriptorsCollectionProvider actionDescriptorsCollectionProvider,
            [NotNull] IInlineConstraintResolver constraintResolver,
            [NotNull] ILoggerFactory loggerFactory)
        {
            _target = target;
            _actionDescriptorsCollectionProvider = actionDescriptorsCollectionProvider;
            _constraintResolver = constraintResolver;

            _routeLogger = loggerFactory.CreateLogger<InnerAttributeRoute>();
            _constraintLogger = loggerFactory.CreateLogger(typeof(RouteConstraintMatcher).FullName);
        }

        /// <inheritdoc />
        public VirtualPathData GetVirtualPath(VirtualPathContext context)
        {
            var route = GetInnerRoute();
            return route.GetVirtualPath(context);
        }

        /// <inheritdoc />
        public Task RouteAsync(RouteContext context)
        {
            var route = GetInnerRoute();
            return route.RouteAsync(context);
        }

        private InnerAttributeRoute GetInnerRoute()
        {
            var actions = _actionDescriptorsCollectionProvider.ActionDescriptors;

            // This is a safe-race. We'll never set inner back to null after initializing
            // it on startup.
            if (_inner == null || _inner.Version != actions.Version)
            {
                _inner = BuildRoute(actions);
            }

            return _inner;
        }

        private InnerAttributeRoute BuildRoute(ActionDescriptorsCollection actions)
        {
            var routeInfos = GetRouteInfos(_constraintResolver, actions.Items);

            // We're creating one AttributeRouteGenerationEntry per action. This allows us to match the intended
            // action by expected route values, and then use the TemplateBinder to generate the link.
            var generationEntries = new List<AttributeRouteLinkGenerationEntry>();
            foreach (var routeInfo in routeInfos)
            {
                generationEntries.Add(new AttributeRouteLinkGenerationEntry()
                {
                    Binder = new TemplateBinder(routeInfo.ParsedTemplate, routeInfo.Defaults),
                    Defaults = routeInfo.Defaults,
                    Constraints = routeInfo.Constraints,
                    Order = routeInfo.Order,
                    Precedence = routeInfo.Precedence,
                    RequiredLinkValues = routeInfo.ActionDescriptor.RouteValueDefaults,
                    RouteGroup = routeInfo.RouteGroup,
                    Template = routeInfo.ParsedTemplate,
                    TemplateText = routeInfo.RouteTemplate,
                    Name = routeInfo.Name,
                });
            }

            // We're creating one AttributeRouteMatchingEntry per group, so we need to identify the distinct set of
            // groups. It's guaranteed that all members of the group have the same template and precedence,
            // so we only need to hang on to a single instance of the RouteInfo for each group.
            var distinctRouteInfosByGroup = GroupRouteInfosByGroupId(routeInfos);
            var matchingEntries = new List<AttributeRouteMatchingEntry>();
            foreach (var routeInfo in distinctRouteInfosByGroup)
            {
                matchingEntries.Add(new AttributeRouteMatchingEntry()
                {
                    Order = routeInfo.Order,
                    Precedence = routeInfo.Precedence,
                    Target = _target,
                    RouteName = routeInfo.Name,
                    RouteTemplate = routeInfo.RouteTemplate,
                    TemplateMatcher = new TemplateMatcher(
                        routeInfo.ParsedTemplate,
                        new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                        {
                            { AttributeRouting.RouteGroupKey, routeInfo.RouteGroup }
                        }),
                    Constraints = routeInfo.Constraints
                });
            }

            return new InnerAttributeRoute(
                _target,
                matchingEntries,
                generationEntries,
                _routeLogger,
                _constraintLogger,
                actions.Version);
        }

        private static IEnumerable<RouteInfo> GroupRouteInfosByGroupId(List<RouteInfo> routeInfos)
        {
            var routeInfosByGroupId = new Dictionary<string, RouteInfo>(StringComparer.OrdinalIgnoreCase);

            foreach (var routeInfo in routeInfos)
            {
                if (!routeInfosByGroupId.ContainsKey(routeInfo.RouteGroup))
                {
                    routeInfosByGroupId.Add(routeInfo.RouteGroup, routeInfo);
                }
            }

            return routeInfosByGroupId.Values;
        }

        private static List<RouteInfo> GetRouteInfos(
            IInlineConstraintResolver constraintResolver,
            IReadOnlyList<ActionDescriptor> actions)
        {
            var routeInfos = new List<RouteInfo>();
            var errors = new List<RouteInfo>();

            // This keeps a cache of 'Template' objects. It's a fairly common case that multiple actions
            // will use the same route template string; thus, the `Template` object can be shared.
            //
            // For a relatively simple route template, the `Template` object will hold about 500 bytes
            // of memory, so sharing is worthwhile.
            var templateCache = new Dictionary<string, RouteTemplate>(StringComparer.OrdinalIgnoreCase);

            var attributeRoutedActions = actions.Where(a => a.AttributeRouteInfo != null &&
                a.AttributeRouteInfo.Template != null);
            foreach (var action in attributeRoutedActions)
            {
                var routeInfo = GetRouteInfo(constraintResolver, templateCache, action);
                if (routeInfo.ErrorMessage == null)
                {
                    routeInfos.Add(routeInfo);
                }
                else
                {
                    errors.Add(routeInfo);
                }
            }

            if (errors.Count > 0)
            {
                var allErrors = string.Join(
                    Environment.NewLine + Environment.NewLine,
                    errors.Select(
                        e => Resources.FormatAttributeRoute_IndividualErrorMessage(
                            e.ActionDescriptor.DisplayName,
                            Environment.NewLine,
                            e.ErrorMessage)));

                var message = Resources.FormatAttributeRoute_AggregateErrorMessage(Environment.NewLine, allErrors);
                throw new InvalidOperationException(message);
            }

            return routeInfos;
        }

        private static RouteInfo GetRouteInfo(
            IInlineConstraintResolver constraintResolver,
            Dictionary<string, RouteTemplate> templateCache,
            ActionDescriptor action)
        {
            var constraint = action.RouteConstraints
                .Where(c => c.RouteKey == AttributeRouting.RouteGroupKey)
                .FirstOrDefault();
            if (constraint == null ||
                constraint.KeyHandling != RouteKeyHandling.RequireKey ||
                constraint.RouteValue == null)
            {
                // This can happen if an ActionDescriptor has a route template, but doesn't have one of our
                // special route group constraints. This is a good indication that the user is using a 3rd party
                // routing system, or has customized their ADs in a way that we can no longer understand them.
                //
                // We just treat this case as an 'opt-out' of our attribute routing system.
                return null;
            }

            var routeInfo = new RouteInfo()
            {
                ActionDescriptor = action,
                RouteGroup = constraint.RouteValue,
                RouteTemplate = action.AttributeRouteInfo.Template,
            };

            try
            {
                RouteTemplate parsedTemplate;
                if (!templateCache.TryGetValue(action.AttributeRouteInfo.Template, out parsedTemplate))
                {
                    // Parsing with throw if the template is invalid.
                    parsedTemplate = TemplateParser.Parse(action.AttributeRouteInfo.Template);
                    templateCache.Add(action.AttributeRouteInfo.Template, parsedTemplate);
                }

                routeInfo.ParsedTemplate = parsedTemplate;
            }
            catch (Exception ex)
            {
                routeInfo.ErrorMessage = ex.Message;
                return routeInfo;
            }

            foreach (var kvp in action.RouteValueDefaults)
            {
                foreach (var parameter in routeInfo.ParsedTemplate.Parameters)
                {
                    if (string.Equals(kvp.Key, parameter.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        routeInfo.ErrorMessage = Resources.FormatAttributeRoute_CannotContainParameter(
                            routeInfo.RouteTemplate,
                            kvp.Key,
                            kvp.Value);

                        return routeInfo;
                    }
                }
            }

            routeInfo.Order = action.AttributeRouteInfo.Order;

            routeInfo.Precedence = AttributeRoutePrecedence.Compute(routeInfo.ParsedTemplate);

            routeInfo.Name = action.AttributeRouteInfo.Name;

            var constraintBuilder = new RouteConstraintBuilder(constraintResolver, routeInfo.RouteTemplate);

            foreach (var parameter in routeInfo.ParsedTemplate.Parameters)
            {
                if (parameter.InlineConstraints != null)
                {
                    if (parameter.IsOptional)
                    {
                        constraintBuilder.SetOptional(parameter.Name);
                    }

                    foreach (var inlineConstraint in parameter.InlineConstraints)
                    {
                        constraintBuilder.AddResolvedConstraint(parameter.Name, inlineConstraint.Constraint);
                    }
                }
            }

            routeInfo.Constraints = constraintBuilder.Build();

            routeInfo.Defaults = routeInfo.ParsedTemplate.Parameters
                .Where(p => p.DefaultValue != null)
                .ToDictionary(p => p.Name, p => p.DefaultValue, StringComparer.OrdinalIgnoreCase);

            return routeInfo;
        }

        private class RouteInfo
        {
            public ActionDescriptor ActionDescriptor { get; set; }

            public IReadOnlyDictionary<string, IRouteConstraint> Constraints { get; set; }

            public IReadOnlyDictionary<string, object> Defaults { get; set; }

            public string ErrorMessage { get; set; }

            public RouteTemplate ParsedTemplate { get; set; }

            public int Order { get; set; }

            public decimal Precedence { get; set; }

            public string RouteGroup { get; set; }

            public string RouteTemplate { get; set; }

            public string Name { get; set; }
        }
    }
}
