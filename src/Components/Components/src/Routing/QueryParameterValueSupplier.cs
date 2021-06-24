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
        private QueryParameterMapping[] _mappings;

        public static QueryParameterValueSupplier? ForType(Type componentType)
        {
            if (!_cacheByType.TryGetValue(componentType, out var instanceOrNull))
            {
                var mappings = FindQueryParameterMappings(componentType);
                instanceOrNull = mappings == null ? null : new QueryParameterValueSupplier(mappings);
                _cacheByType.TryAdd(componentType, instanceOrNull);
            }

            return instanceOrNull;
        }

        private QueryParameterValueSupplier(QueryParameterMapping[] mappings)
        {
            _mappings = mappings;
        }

        public void RenderParameterAttributes(RenderTreeBuilder builder, ReadOnlySpan<char> queryString)
        {
            // We must always supply a value for all parameters that can be populated from the querystring
            // (so that, if no value is supplied, the parameter is reset back to a default). So start by
            // building a dictionary containing nulls.
            // TODO: Could this be precomputed and then fast-cloned somehow?
            var assignmentByComponentParameterName = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var mapping in _mappings)
            {
                foreach (var destination in mapping.Destinations)
                {
                    assignmentByComponentParameterName.Add(destination.ComponentParameterName, null);
                }
            }

            // Now we can populate the assignments dictionary in a single pass through the querystring
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
                        foreach (var destination in candidateMapping.Destinations)
                        {
                            // TODO: If we want to support multiple same-named params populating an array,
                            // extend this logic to build a list here then later convert to an array
                            var parsedValue = ParseValue(destination.ParameterType, suppliedPair.ValueEscaped);
                            assignmentByComponentParameterName[destination.ComponentParameterName] = parsedValue;
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

        private object ParseValue(Type parameterType, ReadOnlySpan<char> valueEscaped)
        {
            // TODO: This properly
            if (parameterType == typeof(int))
            {
                return int.Parse(valueEscaped);
            }
            else
            {
                return Uri.UnescapeDataString(valueEscaped.ToString().Replace('+', ' '));
            }
        }

        private static QueryParameterMapping[]? FindQueryParameterMappings(Type componentType)
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

                    mappingsByQueryParameterName[queryParameterName].Add(
                        new QueryParameterDestination(componentParameterName, propertyInfo.PropertyType));
                }
            }

            if (mappingsByQueryParameterName == null)
            {
                return null;
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
        private record QueryParameterDestination(string ComponentParameterName, Type ParameterType);
    }
}
