// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Routing.Template;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Mvc.Routing
{
    public static class AttributeRouting
    {
        // Key used by routing and action selection to match an attribute route entry to a
        // group of action descriptors.
        public static readonly string RouteGroupKey = "!__route_group";

        /// <summary>
        /// Creates an attribute route using the provided services and provided target router.
        /// </summary>
        /// <param name="target">The router to invoke when a route entry matches.</param>
        /// <param name="services">The application services.</param>
        /// <returns>An attribute route.</returns>
        public static IRouter CreateAttributeMegaRoute([NotNull] IRouter target, [NotNull] IServiceProvider services)
        {
            var actions = GetActionDescriptors(services);

            var inlineConstraintResolver = services.GetService<IInlineConstraintResolver>();
            var routeInfos = GetRouteInfos(actions, inlineConstraintResolver);

            // We're creating one AttributeRouteGenerationEntry per action. This allows us to match the intended
            // action by expected route values, and then use the TemplateBinder to generate the link.
            var generationEntries = new List<AttributeRouteLinkGenerationEntry>();
            foreach (var routeInfo in routeInfos)
            {
                var defaults = routeInfo.ParsedTemplate.Parameters
                    .Where(p => p.DefaultValue != null)
                    .ToDictionary(p => p.Name, p => p.DefaultValue, StringComparer.OrdinalIgnoreCase);

                var constraints = routeInfo.ParsedTemplate.Parameters
                    .Where(p => p.InlineConstraint != null)
                    .ToDictionary(p => p.Name, p => p.InlineConstraint, StringComparer.OrdinalIgnoreCase);

                generationEntries.Add(new AttributeRouteLinkGenerationEntry()
                {
                    Binder = new TemplateBinder(routeInfo.ParsedTemplate, defaults),
                    Defaults = defaults,
                    Constraints = constraints,
                    Precedence = routeInfo.Precedence,
                    RequiredLinkValues = routeInfo.ActionDescriptor.RouteValueDefaults,
                    RouteGroup = routeInfo.RouteGroup,
                    Template = routeInfo.ParsedTemplate,
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
                    Precedence = routeInfo.Precedence,
                    Route = new TemplateRoute(
                        target,
                        routeInfo.RouteTemplate,
                        defaults: new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                        {
                            { RouteGroupKey, routeInfo.RouteGroup },
                        },
                        constraints: null,
                        inlineConstraintResolver: inlineConstraintResolver),
                });
            }

            return new AttributeRoute(target, matchingEntries, generationEntries);
        }

        private static IReadOnlyList<ActionDescriptor> GetActionDescriptors(IServiceProvider services)
        {
            var actionDescriptorProvider = services.GetService<IActionDescriptorsCollectionProvider>();

            var actionDescriptorsCollection = actionDescriptorProvider.ActionDescriptors;
            return actionDescriptorsCollection.Items;
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
            IReadOnlyList<ActionDescriptor> actions, 
            IInlineConstraintResolver constraintResolver)
        {
            var routeInfos = new List<RouteInfo>();

            foreach (var action in actions.Where(a => a.AttributeRouteTemplate != null))
            {
                var constraint = action.RouteConstraints
                    .Where(c => c.RouteKey == AttributeRouting.RouteGroupKey)
                    .FirstOrDefault();
                if (constraint == null ||
                    constraint.KeyHandling != RouteKeyHandling.RequireKey ||
                    constraint.RouteValue == null)
                {
                    // This is unlikely to happen by default, but could happen through extensibility. Just ignore it.
                    continue;
                }

                var parsedTemplate = TemplateParser.Parse(action.AttributeRouteTemplate, constraintResolver);
                routeInfos.Add(new RouteInfo()
                {
                    ActionDescriptor = action,
                    ParsedTemplate = parsedTemplate,
                    Precedence = AttributeRoutePrecedence.Compute(parsedTemplate),
                    RouteGroup = constraint.RouteValue,
                    RouteTemplate = action.AttributeRouteTemplate,
                });
            }

            return routeInfos;
        }

        private class RouteInfo
        {
            public ActionDescriptor ActionDescriptor { get; set; }

            public Template ParsedTemplate { get; set; }

            public decimal Precedence { get; set; }

            public string RouteGroup { get; set; }

            public string RouteTemplate { get; set; }
        }
    }
}
