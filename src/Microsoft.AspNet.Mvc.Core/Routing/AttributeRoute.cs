// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Routing.Template;

namespace Microsoft.AspNet.Mvc.Routing
{
    /// <summary>
    /// An <see cref="IRouter"/> implementation for attribute routing.
    /// </summary>
    public class AttributeRoute : IRouter
    {
        private readonly IRouter _next;
        private readonly TemplateRoute[] _matchingRoutes;
        private readonly AttributeRouteLinkGenerationEntry[] _linkGenerationEntries;

        /// <summary>
        /// Creates a new <see cref="AttributeRoute"/>.
        /// </summary>
        /// <param name="next">The next router. Invoked when a route entry matches.</param>
        /// <param name="entries">The set of route entries.</param>
        public AttributeRoute(
            [NotNull] IRouter next, 
            [NotNull] IEnumerable<AttributeRouteMatchingEntry> matchingEntries,
            [NotNull] IEnumerable<AttributeRouteLinkGenerationEntry> linkGenerationEntries)
        {
            _next = next;

            // FOR RIGHT NOW - this is just an array of regular template routes. We'll follow up by implementing
            // a good data-structure here. See #740
            _matchingRoutes = matchingEntries.OrderBy(e => e.Precedence).Select(e => e.Route).ToArray();

            // FOR RIGHT NOW - this is just an array of entries. We'll follow up by implementing
            // a good data-structure here. See #741
            _linkGenerationEntries = linkGenerationEntries.OrderBy(e => e.Precedence).ToArray();
        }

        /// <inheritdoc />
        public async Task RouteAsync([NotNull] RouteContext context)
        {
            foreach (var route in _matchingRoutes)
            {
                await route.RouteAsync(context);
                if (context.IsHandled)
                {
                    return;
                }
            }
        }

        /// <inheritdoc />
        public string GetVirtualPath([NotNull] VirtualPathContext context)
        {
            // To generate a link, we iterate the collection of entries (in order of precedence) and execute
            // each one that matches the 'required link values' - which will typically be a value for action
            // and controller.
            //
            // Building a proper data structure to optimize this is tracked by #741
            foreach (var entry in _linkGenerationEntries)
            {
                var isMatch = true;
                foreach (var requiredLinkValue in entry.RequiredLinkValues)
                {
                    if (!ContextHasSameValue(context, requiredLinkValue.Key, requiredLinkValue.Value))
                    {
                        isMatch = false;
                        break;
                    }
                }
                
                if (isMatch)
                {
                    var path = GenerateLink(context, entry);
                    if (path != null)
                    {
                        context.IsBound = true;
                        return path;
                    }
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
                RouteDirection.UrlGeneration);
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
