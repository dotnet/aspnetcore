// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Components.Reflection;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Internal;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Components.Routing
{
    internal sealed class QueryParameterValueSupplier
    {
        private static readonly Dictionary<Type, QueryParameterValueSupplier?> _cacheByType = new();
        private readonly QueryParameterMapping[] _mappings;
        private readonly Dictionary<QueryParameterDestination, StringValues> _assignmentsTemplate;

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
            _assignmentsTemplate = new();
            foreach (var mapping in _mappings)
            {
                foreach (var destination in mapping.Destinations)
                {
                    _assignmentsTemplate.Add(destination, default);
                }
            }
        }

        public void RenderParameterAttributes(RenderTreeBuilder builder, ReadOnlySpan<char> queryString)
        {
            var assignmentsByDestination = new Dictionary<QueryParameterDestination, StringValues>(_assignmentsTemplate);

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
                    // Open question: should we support encoded parameter names?
                    // - It's very unlikely that anyone would want to have parameter names that require encoding,
                    //   given that they are mapping them to C# properties and not some more complex data structure
                    // - Doing the comparisons without decoding is better for perf, especially against hostile input
                    // - We could add support for decoding later non-breakingly
                    // ... so I think for now, I'm on the side of not supporting encoded parameter names.
                    if (suppliedPair.EncodedName.Equals(candidateMapping.QueryParameterName, StringComparison.OrdinalIgnoreCase))
                    {
                        var decodedValue = suppliedPair.DecodeValue();
                        foreach (var destination in candidateMapping.Destinations)
                        {
                            if (destination.IsArray)
                            {
                                // Collect as many values as we receive
                                assignmentsByDestination[destination] = StringValues.Concat(
                                    assignmentsByDestination[destination],
                                    decodedValue.ToString());
                            }
                            else
                            {
                                // TODO: Consider some mechanism for not stringifying this span and retaining it as span
                                // for the parser
                                assignmentsByDestination[destination] = new StringValues(decodedValue.ToString());
                            }
                        }

                        break;
                    }
                }
            }

            // Finally actually emit the rendertree frames
            foreach (var (destination, value) in assignmentsByDestination)
            {
                var valueToSupply = destination.IsArray
                    ? destination.Parser.ParseMultiple(value, destination.ComponentParameterName)
                    : value.Count == 0
                        ? default
                        : destination.Parser.Parse(value[0], destination.ComponentParameterName);
                builder.AddAttribute(0, destination.ComponentParameterName, valueToSupply);
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

                    // Lazily create a destination list this querystring parameter name
                    mappingsByQueryParameterName ??= new(StringComparer.OrdinalIgnoreCase);
                    if (!mappingsByQueryParameterName.ContainsKey(queryParameterName))
                    {
                        mappingsByQueryParameterName.Add(queryParameterName, new());
                    }

                    // If it's an array type, capture that info and prepare to parse the element type
                    Type effectiveType = propertyInfo.PropertyType;
                    var isArray = false;
                    if (effectiveType.IsArray)
                    {
                        isArray = true;
                        effectiveType = effectiveType.GetElementType()!;
                    }

                    if (!UrlValueConstraint.TryGetByTargetType(effectiveType, out var parser))
                    {
                        throw new NotSupportedException($"Querystring values cannot be parsed as type '{propertyInfo.PropertyType}'.");
                    }

                    // Append a destination list entry for this component parameter name
                    mappingsByQueryParameterName[queryParameterName].Add(
                        new QueryParameterDestination(componentParameterName, parser, isArray));
                }
            }

            if (mappingsByQueryParameterName == null)
            {
                return Array.Empty<QueryParameterMapping>();
            }
            else
            {
                // Flatten the dictionary to a plain array. For the expected usage patterns, this will
                // be faster to seek into (see comment above).
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

        private record QueryParameterDestination(string ComponentParameterName, UrlValueConstraint Parser, bool IsArray);
    }
}
