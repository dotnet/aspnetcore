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
    public class TreeRouteBuilder
    {
        private readonly ILogger _logger;
        private readonly ILogger _constraintLogger;
        private readonly UrlEncoder _urlEncoder;
        private readonly ObjectPool<UriBuildingContext> _objectPool;
        private readonly IInlineConstraintResolver _constraintResolver;

        public TreeRouteBuilder(
            ILoggerFactory loggerFactory,
            UrlEncoder urlEncoder,
            ObjectPool<UriBuildingContext> objectPool,
            IInlineConstraintResolver constraintResolver)
        {
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            if (urlEncoder == null)
            {
                throw new ArgumentNullException(nameof(urlEncoder));
            }

            if (objectPool == null)
            {
                throw new ArgumentNullException(nameof(objectPool));
            }

            if (constraintResolver == null)
            {
                throw new ArgumentNullException(nameof(constraintResolver));
            }

            _urlEncoder = urlEncoder;
            _objectPool = objectPool;
            _constraintResolver = constraintResolver;

            _logger = loggerFactory.CreateLogger<TreeRouter>();
            _constraintLogger = loggerFactory.CreateLogger(typeof(RouteConstraintMatcher).FullName);
        }

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

        public IList<InboundRouteEntry> InboundEntries { get; } = new List<InboundRouteEntry>();

        public IList<OutboundRouteEntry> OutboundEntries { get; } = new List<OutboundRouteEntry>();

        public TreeRouter Build()
        {
            return Build(version: 0);
        }

        public TreeRouter Build(int version)
        {
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

        public void Clear()
        {
            InboundEntries.Clear();
            OutboundEntries.Clear();
        }

        private void AddEntryToTree(UrlMatchingTree tree, InboundRouteEntry entry)
        {
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

                if (part.IsParameter && (part.IsOptional || part.IsCatchAll))
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
    }
}
