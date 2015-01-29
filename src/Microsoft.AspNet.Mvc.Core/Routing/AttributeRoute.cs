// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.Internal.Routing;
using Microsoft.AspNet.Mvc.Logging;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Routing.Template;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Mvc.Routing
{
    /// <summary>
    /// An <see cref="IRouter"/> implementation for attribute routing.
    /// </summary>
    public class AttributeRoute : IRouter
    {
        private readonly IRouter _next;
        private readonly LinkGenerationDecisionTree _linkGenerationTree;
        private readonly TemplateRoute[] _matchingRoutes;
        private readonly IDictionary<string, AttributeRouteLinkGenerationEntry> _namedEntries;

        private ILogger _logger;
        private ILogger _constraintLogger;

        /// <summary>
        /// Creates a new <see cref="AttributeRoute"/>.
        /// </summary>
        /// <param name="next">The next router. Invoked when a route entry matches.</param>
        /// <param name="entries">The set of route entries.</param>
        public AttributeRoute(
            [NotNull] IRouter next,
            [NotNull] IEnumerable<AttributeRouteMatchingEntry> matchingEntries,
            [NotNull] IEnumerable<AttributeRouteLinkGenerationEntry> linkGenerationEntries,
            [NotNull] ILoggerFactory factory)
        {
            _next = next;

            // Order all the entries by order, then precedence, and then finally by template in order to provide
            // a stable routing and link generation order for templates with same order and precedence.
            // We use ordinal comparison for the templates because we only care about them being exactly equal and
            // we don't want to make any equivalence between templates based on the culture of the machine.

            _matchingRoutes = matchingEntries
                .OrderBy(o => o.Order)
                .ThenBy(e => e.Precedence)
                .ThenBy(e => e.Route.RouteTemplate, StringComparer.Ordinal)
                .Select(e => e.Route)
                .ToArray();

            var namedEntries = new Dictionary<string, AttributeRouteLinkGenerationEntry>(
                StringComparer.OrdinalIgnoreCase);

            foreach (var entry in linkGenerationEntries)
            {
                // Skip unnamed entries
                if (entry.Name == null)
                {
                    continue;
                }

                // We only need to keep one AttributeRouteLinkGenerationEntry per route template
                // so in case two entries have the same name and the same template we only keep
                // the first entry.
                AttributeRouteLinkGenerationEntry namedEntry = null;
                if (namedEntries.TryGetValue(entry.Name, out namedEntry) &&
                    !namedEntry.TemplateText.Equals(entry.TemplateText, StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException(
                        Resources.FormatAttributeRoute_DifferentLinkGenerationEntries_SameName(entry.Name),
                        "linkGenerationEntries");
                }
                else if (namedEntry == null)
                {
                    namedEntries.Add(entry.Name, entry);
                }
            }

            _namedEntries = namedEntries;

            // The decision tree will take care of ordering for these entries.
            _linkGenerationTree = new LinkGenerationDecisionTree(linkGenerationEntries.ToArray());

            _logger = factory.Create<AttributeRoute>();
            _constraintLogger = factory.Create(typeof(RouteConstraintMatcher).FullName);
        }

        /// <inheritdoc />
        public async Task RouteAsync([NotNull] RouteContext context)
        {
            using (_logger.BeginScope("AttributeRoute.RouteAsync"))
            {
                foreach (var route in _matchingRoutes)
                {
                    var oldRouteData = context.RouteData;

                    var newRouteData = new RouteData(oldRouteData);
                    newRouteData.Routers.Add(route);

                    try
                    {
                        context.RouteData = newRouteData;
                        await route.RouteAsync(context);
                    }
                    finally
                    {
                        if (!context.IsHandled)
                        {
                            context.RouteData = oldRouteData;
                        }
                    }

                    if (context.IsHandled)
                    {
                        break;
                    }
                }
            }

            if (_logger.IsEnabled(LogLevel.Verbose))
            {
                _logger.WriteValues(new AttributeRouteRouteAsyncValues()
                {
                    MatchingRoutes = _matchingRoutes,
                    Handled = context.IsHandled
                });
            }
        }

        /// <inheritdoc />
        public string GetVirtualPath([NotNull] VirtualPathContext context)
        {
            // If it's a named route we will try to generate a link directly and
            // if we can't, we will not try to generate it using an unnamed route.
            if (context.RouteName != null)
            {
                return GetVirtualPathForNamedRoute(context);
            }

            // The decision tree will give us back all entries that match the provided route data in the correct
            // order. We just need to iterate them and use the first one that can generate a link.
            var matches = _linkGenerationTree.GetMatches(context);

            foreach (var entry in matches)
            {
                var path = GenerateLink(context, entry);
                if (path != null)
                {
                    context.IsBound = true;
                    return path;
                }
            }

            return null;
        }

        private string GetVirtualPathForNamedRoute(VirtualPathContext context)
        {
            AttributeRouteLinkGenerationEntry entry;
            if (_namedEntries.TryGetValue(context.RouteName, out entry))
            {
                var path = GenerateLink(context, entry);
                if (path != null)
                {
                    context.IsBound = true;
                    return path;
                }
            }
            return null;
        }

        private string GenerateLink(VirtualPathContext context, AttributeRouteLinkGenerationEntry entry)
        {
            // In attribute the context includes the values that are used to select this entry - typically
            // these will be the standard 'action', 'controller' and maybe 'area' tokens. However, we don't
            // want to pass these to the link generation code, or else they will end up as query parameters.
            //
            // So, we need to exclude from here any values that are 'required link values', but aren't
            // parameters in the template.
            //
            // Ex:
            //      template: api/Products/{action}
            //      required values: { id = "5", action = "Buy", Controller = "CoolProducts" }
            //
            //      result: { id = "5", action = "Buy" }
            var inputValues = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in context.Values)
            {
                if (entry.RequiredLinkValues.ContainsKey(kvp.Key))
                {
                    var parameter = entry.Template.Parameters
                        .FirstOrDefault(p => string.Equals(p.Name, kvp.Key, StringComparison.OrdinalIgnoreCase));

                    if (parameter == null)
                    {
                        continue;
                    }
                }

                inputValues.Add(kvp.Key, kvp.Value);
            }

            var bindingResult = entry.Binder.GetValues(context.AmbientValues, inputValues);
            if (bindingResult == null)
            {
                // A required parameter in the template didn't get a value.
                return null;
            }

            var matched = RouteConstraintMatcher.Match(
                entry.Constraints,
                bindingResult.CombinedValues,
                context.Context,
                this,
                RouteDirection.UrlGeneration,
                _constraintLogger);

            if (!matched)
            {
                // A constraint rejected this link.
                return null;
            }

            // These values are used to signal to the next route what we would produce if we round-tripped
            // (generate a link and then parse). In MVC the 'next route' is typically the MvcRouteHandler.
            var providedValues = new Dictionary<string, object>(
                bindingResult.AcceptedValues,
                StringComparer.OrdinalIgnoreCase);
            providedValues.Add(AttributeRouting.RouteGroupKey, entry.RouteGroup);

            var childContext = new VirtualPathContext(context.Context, context.AmbientValues, context.Values)
            {
                ProvidedValues = providedValues,
            };

            var path = _next.GetVirtualPath(childContext);
            if (path != null)
            {
                // If path is non-null then the target router short-circuited, we don't expect this
                // in typical MVC scenarios.
                return path;
            }
            else if (!childContext.IsBound)
            {
                // The target router has rejected these values. We don't expect this in typical MVC scenarios.
                return null;
            }

            path = entry.Binder.BindValues(bindingResult.AcceptedValues);
            return path;
        }

        private bool ContextHasSameValue(VirtualPathContext context, string key, object value)
        {
            object providedValue;
            if (!context.Values.TryGetValue(key, out providedValue))
            {
                // If the required value is an 'empty' route value, then ignore ambient values.
                // This handles a case where we're generating a link to an action like:
                // { area = "", controller = "Home", action = "Index" }
                //
                // and the ambient values has a value for area.
                if (value != null)
                {
                    context.AmbientValues.TryGetValue(key, out providedValue);
                }
            }

            return TemplateBinder.RoutePartsEqual(providedValue, value);
        }
    }
}
