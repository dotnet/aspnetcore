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
        private readonly Dictionary<QueryParameterDestination, object?> _assignmentsTemplate;

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
                    _assignmentsTemplate.Add(destination, null);
                }
            }
        }

        public void RenderParameterAttributes(RenderTreeBuilder builder, ReadOnlySpan<char> queryString)
        {
            // TODO: For unparseable values, should we throw? I'm starting to think so, since there's no
            // normal case where you'd expect to see unparseable values, and if you really need this,
            // set the type to string and do you own parsing later.

            // TODO: Consider changing this to Dictionary<QueryParameterDestination, StringValues> and just
            // accumulating the decoded values by destination. Then when emitting the render tree frames,
            // you can allocate arrays of the right size and parse into them.
            // This only works if you're willing to throw for unparseable values, otherwise you wouldn't
            // be able to know the right size up front.
            // Benefit is that we can eliminate the whole concept of lists from UrlValueConstraint.
            var assignmentsByDestination = new Dictionary<QueryParameterDestination, object?>(_assignmentsTemplate);

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
                            if (destination.IsArray)
                            {
                                // If we're supplying an array, then assignmentsByDestination will be accumulating
                                // the values in a List<T>. Only the closed-generic-type parser knows how to append
                                // typed values to that list. The code here just sees it as Object.
                                assignmentsByDestination.TryGetValue(destination, out var existingList);
                                if (destination.Parser.TryAppendListValue(existingList, unescapedValue, out var updatedList))
                                {
                                    assignmentsByDestination[destination] = updatedList;
                                }
                            }
                            else if (destination.Parser.TryParseUntyped(unescapedValue, out var parsedVaue))
                            {
                                assignmentsByDestination[destination] = parsedVaue;
                            }
                        }

                        break;
                    }
                }
            }

            // Finally actually emit the rendertree frames
            foreach (var (destination, value) in assignmentsByDestination)
            {
                // If we're supplying multiple values, we'll have a List<T> here. The closed-generic-type
                // parser knows how to convert this into a T[].
                var finalValue = destination.IsArray && value is not null
                    ? destination.Parser.ToArray(value)
                    : value;
                builder.AddAttribute(0, destination.ComponentParameterName, finalValue);
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

                    // If it's nullable, we can just use the underlying type, because missing values
                    // will naturally leave the default value in the assignments list
                    if (Nullable.GetUnderlyingType(effectiveType) is Type nullableUnderlyingType)
                    {
                        if (isArray)
                        {
                            // We could support this, but it would greatly complicate things, and it's unclear there are scenarios for it
                            throw new NotSupportedException($"Querystring values cannot be parsed as arrays of nullables");
                        }

                        effectiveType = nullableUnderlyingType;
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
