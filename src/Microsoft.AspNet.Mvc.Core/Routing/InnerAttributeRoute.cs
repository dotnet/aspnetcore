// Copyright (c) .NET Foundation. All rights reserved.
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
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Mvc.Routing
{
    /// <summary>
    /// An <see cref="IRouter"/> implementation for attribute routing.
    /// </summary>
    public class InnerAttributeRoute : IRouter
    {
        private readonly IRouter _next;
        private readonly LinkGenerationDecisionTree _linkGenerationTree;
        private readonly AttributeRouteMatchingEntry[] _matchingEntries;
        private readonly IDictionary<string, AttributeRouteLinkGenerationEntry> _namedEntries;

        private ILogger _logger;
        private ILogger _constraintLogger;

        /// <summary>
        /// Creates a new <see cref="InnerAttributeRoute"/>.
        /// </summary>
        /// <param name="next">The next router. Invoked when a route entry matches.</param>
        /// <param name="entries">The set of route entries.</param>
        public InnerAttributeRoute(
            IRouter next,
            IEnumerable<AttributeRouteMatchingEntry> matchingEntries,
            IEnumerable<AttributeRouteLinkGenerationEntry> linkGenerationEntries,
            ILogger logger,
            ILogger constraintLogger,
            int version)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            if (matchingEntries == null)
            {
                throw new ArgumentNullException(nameof(matchingEntries));
            }

            if (linkGenerationEntries == null)
            {
                throw new ArgumentNullException(nameof(linkGenerationEntries));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (constraintLogger == null)
            {
                throw new ArgumentNullException(nameof(constraintLogger));
            }

            _next = next;
            _logger = logger;
            _constraintLogger = constraintLogger;

            Version = version;

            // Order all the entries by order, then precedence, and then finally by template in order to provide
            // a stable routing and link generation order for templates with same order and precedence.
            // We use ordinal comparison for the templates because we only care about them being exactly equal and
            // we don't want to make any equivalence between templates based on the culture of the machine.

            _matchingEntries = matchingEntries
                .OrderBy(o => o.Order)
                .ThenBy(e => e.Precedence)
                .ThenBy(e => e.RouteTemplate, StringComparer.Ordinal)
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
                        nameof(linkGenerationEntries));
                }
                else if (namedEntry == null)
                {
                    namedEntries.Add(entry.Name, entry);
                }
            }

            _namedEntries = namedEntries;

            // The decision tree will take care of ordering for these entries.
            _linkGenerationTree = new LinkGenerationDecisionTree(linkGenerationEntries.ToArray());
        }

        /// <summary>
        /// Gets the version of this route. This corresponds to the value of
        /// <see cref="Infrastructure.ActionDescriptorsCollection.Version"/> when this route was created.
        /// </summary>
        public int Version { get; }

        /// <inheritdoc />
        public async Task RouteAsync(RouteContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            foreach (var matchingEntry in _matchingEntries)
            {
                var requestPath = context.HttpContext.Request.Path;
                var values = matchingEntry.TemplateMatcher.Match(requestPath);
                if (values == null)
                {
                    // If we got back a null value set, that means the URI did not match
                    continue;
                }

                var oldRouteData = context.RouteData;

                var newRouteData = new RouteData(oldRouteData);
                newRouteData.Routers.Add(matchingEntry.Target);
                MergeValues(newRouteData.Values, values);

                if (!RouteConstraintMatcher.Match(
                    matchingEntry.Constraints,
                    newRouteData.Values,
                    context.HttpContext,
                    this,
                    RouteDirection.IncomingRequest,
                    _constraintLogger))
                {
                    continue;
                }

                _logger.MatchedRouteName(
                    matchingEntry.RouteName,
                    matchingEntry.RouteTemplate);

                try
                {
                    context.RouteData = newRouteData;

                    await matchingEntry.Target.RouteAsync(context);
                }
                finally
                {
                    // Restore the original values to prevent polluting the route data.
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

        /// <inheritdoc />
        public VirtualPathData GetVirtualPath(VirtualPathContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // If it's a named route we will try to generate a link directly and
            // if we can't, we will not try to generate it using an unnamed route.
            if (context.RouteName != null)
            {
                return GetVirtualPathForNamedRoute(context);
            }

            // The decision tree will give us back all entries that match the provided route data in the correct
            // order. We just need to iterate them and use the first one that can generate a link.
            var matches = _linkGenerationTree.GetMatches(context);

            foreach (var match in matches)
            {
                var path = GenerateVirtualPath(context, match.Entry);
                if (path != null)
                {
                    context.IsBound = true;
                    return path;
                }
            }

            return null;
        }

        private VirtualPathData GetVirtualPathForNamedRoute(VirtualPathContext context)
        {
            AttributeRouteLinkGenerationEntry entry;
            if (_namedEntries.TryGetValue(context.RouteName, out entry))
            {
                var path = GenerateVirtualPath(context, entry);
                if (path != null)
                {
                    context.IsBound = true;
                    return path;
                }
            }
            return null;
        }

        private VirtualPathData GenerateVirtualPath(VirtualPathContext context, AttributeRouteLinkGenerationEntry entry)
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

            var pathData = _next.GetVirtualPath(childContext);
            if (pathData != null)
            {
                // If path is non-null then the target router short-circuited, we don't expect this
                // in typical MVC scenarios.
                return pathData;
            }
            else if (!childContext.IsBound)
            {
                // The target router has rejected these values. We don't expect this in typical MVC scenarios.
                return null;
            }

            var path = entry.Binder.BindValues(bindingResult.AcceptedValues);
            if (path == null)
            {
                return null;
            }

            return new VirtualPathData(this, path);
        }

        private static void MergeValues(
                IDictionary<string, object> destination,
                IDictionary<string, object> values)
        {
            foreach (var kvp in values)
            {
                if (kvp.Value != null)
                {
                    // This will replace the original value for the specified key.
                    // Values from the matched route will take preference over previous
                    // data in the route context.
                    destination[kvp.Key] = kvp.Value;
                }
            }
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
