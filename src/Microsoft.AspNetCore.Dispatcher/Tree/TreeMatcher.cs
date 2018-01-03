// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Dispatcher.Internal;
using Microsoft.AspNetCore.Dispatcher.Patterns;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using Primitives = Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Dispatcher
{
    public class TreeMatcher : MatcherBase
    {
        private bool _onChange;
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

        public override async Task MatchAsync(MatcherContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            EnsureTreeMatcherServicesInitialized(context);

            var cache = LazyInitializer.EnsureInitialized(ref _cache, ref _dataInitialized, ref _lock, _initializer);

            var values = new DispatcherValueCollection();
            context.Values = values;

            for (var i = 0; i < cache.Trees.Length; i++)
            {
                var tree = cache.Trees[i];
                var tokenizer = new PathTokenizer(context.HttpContext.Request.Path);

                var treenumerator = new TreeEnumerator(tree.Root, tokenizer);

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

        private void EnsureTreeMatcherServicesInitialized(MatcherContext context)
        {
            EnsureServicesInitialized(context);
            if (Volatile.Read(ref _onChange))
            {
                return;
            }

            lock (_lock)
            {
                if (!Volatile.Read(ref _onChange))
                {
                    _onChange = true;
                    Primitives.ChangeToken.OnChange(() => ChangeToken, () => Volatile.Write(ref _dataInitialized, false));
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

                if (!(endpoint is IRoutePatternEndpoint templateEndpoint))
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

                UrlMatchingTree.AddEntryToTree(tree, entry);
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

        protected override void InitializeServices(IServiceProvider services)
        {
            _constraintFactory = services.GetRequiredService<IConstraintFactory>();
        }
    }
}
