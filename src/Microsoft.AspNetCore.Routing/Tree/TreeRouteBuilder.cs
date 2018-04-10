// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Routing.Internal;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.AspNetCore.Routing.Tree
{
    /// <summary>
    /// Builder for <see cref="TreeRouter"/> instances.
    /// </summary>
    public class TreeRouteBuilder
    {
        private readonly ILogger _logger;
        private readonly ILogger _constraintLogger;
        private readonly UrlEncoder _urlEncoder;
        private readonly ObjectPool<UriBuildingContext> _objectPool;
        private readonly IInlineConstraintResolver _constraintResolver;

        /// <summary>
        /// <para>
        /// This constructor is obsolete and will be removed in a future version. The recommended
        /// alternative is the overload that does not take a UrlEncoder.
        /// </para>
        /// <para>Initializes a new instance of <see cref="TreeRouteBuilder"/>.</para>
        /// </summary>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        /// <param name="urlEncoder">The <see cref="UrlEncoder"/>.</param>
        /// <param name="objectPool">The <see cref="ObjectPool{UrlBuildingContext}"/>.</param>
        /// <param name="constraintResolver">The <see cref="IInlineConstraintResolver"/>.</param>
        [Obsolete("This constructor is obsolete and will be removed in a future version. The recommended " +
            "alternative is the overload that does not take a UrlEncoder.")]
        public TreeRouteBuilder(
            ILoggerFactory loggerFactory,
            UrlEncoder urlEncoder,
            ObjectPool<UriBuildingContext> objectPool,
            IInlineConstraintResolver constraintResolver)
            : this(loggerFactory, objectPool, constraintResolver)
        {
            if (urlEncoder == null)
            {
                throw new ArgumentNullException(nameof(urlEncoder));
            }

            _urlEncoder = urlEncoder;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="TreeRouteBuilder"/>.
        /// </summary>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        /// <param name="objectPool">The <see cref="ObjectPool{UrlBuildingContext}"/>.</param>
        /// <param name="constraintResolver">The <see cref="IInlineConstraintResolver"/>.</param>
        public TreeRouteBuilder(
            ILoggerFactory loggerFactory,
            ObjectPool<UriBuildingContext> objectPool,
            IInlineConstraintResolver constraintResolver)
        {
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            if (objectPool == null)
            {
                throw new ArgumentNullException(nameof(objectPool));
            }

            if (constraintResolver == null)
            {
                throw new ArgumentNullException(nameof(constraintResolver));
            }

            _urlEncoder = UrlEncoder.Default;
            _objectPool = objectPool;
            _constraintResolver = constraintResolver;

            _logger = loggerFactory.CreateLogger<TreeRouter>();
            _constraintLogger = loggerFactory.CreateLogger(typeof(RouteConstraintMatcher).FullName);
        }

        /// <summary>
        /// Adds a new inbound route to the <see cref="TreeRouter"/>.
        /// </summary>
        /// <param name="handler">The <see cref="IRouter"/> for handling the route.</param>
        /// <param name="routeTemplate">The <see cref="RouteTemplate"/> of the route.</param>
        /// <param name="routeName">The route name.</param>
        /// <param name="order">The route order.</param>
        /// <returns>The <see cref="InboundRouteEntry"/>.</returns>
        public InboundRouteEntry MapInbound(
            IRouter handler,
            RouteTemplate routeTemplate,
            string routeName,
            int order)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            if (routeTemplate == null)
            {
                throw new ArgumentNullException(nameof(routeTemplate));
            }

            var entry = new InboundRouteEntry()
            {
                Handler = handler,
                Order = order,
                Precedence = RoutePrecedence.ComputeInbound(routeTemplate),
                RouteName = routeName,
                RouteTemplate = routeTemplate,
            };

            var constraintBuilder = new RouteConstraintBuilder(_constraintResolver, routeTemplate.TemplateText);
            foreach (var parameter in routeTemplate.Parameters)
            {
                if (parameter.InlineConstraints != null)
                {
                    if (parameter.IsOptional)
                    {
                        constraintBuilder.SetOptional(parameter.Name);
                    }

                    foreach (var constraint in parameter.InlineConstraints)
                    {
                        constraintBuilder.AddResolvedConstraint(parameter.Name, constraint.Constraint);
                    }
                }
            }

            entry.Constraints = constraintBuilder.Build();

            entry.Defaults = new RouteValueDictionary();
            foreach (var parameter in entry.RouteTemplate.Parameters)
            {
                if (parameter.DefaultValue != null)
                {
                    entry.Defaults.Add(parameter.Name, parameter.DefaultValue);
                }
            }

            InboundEntries.Add(entry);
            return entry;
        }

        /// <summary>
        /// Adds a new outbound route to the <see cref="TreeRouter"/>.
        /// </summary>
        /// <param name="handler">The <see cref="IRouter"/> for handling the link generation.</param>
        /// <param name="routeTemplate">The <see cref="RouteTemplate"/> of the route.</param>
        /// <param name="requiredLinkValues">The <see cref="RouteValueDictionary"/> containing the route values.</param>
        /// <param name="routeName">The route name.</param>
        /// <param name="order">The route order.</param>
        /// <returns>The <see cref="OutboundRouteEntry"/>.</returns>
        public OutboundRouteEntry MapOutbound(
            IRouter handler,
            RouteTemplate routeTemplate,
            RouteValueDictionary requiredLinkValues,
            string routeName,
            int order)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            if (routeTemplate == null)
            {
                throw new ArgumentNullException(nameof(routeTemplate));
            }

            if (requiredLinkValues == null)
            {
                throw new ArgumentNullException(nameof(requiredLinkValues));
            }

            var entry = new OutboundRouteEntry()
            {
                Handler = handler,
                Order = order,
                Precedence = RoutePrecedence.ComputeOutbound(routeTemplate),
                RequiredLinkValues = requiredLinkValues,
                RouteName = routeName,
                RouteTemplate = routeTemplate,
            };

            var constraintBuilder = new RouteConstraintBuilder(_constraintResolver, routeTemplate.TemplateText);
            foreach (var parameter in routeTemplate.Parameters)
            {
                if (parameter.InlineConstraints != null)
                {
                    if (parameter.IsOptional)
                    {
                        constraintBuilder.SetOptional(parameter.Name);
                    }

                    foreach (var constraint in parameter.InlineConstraints)
                    {
                        constraintBuilder.AddResolvedConstraint(parameter.Name, constraint.Constraint);
                    }
                }
            }

            entry.Constraints = constraintBuilder.Build();

            entry.Defaults = new RouteValueDictionary();
            foreach (var parameter in entry.RouteTemplate.Parameters)
            {
                if (parameter.DefaultValue != null)
                {
                    entry.Defaults.Add(parameter.Name, parameter.DefaultValue);
                }
            }

            OutboundEntries.Add(entry);
            return entry;
        }

        /// <summary>
        /// Gets the list of <see cref="InboundRouteEntry"/>.
        /// </summary>
        public IList<InboundRouteEntry> InboundEntries { get; } = new List<InboundRouteEntry>();

        /// <summary>
        /// Gets the list of <see cref="OutboundRouteEntry"/>.
        /// </summary>
        public IList<OutboundRouteEntry> OutboundEntries { get; } = new List<OutboundRouteEntry>();

        /// <summary>
        /// Builds a <see cref="TreeRouter"/> with the <see cref="InboundEntries"/>
        /// and <see cref="OutboundEntries"/> defined in this <see cref="TreeRouteBuilder"/>.
        /// </summary>
        /// <returns>The <see cref="TreeRouter"/>.</returns>
        public TreeRouter Build()
        {
            return Build(version: 0);
        }

        /// <summary>
        /// Builds a <see cref="TreeRouter"/> with the <see cref="InboundEntries"/>
        /// and <see cref="OutboundEntries"/> defined in this <see cref="TreeRouteBuilder"/>.
        /// </summary>
        /// <param name="version">The version of the <see cref="TreeRouter"/>.</param>
        /// <returns>The <see cref="TreeRouter"/>.</returns>
        public TreeRouter Build(int version)
        {
            // Tree route builder builds a tree for each of the different route orders defined by
            // the user. When a route needs to be matched, the matching algorithm in tree router
            // just iterates over the trees in ascending order when it tries to match the route.
            var trees = new Dictionary<int, UrlMatchingTree>();

            foreach (var entry in InboundEntries)
            {
                UrlMatchingTree tree;
                if (!trees.TryGetValue(entry.Order, out tree))
                {
                    tree = new UrlMatchingTree(entry.Order);
                    trees.Add(entry.Order, tree);
                }

                AddEntryToTree(tree, entry);
            }

            return new TreeRouter(
                trees.Values.OrderBy(tree => tree.Order).ToArray(),
                OutboundEntries,
                _urlEncoder,
                _objectPool,
                _logger,
                _constraintLogger,
                version);
        }

        /// <summary>
        /// Removes all <see cref="InboundEntries"/> and <see cref="OutboundEntries"/> from this
        /// <see cref="TreeRouteBuilder"/>.
        /// </summary>
        public void Clear()
        {
            InboundEntries.Clear();
            OutboundEntries.Clear();
        }

        private void AddEntryToTree(UrlMatchingTree tree, InboundRouteEntry entry)
        {
            // The url matching tree represents all the routes asociated with a given
            // order. Each node in the tree represents all the different categories
            // a segment can have for which there is a defined inbound route entry.
            // Each node contains a set of Matches that indicate all the routes for which
            // a URL is a potential match. This list contains the routes with the same
            // number of segments and the routes with the same number of segments plus an
            // additional catch all parameter (as it can be empty).
            // For example, for a set of routes like:
            // 'Customer/Index/{id}'
            // '{Controller}/{Action}/{*parameters}'
            //
            // The route tree will look like:
            // Root ->
            //     Literals: Customer ->
            //                   Literals: Index ->
            //                                Parameters: {id}
            //                                                Matches: 'Customer/Index/{id}'
            //     Parameters: {Controller} ->
            //                     Parameters: {Action} ->
            //                                     Matches: '{Controller}/{Action}/{*parameters}'
            //                                     CatchAlls: {*parameters}
            //                                                    Matches: '{Controller}/{Action}/{*parameters}'
            //
            // When the tree router tries to match a route, it iterates the list of url matching trees
            // in ascending order. For each tree it traverses each node starting from the root in the
            // following order: Literals, constrained parameters, parameters, constrained catch all routes, catch alls.
            // When it gets to a node of the same length as the route its trying to match, it simply looks at the list of
            // candidates (which is in precence order) and tries to match the url against it.
            //

            var current = tree.Root;
            var matcher = new TemplateMatcher(entry.RouteTemplate, entry.Defaults);

            for (var i = 0; i < entry.RouteTemplate.Segments.Count; i++)
            {
                var segment = entry.RouteTemplate.Segments[i];
                if (!segment.IsSimple)
                {
                    // Treat complex segments as a constrained parameter
                    if (current.ConstrainedParameters == null)
                    {
                        current.ConstrainedParameters = new UrlMatchingNode(length: i + 1);
                    }

                    current = current.ConstrainedParameters;
                    continue;
                }

                Debug.Assert(segment.Parts.Count == 1);
                var part = segment.Parts[0];
                if (part.IsLiteral)
                {
                    UrlMatchingNode next;
                    if (!current.Literals.TryGetValue(part.Text, out next))
                    {
                        next = new UrlMatchingNode(length: i + 1);
                        current.Literals.Add(part.Text, next);
                    }

                    current = next;
                    continue;
                }

                // We accept templates that have intermediate optional values, but we ignore
                // those values for route matching. For that reason, we need to add the entry
                // to the list of matches, only if the remaining segments are optional. For example:
                // /{controller}/{action=Index}/{id} will be equivalent to /{controller}/{action}/{id}
                // for the purposes of route matching.
                if (part.IsParameter &&
                    RemainingSegmentsAreOptional(entry.RouteTemplate.Segments, i))
                {
                    current.Matches.Add(new InboundMatch() { Entry = entry, TemplateMatcher = matcher });
                }

                if (part.IsParameter && part.InlineConstraints.Any() && !part.IsCatchAll)
                {
                    if (current.ConstrainedParameters == null)
                    {
                        current.ConstrainedParameters = new UrlMatchingNode(length: i + 1);
                    }

                    current = current.ConstrainedParameters;
                    continue;
                }

                if (part.IsParameter && !part.IsCatchAll)
                {
                    if (current.Parameters == null)
                    {
                        current.Parameters = new UrlMatchingNode(length: i + 1);
                    }

                    current = current.Parameters;
                    continue;
                }

                if (part.IsParameter && part.InlineConstraints.Any() && part.IsCatchAll)
                {
                    if (current.ConstrainedCatchAlls == null)
                    {
                        current.ConstrainedCatchAlls = new UrlMatchingNode(length: i + 1) { IsCatchAll = true };
                    }

                    current = current.ConstrainedCatchAlls;
                    continue;
                }

                if (part.IsParameter && part.IsCatchAll)
                {
                    if (current.CatchAlls == null)
                    {
                        current.CatchAlls = new UrlMatchingNode(length: i + 1) { IsCatchAll = true };
                    }

                    current = current.CatchAlls;
                    continue;
                }

                Debug.Fail("We shouldn't get here.");
            }

            current.Matches.Add(new InboundMatch() { Entry = entry, TemplateMatcher = matcher });
            current.Matches.Sort((x, y) =>
            {
                var result = x.Entry.Precedence.CompareTo(y.Entry.Precedence);
                return result == 0 ? x.Entry.RouteTemplate.TemplateText.CompareTo(y.Entry.RouteTemplate.TemplateText) : result;
            });
        }

        private static bool RemainingSegmentsAreOptional(IList<TemplateSegment> segments, int currentParameterIndex)
        {
            for (var i = currentParameterIndex; i < segments.Count; i++)
            {
                if (!segments[i].IsSimple)
                {
                    // /{complex}-{segment}
                    return false;
                }

                var part = segments[i].Parts[0];
                if (!part.IsParameter)
                {
                    // /literal
                    return false;
                }

                var isOptionlCatchAllOrHasDefaultValue = part.IsOptional ||
                    part.IsCatchAll ||
                    part.DefaultValue != null;

                if (!isOptionlCatchAllOrHasDefaultValue)
                {
                    // /{parameter}
                    return false;
                }
            }

            return true;
        }
    }
}
