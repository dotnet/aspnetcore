// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Components.Reflection;
using Microsoft.AspNetCore.Internal;

namespace Microsoft.AspNetCore.Components.Routing
{
    internal sealed class QueryParameterValueSupplier
    {
        private static readonly Dictionary<Type, QueryParameterValueSupplier?> _cacheByType = new();
        private readonly SortedDictionary<ReadOnlyMemory<char>, List<QueryParameterDestination>> _destinationsByQueryParameterName;
        private readonly QueryParameterDestination[] _destinations;
        private readonly int _destinationsCount;

        public static QueryParameterValueSupplier? ForType(Type componentType)
        {
            if (!_cacheByType.TryGetValue(componentType, out var instanceOrNull))
            {
                // If the component doesn't have any query parameters, store a null value for it
                // so we know the upstream code can't try to render query parameter frames for it.
                var destinations = BuildQueryParameterMappings(componentType, out var destinationsCount);
                instanceOrNull = destinations == null ? null : new QueryParameterValueSupplier(destinations, destinationsCount);
                _cacheByType.TryAdd(componentType, instanceOrNull);
            }

            return instanceOrNull;
        }

        private QueryParameterValueSupplier(SortedDictionary<ReadOnlyMemory<char>, List<QueryParameterDestination>> destinationsByQueryParameterName, int destinationsCount)
        {
            _destinationsByQueryParameterName = destinationsByQueryParameterName;
            _destinationsCount = destinationsCount;
            _destinations = new QueryParameterDestination[destinationsCount];
            var index = 0;
            foreach (var group in destinationsByQueryParameterName)
            {
                foreach (var destination in CollectionsMarshal.AsSpan(group.Value))
                {
                    _destinations[index++] = destination;
                }
            }
        }

        public void AddParametersFromQueryString(Dictionary<string, object?> target, ReadOnlyMemory<char> queryString)
        {
            // Temporary workspace in which we accumulate the data while walking the querystring.
            var valuesByDestination = ArrayPool<StringSegmentAccumulator>.Shared.Rent(_destinationsCount);

            try
            {
                // Capture values by destination in a single pass through the querystring
                var queryStringEnumerable = new QueryStringEnumerable(queryString);
                foreach (var suppliedPair in queryStringEnumerable)
                {
                    if (_destinationsByQueryParameterName.TryGetValue(suppliedPair.EncodedName, out var destinations))
                    {
                        var decodedValue = suppliedPair.DecodeValue();

                        foreach (ref var destination in CollectionsMarshal.AsSpan(destinations))
                        {
                            if (destination.IsArray)
                            {
                                valuesByDestination[destination.Index].Add(decodedValue);
                            }
                            else
                            {
                                valuesByDestination[destination.Index].SetSingle(decodedValue);
                            }
                        }
                    }
                }

                // Finally, populate the target dictionary by parsing all the string segments and building arrays
                for (var destinationIndex = 0; destinationIndex < _destinations.Length; destinationIndex++)
                {
                    ref var destination = ref _destinations[destinationIndex];
                    ref var values = ref valuesByDestination[destination.Index];

                    target[destination.ComponentParameterName] = destination.IsArray
                        ? destination.Parser.ParseMultiple(values, destination.ComponentParameterName)
                        : values.Count == 0
                            ? default
                            : destination.Parser.Parse(values[0].Span, destination.ComponentParameterName);
                }
            }
            finally
            {
                ArrayPool<StringSegmentAccumulator>.Shared.Return(valuesByDestination, true);
            }
        }

        private static SortedDictionary<ReadOnlyMemory<char>, List<QueryParameterDestination>>? BuildQueryParameterMappings(Type componentType, out int destinationsCount)
        {
            var candidateProperties = MemberAssignment.GetPropertiesIncludingInherited(componentType, ComponentProperties.BindablePropertyFlags);
            SortedDictionary<ReadOnlyMemory<char>, List<QueryParameterDestination>>? result = null;
            destinationsCount = 0;

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
                    var queryParameterName = (string.IsNullOrEmpty(fromQueryAttribute.Name)
                        ? componentParameterName
                        : fromQueryAttribute.Name).AsMemory();

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
                    result ??= new(QueryParameterNameComparer.Instance);
                    if (!result.TryGetValue(queryParameterName, out var list))
                    {
                        result[queryParameterName] = list = new();
                    }
                    list.Add(new QueryParameterDestination(componentParameterName, parser, isArray, destinationsCount++));
                }
            }

            return result;
        }

        private readonly struct QueryParameterDestination
        {
            public readonly string ComponentParameterName;
            public readonly UrlValueConstraint Parser;
            public readonly bool IsArray;
            public readonly int Index;

            public QueryParameterDestination(string componentParameterName, UrlValueConstraint parser, bool isArray, int index)
            {
                ComponentParameterName = componentParameterName;
                Parser = parser;
                IsArray = isArray;
                Index = index;
            }
        }

        private class QueryParameterNameComparer : IComparer<ReadOnlyMemory<char>>
        {
            public static readonly QueryParameterNameComparer Instance = new();

            public int Compare(ReadOnlyMemory<char> x, ReadOnlyMemory<char> y)
                => x.Span.CompareTo(y.Span, StringComparison.OrdinalIgnoreCase);
        }
    }
}
