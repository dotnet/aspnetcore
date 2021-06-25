// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Components.Reflection;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Internal;

namespace Microsoft.AspNetCore.Components.Routing
{
    internal sealed class QueryParameterValueSupplier
    {
        private static Dictionary<Type, QueryParameterValueSupplier?> _cacheByType = new();
        private readonly QueryParameterMapping[] _mappings;
        private readonly SortedList<string, object?> _assignmentsTemplate;

        public static QueryParameterValueSupplier? ForType(Type componentType)
        {
            if (!_cacheByType.TryGetValue(componentType, out var instanceOrNull))
            {
                // If the component doesn't have any query parameters, store a null value for it
                // so we know the upstream code can't try to render query parameter frames for it.
                var mappings = FindQueryParameterMappings(componentType);
                instanceOrNull = mappings.Length == 0 ? null : new QueryParameterValueSupplier(mappings);
                _cacheByType.TryAdd(componentType, instanceOrNull);
            }

            return instanceOrNull;
        }

        private QueryParameterValueSupplier(QueryParameterMapping[] mappings)
        {
            _mappings = mappings;

            // We must always supply a value for all parameters that can be populated from the querystring
            // (so that, if no value is supplied, the parameter is reset back to a default). So, precompute
            // a SortedList with nulls for all values. We can then shallow-clone this on each navigation.
            // The comparer can be case-sensitive because the keys only come from our own code based on
            // PropertyInfo.Name.
            _assignmentsTemplate = new (StringComparer.Ordinal);
            foreach (var mapping in _mappings)
            {
                foreach (var destination in mapping.Destinations)
                {
                    _assignmentsTemplate.Add(destination.ComponentParameterName, null);
                }
            }
        }

        public void RenderParameterAttributes(RenderTreeBuilder builder, ReadOnlySpan<char> queryString)
        {
            var assignmentByComponentParameterName = new SortedList<string, object?>(
                _assignmentsTemplate, StringComparer.Ordinal);

            // Populate the assignments dictionary in a single pass through the querystring
            var queryStringEnumerable = new QueryStringEnumerable(queryString);
            foreach (var suppliedPair in queryStringEnumerable)
            {
                // The reason we do an O(N) linear search rather than something like a dictionary lookup is
                // that _mappings will usually contain < 5 entries, so a series of string comparisons will
                // likely be much faster than hashing a potentially long user-supplied ReadOnlySpan<char>.
                // If this becomes limiting, consider other options like a SortedList<string> so we can
                // seek into it by binary search.
                foreach (var candidateMapping in _mappings)
                {
                    // Note that we're comparing the names without unescaping.
                    // Cost: we don't support component parameter names that need escaping (who would do this?)
                    // Benefit: no need to allocate and decode for every unrelated user-supplied query param name
                    // Alternatively, we could store pre-escaped names in _queryParameterToComponentParametersMap.
                    // Then, if the param was either a simple name or used an escaping that matches up with ours,
                    // then it would still work.
                    if (suppliedPair.NameEscaped.Equals(candidateMapping.QueryParameterName, StringComparison.OrdinalIgnoreCase))
                    {
                        var unescapedValue = Uri.UnescapeDataString(suppliedPair.ValueEscaped.ToString().Replace('+', ' '));

                        foreach (var destination in candidateMapping.Destinations)
                        {
                            // TODO: If we want to support multiple same-named params populating an array,
                            // extend this logic to build a list here then later convert to an array
                            if (destination.Constraint.Match(unescapedValue, out var parsedVaue))
                            {
                                assignmentByComponentParameterName[destination.ComponentParameterName] = parsedVaue;
                            }
                        }

                        break;
                    }
                }
            }

            // Finally actually emit the rendertree frames
            foreach (var (name, value) in assignmentByComponentParameterName)
            {
                builder.AddAttribute(0, name, value);
            }
        }

        private static QueryParameterMapping[] FindQueryParameterMappings(Type componentType)
        {
            var candidateProperties = MemberAssignment.GetPropertiesIncludingInherited(componentType, ComponentProperties.BindablePropertyFlags);
            Dictionary<string, List<QueryParameterDestination>>? mappingsByQueryParameterName = null;

            foreach (var propertyInfo in candidateProperties)
            {
                if (!propertyInfo.IsDefined(typeof(ParameterAttribute)))
                {
                    continue;
                }

                var fromQueryAttribute = propertyInfo.GetCustomAttribute<SupplyParameterFromQueryAttribute>();
                if (fromQueryAttribute is not null)
                {
                    // Found a parameter that's assignable from querystring
                    var componentParameterName = propertyInfo.Name;
                    var queryParameterName = string.IsNullOrEmpty(fromQueryAttribute.Name)
                        ? componentParameterName
                        : fromQueryAttribute.Name;

                    mappingsByQueryParameterName ??= new(StringComparer.OrdinalIgnoreCase);
                    if (!mappingsByQueryParameterName.ContainsKey(queryParameterName))
                    {
                        mappingsByQueryParameterName.Add(queryParameterName, new());
                    }

                    if (!SupportedQueryValueTargetTypeToConstraintName.TryGetValue(propertyInfo.PropertyType, out var constraintName)
                        || !RouteConstraint.TryGetOrCreateRouteConstraint(constraintName, out var constraint))
                    {
                        throw new NotSupportedException($"Query parameters cannot be parsed as type '{propertyInfo.PropertyType}'.");
                    }

                    mappingsByQueryParameterName[queryParameterName].Add(
                        new QueryParameterDestination(componentParameterName, constraint));
                }
            }

            if (mappingsByQueryParameterName == null)
            {
                return Array.Empty<QueryParameterMapping>();
            }
            else
            {
                var result = new QueryParameterMapping[mappingsByQueryParameterName.Count];
                var index = 0;
                foreach (var (name, destinations) in mappingsByQueryParameterName)
                {
                    result[index++] = new QueryParameterMapping(name, destinations.ToArray());
                }

                return result;
            }
        }

        private record QueryParameterMapping(string QueryParameterName, QueryParameterDestination[] Destinations);
        private record QueryParameterDestination(string ComponentParameterName, RouteConstraint Constraint);

        private readonly static Dictionary<Type, string> SupportedQueryValueTargetTypeToConstraintName = new()
        {
            { typeof(bool), "bool" },
            { typeof(DateTime), "datetime" },
            { typeof(decimal), "decimal" },
            { typeof(double), "double" },
            { typeof(float), "float" },
            { typeof(Guid), "guid" },
            { typeof(int), "int" },
            { typeof(long), "long" },
        };
    }
}
