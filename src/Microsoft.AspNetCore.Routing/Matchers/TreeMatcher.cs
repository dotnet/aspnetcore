// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.EndpointConstraints;
using Microsoft.AspNetCore.Routing.Internal;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.AspNetCore.Routing.Tree;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    internal class TreeMatcher : Matcher
    {
        private readonly IInlineConstraintResolver _constraintFactory;
        private readonly ILogger _logger;
        private readonly EndpointSelector _endpointSelector;
        private readonly DataSourceDependantCache<UrlMatchingTree[]> _cache;

        public TreeMatcher(
            IInlineConstraintResolver constraintFactory,
            ILogger logger,
            EndpointDataSource dataSource,
            EndpointSelector endpointSelector)
        {
            if (constraintFactory == null)
            {
                throw new ArgumentNullException(nameof(constraintFactory));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (dataSource == null)
            {
                throw new ArgumentNullException(nameof(dataSource));
            }

            _constraintFactory = constraintFactory;
            _logger = logger;
            _endpointSelector = endpointSelector;
            _cache = new DataSourceDependantCache<UrlMatchingTree[]>(dataSource, CreateTrees);
            _cache.EnsureInitialized();
        }

        public override Task MatchAsync(HttpContext httpContext, IEndpointFeature feature)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            if (feature == null)
            {
                throw new ArgumentNullException(nameof(feature));
            }

            var values = new RouteValueDictionary();
            feature.Values = values;

            var cache = _cache.Value;
            for (var i = 0; i < cache.Length; i++)
            {
                var tree = cache[i];
                var tokenizer = new PathTokenizer(httpContext.Request.Path);

                var treenumerator = new TreeEnumerator(tree.Root, tokenizer);

                while (treenumerator.MoveNext())
                {
                    var node = treenumerator.Current;
                    foreach (var item in node.Matches)
                    {
                        var entry = item.Entry;
                        var matcher = item.TemplateMatcher;

                        values.Clear();
                        if (!matcher.TryMatch(httpContext.Request.Path, values))
                        {
                            continue;
                        }

                        Log.MatchedTemplate(_logger, httpContext, entry.RouteTemplate);

                        if (!MatchConstraints(httpContext, values, entry.Constraints))
                        {
                            continue;
                        }

                        SelectEndpoint(httpContext, feature, (MatcherEndpoint[])entry.Tag);

                        if (feature.Endpoint != null)
                        {
                            if (feature.Endpoint is MatcherEndpoint endpoint)
                            {
                                foreach (var kvp in endpoint.Defaults)
                                {
                                    if (!feature.Values.ContainsKey(kvp.Key))
                                    {
                                        feature.Values[kvp.Key] = kvp.Value;
                                    }
                                }
                            }

                            // Found a matching endpoint
                            return Task.CompletedTask;
                        }
                    }
                }
            }

            // No match found
            return Task.CompletedTask;
        }

        private bool MatchConstraints(
            HttpContext httpContext,
            RouteValueDictionary values,
            IDictionary<string, IRouteConstraint> constraints)
        {
            if (constraints != null)
            {
                foreach (var kvp in constraints)
                {
                    var constraint = kvp.Value;
                    if (!constraint.Match(httpContext, new DummyRouter(), kvp.Key, values, RouteDirection.IncomingRequest))
                    {
                        values.TryGetValue(kvp.Key, out var value);

                        Log.ConstraintFailed(_logger, value, kvp.Key, kvp.Value);
                        return false;
                    }
                }
            }

            return true;
        }

        private class DummyRouter : IRouter
        {
            public VirtualPathData GetVirtualPath(VirtualPathContext context)
            {
                throw new NotImplementedException();
            }

            public Task RouteAsync(RouteContext context)
            {
                return Task.CompletedTask;
            }
        }

        private void SelectEndpoint(HttpContext httpContext, IEndpointFeature feature, IReadOnlyList<MatcherEndpoint> endpoints)
        {
            var endpoint = (MatcherEndpoint)_endpointSelector.SelectBestCandidate(httpContext, endpoints);

            if (endpoint == null)
            {
                Log.MatchFailed(_logger, httpContext);
            }
            else
            {
                Log.MatchSuccess(_logger, httpContext, endpoint);

                feature.Endpoint = endpoint;
                feature.Invoker = endpoint.Invoker;
            }
        }

        private UrlMatchingTree[] CreateTrees(IReadOnlyList<Endpoint> endpoints)
        {
            var groups = new Dictionary<Key, List<MatcherEndpoint>>();

            for (var i = 0; i < endpoints.Count; i++)
            {
                var endpoint = endpoints[i] as MatcherEndpoint;
                if (endpoint == null)
                {
                    continue;
                }

                var order = endpoint.Order;
                if (!groups.TryGetValue(new Key(order, endpoint.Template), out var group))
                {
                    group = new List<MatcherEndpoint>();
                    groups.Add(new Key(order, endpoint.Template), group);
                }

                group.Add(endpoint);
            }

            var entries = new List<InboundRouteEntry>();
            foreach (var group in groups)
            {
                var template = TemplateParser.Parse(group.Key.Template);
                var entryExists = entries.Any(item => item.RouteTemplate.TemplateText == template.TemplateText && item.Order == group.Key.Order);
                if (!entryExists)
                {
                    entries.Add(MapInbound(template, group.Value.ToArray(), group.Key.Order));
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
                tree.AddEntry(entry);
            }

            return trees.ToArray();
        }

        private InboundRouteEntry MapInbound(RouteTemplate template, Endpoint[] endpoints, int order)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            var entry = new InboundRouteEntry()
            {
                Precedence = RoutePrecedence.ComputeInbound(template),
                RouteTemplate = template,
                Order = order,
                Tag = endpoints,
            };

            var constraintBuilder = new RouteConstraintBuilder(_constraintFactory, template.TemplateText);
            foreach (var parameter in template.Parameters)
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
            return entry;
        }

        private readonly struct Key : IEquatable<Key>
        {
            public readonly int Order;
            public readonly string Template;

            public Key(int order, string routePattern)
            {
                Order = order;
                Template = routePattern;
            }

            public bool Equals(Key other)
            {
                return Order == other.Order && string.Equals(Template, other.Template, StringComparison.OrdinalIgnoreCase);
            }

            public override bool Equals(object obj)
            {
                return obj is Key ? Equals((Key)obj) : false;
            }

            public override int GetHashCode()
            {
                var hash = new HashCodeCombiner();
                hash.Add(Order);
                hash.Add(Template, StringComparer.OrdinalIgnoreCase);
                return hash;
            }
        }

        private static class Log
        {
            private static readonly Action<ILogger, string, PathString, Exception> _matchSuccess = LoggerMessage.Define<string, PathString>(
                LogLevel.Debug,
                new EventId(1, "MatchSuccess"),
                "Request matched endpoint '{EndpointName}' for request path '{Path}'.");

            private static readonly Action<ILogger, PathString, Exception> _matchFailed = LoggerMessage.Define<PathString>(
                LogLevel.Debug,
                new EventId(2, "MatchFailed"),
                "No endpoints matched request path '{Path}'.");

            private static readonly Action<ILogger, PathString, IEnumerable<string>, Exception> _matchAmbiguous = LoggerMessage.Define<PathString, IEnumerable<string>>(
                LogLevel.Error,
                new EventId(3, "MatchAmbiguous"),
                "Request matched multiple endpoints for request path '{Path}'. Matching endpoints: {AmbiguousEndpoints}");

            private static readonly Action<ILogger, object, string, IRouteConstraint, Exception> _constraintFailed = LoggerMessage.Define<object, string, IRouteConstraint>(
                LogLevel.Debug,
                new EventId(4, "ContraintFailed"),
                "Route value '{RouteValue}' with key '{RouteKey}' did not match the constraint '{RouteConstraint}'.");

            private static readonly Action<ILogger, string, PathString, Exception> _matchedTemplate = LoggerMessage.Define<string, PathString>(
                LogLevel.Debug,
                new EventId(5, "MatchedTemplate"),
                "Request matched the route pattern '{RouteTemplate}' for request path '{Path}'.");

            public static void MatchSuccess(ILogger logger, HttpContext httpContext, Endpoint endpoint)
            {
                _matchSuccess(logger, endpoint.DisplayName, httpContext.Request.Path, null);
            }

            public static void MatchFailed(ILogger logger, HttpContext httpContext)
            {
                _matchFailed(logger, httpContext.Request.Path, null);
            }

            public static void MatchAmbiguous(ILogger logger, HttpContext httpContext, IEnumerable<Endpoint> endpoints)
            {
                _matchAmbiguous(logger, httpContext.Request.Path, endpoints.Select(e => e.DisplayName), null);
            }

            public static void ConstraintFailed(ILogger logger, object routeValue, string routeKey, IRouteConstraint routeConstraint)
            {
                _constraintFailed(logger, routeValue, routeKey, routeConstraint, null);
            }

            public static void MatchedTemplate(ILogger logger, HttpContext httpContext, RouteTemplate template)
            {
                _matchedTemplate(logger, template.TemplateText, httpContext.Request.Path, null);
            }
        }
    }
}
