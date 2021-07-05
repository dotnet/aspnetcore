// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Components.Reflection;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Internal;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components.Routing
{
    internal sealed class QueryParameterValueSupplier
    {
        public static void ClearCache() => _cacheByType.Clear();

        private static readonly Dictionary<Type, QueryParameterValueSupplier?> _cacheByType = new();
        private readonly SortedDictionary<ReadOnlyMemory<char>, QueryParameterDestination> _destinationsByQueryParameterName;
        private readonly QueryParameterDestination[] _destinations;

        public static QueryParameterValueSupplier? ForType([DynamicallyAccessedMembers(Component)] Type componentType)
        {
            if (!_cacheByType.TryGetValue(componentType, out var instanceOrNull))
            {
                // If the component doesn't have any query parameters, store a null value for it
                // so we know the upstream code can't try to render query parameter frames for it.
                var destinations = BuildQueryParameterMappings(componentType);
                instanceOrNull = destinations == null ? null : new QueryParameterValueSupplier(destinations);
                _cacheByType.TryAdd(componentType, instanceOrNull);
            }

            return instanceOrNull;
        }

        private QueryParameterValueSupplier(SortedDictionary<ReadOnlyMemory<char>, QueryParameterDestination> destinationsByQueryParameterName)
        {
            _destinationsByQueryParameterName = destinationsByQueryParameterName;

            // Also store a flat array of destinations for lookup by index
            _destinations = new QueryParameterDestination[destinationsByQueryParameterName.Count];
            foreach (var destination in _destinationsByQueryParameterName.Values)
            {
                _destinations[destination.Index] = destination;
            }
        }

        public void RenderParametersFromQueryString(RenderTreeBuilder builder, ReadOnlyMemory<char> queryString)
        {
            // If there's no querystring contents, we can skip renting from the pool
            if (queryString.IsEmpty)
            {
                for (var destinationIndex = 0; destinationIndex < _destinations.Length; destinationIndex++)
                {
                    ref var destination = ref _destinations[destinationIndex];
                    var blankValue = destination.IsArray ? destination.Parser.ParseMultiple(default, string.Empty) : null;
                    builder.AddAttribute(0, destination.ComponentParameterName, blankValue);
                }
                return;
            }

            // Temporary workspace in which we accumulate the data while walking the querystring.
            var valuesByDestination = ArrayPool<StringSegmentAccumulator>.Shared.Rent(_destinations.Length);

            try
            {
                // Capture values by destination in a single pass through the querystring
                var queryStringEnumerable = new QueryStringEnumerable(queryString);
                foreach (var suppliedPair in queryStringEnumerable)
                {
                    if (_destinationsByQueryParameterName.TryGetValue(suppliedPair.EncodedName, out var destination))
                    {
                        var decodedValue = suppliedPair.DecodeValue();

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

                // Finally, emit the parameter attributes by parsing all the string segments and building arrays
                for (var destinationIndex = 0; destinationIndex < _destinations.Length; destinationIndex++)
                {
                    ref var destination = ref _destinations[destinationIndex];
                    ref var values = ref valuesByDestination[destination.Index];

                    var parsedValue = destination.IsArray
                        ? destination.Parser.ParseMultiple(values, destination.ComponentParameterName)
                        : values.Count == 0
                            ? default
                            : destination.Parser.Parse(values[0].Span, destination.ComponentParameterName);

                    builder.AddAttribute(0, destination.ComponentParameterName, parsedValue);
                }
            }
            finally
            {
                ArrayPool<StringSegmentAccumulator>.Shared.Return(valuesByDestination, true);
            }
        }

        private static SortedDictionary<ReadOnlyMemory<char>, QueryParameterDestination>? BuildQueryParameterMappings(
            [DynamicallyAccessedMembers(Component)] Type componentType)
        {
            var candidateProperties = MemberAssignment.GetPropertiesIncludingInherited(componentType, ComponentProperties.BindablePropertyFlags);
            SortedDictionary<ReadOnlyMemory<char>, QueryParameterDestination>? result = null;
            var destinationIndex = 0;

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

                    // Add the destination for this component parameter name
                    result ??= new(QueryParameterNameComparer.Instance);
                    if (result.ContainsKey(queryParameterName))
                    {
                        throw new InvalidOperationException($"The component '{componentType}' declares more than one mapping for the query parameter '{queryParameterName}'.");
                    }

                    result.Add(queryParameterName, new QueryParameterDestination(componentParameterName, parser, isArray, destinationIndex++));
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
