// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Dispatcher.Internal;
using Microsoft.AspNetCore.Dispatcher.Patterns;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Dispatcher
{
    public class TreeMatcher : MatcherBase
    {
        private bool _dataInitialized;
        private object _lock;
        private Cache _cache;
        private IConstraintFactory _constraintFactory;

        private readonly Func<Cache> _initializer;

        public TreeMatcher()
        {
            _lock = new object();
            _initializer = CreateCache;
        }

        public int Version { get; private set; }

        public override async Task MatchAsync(MatcherContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            EnsureServicesInitialized(context);

            var cache = LazyInitializer.EnsureInitialized(ref _cache, ref _dataInitialized, ref _lock, _initializer);

            var values = new DispatcherValueCollection();
            context.Values = values;

            for (var i = 0; i < cache.Trees.Length; i++)
            {
                var tree = cache.Trees[i];
                var tokenizer = new PathTokenizer(context.HttpContext.Request.Path);

                var treenumerator = new Treenumerator(tree.Root, tokenizer);

                while (treenumerator.MoveNext())
                {
                    var node = treenumerator.Current;
                    foreach (var item in node.Matches)
                    {
                        var entry = item.Entry;
                        var matcher = item.RoutePatternMatcher;

                        values.Clear();
                        if (!matcher.TryMatch(context.HttpContext.Request.Path, values))
                        {
                            continue;
                        }

                        Logger.MatchedRoute(entry.RoutePattern.RawText);

                        if (!MatchConstraints(context.HttpContext, values, entry.Constraints))
                        {
                            continue;
                        }

                        await SelectEndpointAsync(context, (entry.Endpoints));
                        if (context.ShortCircuit != null)
                        {
                            Logger.RequestShortCircuited(context);
                            return;
                        }

                        if (context.Endpoint != null)
                        {
                            if (context.Endpoint is IRoutePatternEndpoint templateEndpoint)
                            {
                                foreach (var kvp in templateEndpoint.Values)
                                {
                                    if (!context.Values.ContainsKey(kvp.Key))
                                    {
                                        context.Values[kvp.Key] = kvp.Value;
                                    }
                                }
                            }

                            return;
                        }
                    }
                }
            }
        }

        private bool MatchConstraints(HttpContext httpContext, DispatcherValueCollection values, IDictionary<string, IDispatcherValueConstraint> constraints)
        {
            if (constraints != null)
            {
                foreach (var kvp in constraints)
                {
                    var constraint = kvp.Value;
                    var constraintContext = new DispatcherValueConstraintContext(httpContext, values, ConstraintPurpose.IncomingRequest)
                    {
                        Key = kvp.Key
                    };

                    if (!constraint.Match(constraintContext))
                    {
                        values.TryGetValue(kvp.Key, out var value);

                        Logger.RouteValueDoesNotMatchConstraint(value, kvp.Key, kvp.Value);
                        return false;
                    }
                }
            }

            return true;
        }

        internal Cache CreateCache()
        {
            var endpoints = GetEndpoints();

            var groups = new Dictionary<Key, List<Endpoint>>();

            for (var i = 0; i < endpoints.Count; i++)
            {
                var endpoint = endpoints[i];

                var templateEndpoint = endpoint as IRoutePatternEndpoint;
                if (templateEndpoint == null)
                {
                    continue;
                }

                var order = endpoint.Metadata?.GetMetadata<IEndpointOrderMetadata>()?.Order ?? 0;
                if (!groups.TryGetValue(new Key(order, templateEndpoint.Pattern), out var group))
                {
                    group = new List<Endpoint>();
                    groups.Add(new Key(order, templateEndpoint.Pattern), group);
                }

                group.Add(endpoint);
            }

            var entries = new List<InboundRouteEntry>();
            foreach (var group in groups)
            {
                var routePattern = RoutePattern.Parse(group.Key.RoutePattern);
                var entryExists = entries.Any(item => item.RoutePattern.RawText == routePattern.RawText);
                if (!entryExists)
                {
                    entries.Add(MapInbound(routePattern, group.Value.ToArray(), group.Key.Order));
                }
            }

            var trees = new List<UrlMatchingTree>();
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];

                while (trees.Count <= entry.Order)
                {
                    trees.Add(new UrlMatchingTree(entry.Order));
                }

                var tree = trees[entry.Order];

                AddEntryToTree(tree, entry);
            }

            return new Cache(trees.ToArray());
        }

        private InboundRouteEntry MapInbound(
            RoutePattern routePattern,
            Endpoint[] endpoints,
            int order)
        {
            if (routePattern == null)
            {
                throw new ArgumentNullException(nameof(routePattern));
            }

            var entry = new InboundRouteEntry()
            {
                Precedence = RoutePrecedence.ComputeInbound(routePattern),
                RoutePattern = routePattern,
                Order = order,
                Endpoints = endpoints,
            };

            var constraintBuilder = new DispatcherValueConstraintBuilder(_constraintFactory, routePattern.RawText);
            foreach (var parameter in routePattern.Parameters)
            {
                if (parameter.Constraints != null)
                {
                    if (parameter.IsOptional)
                    {
                        constraintBuilder.SetOptional(parameter.Name);
                    }

                    foreach (var constraint in parameter.Constraints)
                    {
                        constraintBuilder.AddResolvedConstraint(parameter.Name, constraint.RawText);
                    }
                }
            }

            entry.Constraints = constraintBuilder.Build();

            entry.Defaults = new DispatcherValueCollection();
            foreach (var parameter in entry.RoutePattern.Parameters)
            {
                if (parameter.DefaultValue != null)
                {
                    entry.Defaults.Add(parameter.Name, parameter.DefaultValue);
                }
            }
            return entry;
        }

        internal static void AddEntryToTree(UrlMatchingTree tree, InboundRouteEntry entry)
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

            var current = tree.Root;
            var matcher = new RoutePatternMatcher(entry.RoutePattern, entry.Defaults);

            for (var i = 0; i < entry.RoutePattern.PathSegments.Count; i++)
            {
                var segment = entry.RoutePattern.PathSegments[i];
                if (!segment.IsSimple)
                {
                    // Treat complex segments as a constrained parameter
                    if (current.ConstrainedParameters == null)
                    {
                        current.ConstrainedParameters = new UrlMatchingNode(depth: i + 1);
                    }

                    current = current.ConstrainedParameters;
                    continue;
                }

                Debug.Assert(segment.Parts.Count == 1);
                var part = segment.Parts[0];
                if (part.IsLiteral)
                {
                    var literal = (RoutePatternLiteral)part;
                    if (!current.Literals.TryGetValue(literal.Content, out var next))
                    {
                        next = new UrlMatchingNode(depth: i + 1);
                        current.Literals.Add(literal.Content, next);
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
                    RemainingSegmentsAreOptional(entry.RoutePattern.PathSegments, i))
                {
                    current.Matches.Add(new InboundMatch() { Entry = entry, RoutePatternMatcher = matcher });
                }

                var parameter = part as RoutePatternParameter;
                if (parameter != null && parameter.Constraints.Any() && !parameter.IsCatchAll)
                {
                    if (current.ConstrainedParameters == null)
                    {
                        current.ConstrainedParameters = new UrlMatchingNode(depth: i + 1);
                    }

                    current = current.ConstrainedParameters;
                    continue;
                }

                if (parameter != null && !parameter.IsCatchAll)
                {
                    if (current.Parameters == null)
                    {
                        current.Parameters = new UrlMatchingNode(depth: i + 1);
                    }

                    current = current.Parameters;
                    continue;
                }

                if (parameter != null && parameter.Constraints.Any() && parameter.IsCatchAll)
                {
                    if (current.ConstrainedCatchAlls == null)
                    {
                        current.ConstrainedCatchAlls = new UrlMatchingNode(depth: i + 1) { IsCatchAll = true };
                    }

                    current = current.ConstrainedCatchAlls;
                    continue;
                }

                if (parameter != null && parameter.IsCatchAll)
                {
                    if (current.CatchAlls == null)
                    {
                        current.CatchAlls = new UrlMatchingNode(depth: i + 1) { IsCatchAll = true };
                    }

                    current = current.CatchAlls;
                    continue;
                }

                Debug.Fail("We shouldn't get here.");
            }

            current.Matches.Add(new InboundMatch() { Entry = entry, RoutePatternMatcher = matcher });
            current.Matches.Sort((x, y) =>
            {
                var result = x.Entry.Precedence.CompareTo(y.Entry.Precedence);
                return result == 0 ? x.Entry.RoutePattern.RawText.CompareTo(y.Entry.RoutePattern.RawText) : result;
            });
        }

        private static bool RemainingSegmentsAreOptional(IReadOnlyList<RoutePatternPathSegment> segments, int currentParameterIndex)
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

                var parameter = (RoutePatternParameter)part;
                var isOptionlCatchAllOrHasDefaultValue = parameter.IsOptional ||
                    parameter.IsCatchAll ||
                    parameter.DefaultValue != null;

                if (!isOptionlCatchAllOrHasDefaultValue)
                {
                    // /{parameter}
                    return false;
                }
            }

            return true;
        }

        private struct Key : IEquatable<Key>
        {
            public readonly int Order;
            public readonly string RoutePattern;

            public Key(int order, string routePattern)
            {
                Order = order;
                RoutePattern = routePattern;
            }

            public bool Equals(Key other)
            {
                return Order == other.Order && string.Equals(RoutePattern, other.RoutePattern, StringComparison.OrdinalIgnoreCase);
            }

            public override bool Equals(object obj)
            {
                return obj is Key ? Equals((Key)obj) : false;
            }

            public override int GetHashCode()
            {
                var hash = new HashCodeCombiner();
                hash.Add(Order);
                hash.Add(RoutePattern, StringComparer.OrdinalIgnoreCase);
                return hash;
            }
        }

        internal class Cache
        {
            public readonly UrlMatchingTree[] Trees;

            public Cache(UrlMatchingTree[] trees)
            {
                Trees = trees;
            }
        }

        private struct Treenumerator : IEnumerator<UrlMatchingNode>
        {
            private readonly Stack<UrlMatchingNode> _stack;
            private readonly PathTokenizer _tokenizer;

            public Treenumerator(UrlMatchingNode root, PathTokenizer tokenizer)
            {
                _stack = new Stack<UrlMatchingNode>();
                _tokenizer = tokenizer;
                Current = null;

                _stack.Push(root);
            }

            public UrlMatchingNode Current { get; private set; }

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (_stack == null)
                {
                    return false;
                }

                while (_stack.Count > 0)
                {
                    var next = _stack.Pop();

                    // In case of wild card segment, the request path segment length can be greater
                    // Example:
                    // Template:    a/{*path}
                    // Request Url: a/b/c/d
                    if (next.IsCatchAll && next.Matches.Count > 0)
                    {
                        Current = next;
                        return true;
                    }

                    // Next template has the same length as the url we are trying to match
                    // The only possible matching segments are either our current matches or
                    // any catch-all segment after this segment in which the catch all is empty.
                    else if (next.Depth >= _tokenizer.Count)
                    {
                        if (next.Matches.Count > 0)
                        {
                            Current = next;
                            return true;
                        }
                        else
                        {
                            // We can stop looking as any other child node from this node will be
                            // either a literal, a constrained parameter or a parameter.
                            // (Catch alls and constrained catch alls will show up as candidate matches).
                            continue;
                        }
                    }

                    if (next.CatchAlls != null)
                    {
                        _stack.Push(next.CatchAlls);
                    }

                    if (next.ConstrainedCatchAlls != null)
                    {
                        _stack.Push(next.ConstrainedCatchAlls);
                    }

                    if (next.Parameters != null)
                    {
                        _stack.Push(next.Parameters);
                    }

                    if (next.ConstrainedParameters != null)
                    {
                        _stack.Push(next.ConstrainedParameters);
                    }

                    if (next.Literals.Count > 0)
                    {
                        Debug.Assert(next.Depth < _tokenizer.Count);
                        if (next.Literals.TryGetValue(_tokenizer[next.Depth].Value, out var node))
                        {
                            _stack.Push(node);
                        }
                    }
                }

                return false;
            }

            public void Reset()
            {
                _stack.Clear();
                Current = null;
            }
        }

        protected override void InitializeServices(IServiceProvider services)
        {
            _constraintFactory = services.GetRequiredService<IConstraintFactory>();
        }
    }
}
