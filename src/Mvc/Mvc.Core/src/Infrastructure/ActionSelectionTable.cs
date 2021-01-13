// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    // Common infrastructure for things that look up actions by route values.
    //
    // The ActionSelectionTable stores a mapping of route-values -> items for each known set of
    // of route-values. We actually build two of these mappings, one for case-sensitive (fast path) and one for
    // case-insensitive (slow path).
    //
    // This is necessary because MVC routing/action-selection is always case-insensitive. So we're going to build
    // a case-sensitive dictionary that will behave like the a case-insensitive dictionary when you hit one of the
    // canonical entries. When you don't hit a case-sensitive match it will try the case-insensitive dictionary
    // so you still get correct behaviors.
    //
    // The difference here is because while MVC is case-insensitive, doing a case-sensitive comparison is much
    // faster. We also expect that most of the URLs we process are canonically-cased because they were generated
    // by Url.Action or another routing api.
    //
    // This means that for a set of actions like:
    //      { controller = "Home", action = "Index" } -> HomeController::Index1()
    //      { controller = "Home", action = "index" } -> HomeController::Index2()
    //
    // Both of these actions match "Index" case-insensitively, but there exist two known canonical casings,
    // so we will create an entry for "Index" and an entry for "index". Both of these entries match **both**
    // actions.
    internal class ActionSelectionTable<TItem>
    {
        private ActionSelectionTable(
            int version, 
            string[] routeKeys,
            Dictionary<string[], List<TItem>> ordinalEntries,
            Dictionary<string[], List<TItem>> ordinalIgnoreCaseEntries)
        {
            Version = version;
            RouteKeys = routeKeys;
            OrdinalEntries = ordinalEntries;
            OrdinalIgnoreCaseEntries = ordinalIgnoreCaseEntries;
        }
        
        public int Version { get; }

        private string[] RouteKeys { get; }

        private Dictionary<string[], List<TItem>> OrdinalEntries { get; }

        private Dictionary<string[], List<TItem>> OrdinalIgnoreCaseEntries { get; }

        public static ActionSelectionTable<ActionDescriptor> Create(ActionDescriptorCollection actions)
        {
            return CreateCore<ActionDescriptor>(

                // We need to store the version so the cache can be invalidated if the actions change.
                version: actions.Version,

                // For action selection, ignore attribute routed actions
                items: actions.Items.Where(a => a.AttributeRouteInfo == null),

                getRouteKeys: a => a.RouteValues?.Keys,
                getRouteValue: (a, key) =>
                {
                    string value = null;
                    a.RouteValues?.TryGetValue(key, out value);
                    return value ?? string.Empty;
                });
        }

        public static ActionSelectionTable<Endpoint> Create(IEnumerable<Endpoint> endpoints)
        {
            return CreateCore<Endpoint>(
                
                // we don't use version for endpoints
                version: 0, 

                // Exclude RouteEndpoints - we only process inert endpoints here. 
                items: endpoints.Where(e =>
                {
                    return e.GetType() == typeof(Endpoint);
                }),

                getRouteKeys: e => e.Metadata.GetMetadata<ActionDescriptor>()?.RouteValues?.Keys,
                getRouteValue: (e, key) =>
                {
                    string value = null;
                    e.Metadata.GetMetadata<ActionDescriptor>()?.RouteValues?.TryGetValue(key, out value);
                    return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
                });
        }

        private static ActionSelectionTable<T> CreateCore<T>(
            int version,
            IEnumerable<T> items,
            Func<T, IEnumerable<string>> getRouteKeys,
            Func<T, string, string> getRouteValue)
        {       
            // We need to build two maps for all of the route values.
            var ordinalEntries = new Dictionary<string[], List<T>>(StringArrayComparer.Ordinal);
            var ordinalIgnoreCaseEntries = new Dictionary<string[], List<T>>(StringArrayComparer.OrdinalIgnoreCase);

            // We need to hold on to an ordered set of keys for the route values. We'll use these later to
            // extract the set of route values from an incoming request to compare against our maps of known
            // route values.
            var routeKeys = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in items)
            {
                var keys = getRouteKeys(item);
                if (keys != null)
                {
                    foreach (var key in keys)
                    {
                        routeKeys.Add(key);
                    }
                }
            }

            foreach (var item in items)
            {
                // This is a conventionally routed action - so we need to extract the route values associated
                // with this action (in order) so we can store them in our dictionaries.
                var index = 0;
                var routeValues = new string[routeKeys.Count];
                foreach (var key in routeKeys)
                {
                    var value = getRouteValue(item, key);
                    routeValues[index++] = value;
                }

                if (!ordinalIgnoreCaseEntries.TryGetValue(routeValues, out var entries))
                {
                    entries = new List<T>();
                    ordinalIgnoreCaseEntries.Add(routeValues, entries);
                }

                entries.Add(item);

                // We also want to add the same (as in reference equality) list of actions to the ordinal entries.
                // We'll keep updating `entries` to include all of the actions in the same equivalence class -
                // meaning, all conventionally routed actions for which the route values are equal ignoring case.
                //
                // `entries` will appear in `OrdinalIgnoreCaseEntries` exactly once and in `OrdinalEntries` once
                // for each variation of casing that we've seen.
                if (!ordinalEntries.ContainsKey(routeValues))
                {
                    ordinalEntries.Add(routeValues, entries);
                }
            }

            return new ActionSelectionTable<T>(version, routeKeys.ToArray(), ordinalEntries, ordinalIgnoreCaseEntries);
        }

        public IReadOnlyList<TItem> Select(RouteValueDictionary values)
        {
            // Select works based on a string[] of the route values in a pre-calculated order. This code extracts
            // those values in the correct order.
            var routeKeys = RouteKeys;
            var routeValues = new string[routeKeys.Length];
            for (var i = 0; i < routeKeys.Length; i++)
            {
                values.TryGetValue(routeKeys[i], out var value);
                routeValues[i] = value as string ?? Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
            }

            // Now look up, first case-sensitive, then case-insensitive.
            if (OrdinalEntries.TryGetValue(routeValues, out var matches) ||
                OrdinalIgnoreCaseEntries.TryGetValue(routeValues, out matches))
            {
                Debug.Assert(matches != null);
                Debug.Assert(matches.Count >= 0);
                return matches;
            }

            return Array.Empty<TItem>();
        }
    }
}
