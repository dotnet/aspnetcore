// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Routing.Tree
{
    public class TreeRouteBuilder
    {
        private readonly IRouter _target;
        private readonly List<TreeRouteLinkGenerationEntry> _generatingEntries;
        private readonly List<TreeRouteMatchingEntry> _matchingEntries;

        private readonly ILogger _logger;
        private readonly ILogger _constraintLogger;

        public TreeRouteBuilder(IRouter target, ILoggerFactory loggerFactory)
        {
            _target = target;
            _generatingEntries = new List<TreeRouteLinkGenerationEntry>();
            _matchingEntries = new List<TreeRouteMatchingEntry>();

            _logger = loggerFactory.CreateLogger<TreeRouter>();
            _constraintLogger = loggerFactory.CreateLogger(typeof(RouteConstraintMatcher).FullName);
        }

        public void Add(TreeRouteLinkGenerationEntry entry)
        {
            _generatingEntries.Add(entry);
        }

        public void Add(TreeRouteMatchingEntry entry)
        {
            _matchingEntries.Add(entry);
        }

        public TreeRouter Build(int version)
        {
            var trees = new Dictionary<int, UrlMatchingTree>();

            foreach (var entry in _matchingEntries)
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
                _target,
                trees.Values.OrderBy(tree => tree.Order).ToArray(),
                _generatingEntries,
                _logger,
                _constraintLogger,
                version);
        }

        public void Clear()
        {
            _generatingEntries.Clear();
            _matchingEntries.Clear();
        }

        private void AddEntryToTree(UrlMatchingTree tree, TreeRouteMatchingEntry entry)
        {
            var current = tree.Root;

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
                    current.Matches.Add(entry);
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
                        current.ConstrainedCatchAlls = new UrlMatchingNode(length: i + 1);
                    }

                    current = current.ConstrainedCatchAlls;
                    continue;
                }

                if (part.IsParameter && part.IsCatchAll)
                {
                    if (current.CatchAlls == null)
                    {
                        current.CatchAlls = new UrlMatchingNode(length: i + 1);
                    }

                    current = current.CatchAlls;
                    continue;
                }

                Debug.Fail("We shouldn't get here.");
            }

            current.Matches.Add(entry);
            current.Matches.Sort((x, y) =>
            {
                var result = x.Precedence.CompareTo(y.Precedence);
                return result == 0 ? x.RouteTemplate.TemplateText.CompareTo(y.RouteTemplate.TemplateText) : result;
            });
        }
    }
}
