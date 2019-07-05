// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components.Routing;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Resolves components for an application.
    /// </summary>
    internal static class RouteTableFactory
    {
        private static readonly string ComponentAssemblyName = typeof(IComponent).Assembly.FullName;
        private static readonly ConcurrentDictionary<Assembly, RouteTable> Cache =
            new ConcurrentDictionary<Assembly, RouteTable>();
        public static readonly IComparer<RouteEntry> RoutePrecedence = Comparer<RouteEntry>.Create(RouteComparison);

        public static RouteTable Create(Assembly appAssembly)
        {
            if (Cache.TryGetValue(appAssembly, out var resolvedComponents))
            {
                return resolvedComponents;
            }

            var componentTypes = DiscoverComponentTypes(appAssembly);
            var routeTable = Create(componentTypes);
            Cache.TryAdd(appAssembly, routeTable);
            return routeTable;
        }

        internal static RouteTable Create(IEnumerable<Type> types)
        {
            var routes = new List<RouteEntry>();
            foreach (var type in types)
            {
                // We're deliberately using inherit = false here.
                //
                // RouteAttribute is defined as non-inherited, because inheriting a route attribute always causes an
                // ambiguity. You end up with two components (base class and derived class) with the same route.
                var routeAttributes = type.GetCustomAttributes<RouteAttribute>(inherit: false);

                foreach (var routeAttribute in routeAttributes)
                {
                    var template = TemplateParser.ParseTemplate(routeAttribute.Template);
                    var entry = new RouteEntry(template, type);
                    routes.Add(entry);
                }
            }

            return new RouteTable(routes.OrderBy(id => id, RoutePrecedence).ToArray());
        }

        private static IEnumerable<Type> DiscoverComponentTypes(Assembly assembly)
        {
            var candidateAssemblies = new List<Assembly> { assembly };

            var references = assembly.GetReferencedAssemblies();
            foreach (var referencedAssemblyName in assembly.GetReferencedAssemblies())
            {
                var referencedAssembly = Assembly.Load(referencedAssemblyName);
                if (referencedAssembly.GetReferencedAssemblies().Any(r => string.Equals(r.FullName, ComponentAssemblyName, StringComparison.Ordinal)))
                {
                    // The referenced assembly references components. We'll use it as a candidate for component discovery
                    candidateAssemblies.Add(referencedAssembly);
                }
            }

            var componentTypes = candidateAssemblies.SelectMany(c => c.ExportedTypes)
                .Where(t => typeof(IComponent).IsAssignableFrom(t));

            return componentTypes;
        }

        /// <summary>
        /// Route precedence algorithm.
        /// We collect all the routes and sort them from most specific to
        /// less specific. The specificity of a route is given by the specificity
        /// of its segments and the position of those segments in the route.
        /// * A literal segment is more specific than a parameter segment.
        /// * A parameter segment with more constraints is more specific than one with fewer constraints
        /// * Segment earlier in the route are evaluated before segments later in the route.
        /// For example:
        /// /Literal is more specific than /Parameter
        /// /Route/With/{parameter} is more specific than /{multiple}/With/{parameters}
        /// /Product/{id:int} is more specific than /Product/{id}
        ///
        /// Routes can be ambiguous if:
        /// They are composed of literals and those literals have the same values (case insensitive)
        /// They are composed of a mix of literals and parameters, in the same relative order and the
        /// literals have the same values.
        /// For example:
        /// * /literal and /Literal
        /// /{parameter}/literal and /{something}/literal
        /// /{parameter:constraint}/literal and /{something:constraint}/literal
        ///
        /// To calculate the precedence we sort the list of routes as follows:
        /// * Shorter routes go first.
        /// * A literal wins over a parameter in precedence.
        /// * For literals with different values (case insensitive) we choose the lexical order
        /// * For parameters with different numbers of constraints, the one with more wins
        /// If we get to the end of the comparison routing we've detected an ambiguous pair of routes.
        /// </summary>
        internal static int RouteComparison(RouteEntry x, RouteEntry y)
        {
            var xTemplate = x.Template;
            var yTemplate = y.Template;
            if (xTemplate.Segments.Length != y.Template.Segments.Length)
            {
                return xTemplate.Segments.Length < y.Template.Segments.Length ? -1 : 1;
            }
            else
            {
                for (var i = 0; i < xTemplate.Segments.Length; i++)
                {
                    var xSegment = xTemplate.Segments[i];
                    var ySegment = yTemplate.Segments[i];
                    if (!xSegment.IsParameter && ySegment.IsParameter)
                    {
                        return -1;
                    }
                    if (xSegment.IsParameter && !ySegment.IsParameter)
                    {
                        return 1;
                    }

                    if (xSegment.IsParameter)
                    {
                        if (xSegment.Constraints.Length > ySegment.Constraints.Length)
                        {
                            return -1;
                        }
                        else if (xSegment.Constraints.Length < ySegment.Constraints.Length)
                        {
                            return 1;
                        }
                    }
                    else
                    {
                        var comparison = string.Compare(xSegment.Value, ySegment.Value, StringComparison.OrdinalIgnoreCase);
                        if (comparison != 0)
                        {
                            return comparison;
                        }
                    }
                }

                throw new InvalidOperationException($@"The following routes are ambiguous:
'{x.Template.TemplateText}' in '{x.Handler.FullName}'
'{y.Template.TemplateText}' in '{y.Handler.FullName}'
");
            }
        }
    }
}
