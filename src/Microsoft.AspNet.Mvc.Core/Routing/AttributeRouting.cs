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

            // We're creating one AttributeRouteEntry per group, so we need to identify the distinct set of
            // groups. It's guaranteed that all members of the group have the same template and precedence,
            // so we only need to hang on to a single instance of the template.
            var routeTemplatesByGroup = GroupTemplatesByGroupId(actions);

            var inlineConstraintResolver = services.GetService<IInlineConstraintResolver>();

            var entries = new List<AttributeRouteEntry>();
            foreach (var routeGroup in routeTemplatesByGroup)
            {
                var routeGroupId = routeGroup.Key;
                var template = routeGroup.Value;

                var parsedTemplate = TemplateParser.Parse(template, inlineConstraintResolver);
                var precedence = AttributeRoutePrecedence.Compute(parsedTemplate);

                entries.Add(new AttributeRouteEntry()
                {
                    Precedence = precedence,
                    Route = new TemplateRoute(
                        target,
                        template,
                        defaults: new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                        {
                            { RouteGroupKey, routeGroupId },
                        },
                        constraints: null,
                        inlineConstraintResolver: inlineConstraintResolver),
                });
            }

            return new AttributeRoute(target, entries);
        }

        private static IReadOnlyList<ActionDescriptor> GetActionDescriptors(IServiceProvider services)
        {
            var actionDescriptorProvider = services.GetService<IActionDescriptorsCollectionProvider>();

            var actionDescriptorsCollection = actionDescriptorProvider.ActionDescriptors;
            return actionDescriptorsCollection.Items;
        }

        private static Dictionary<string, string> GroupTemplatesByGroupId(IReadOnlyList<ActionDescriptor> actions)
        {
            var routeTemplatesByGroup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var action in actions.Where(a => a.RouteTemplate != null))
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

                var routeGroup = constraint.RouteValue;
                if (!routeTemplatesByGroup.ContainsKey(routeGroup))
                {
                    routeTemplatesByGroup.Add(routeGroup, action.RouteTemplate);
                }
            }

            return routeTemplatesByGroup;
        }
    }
}