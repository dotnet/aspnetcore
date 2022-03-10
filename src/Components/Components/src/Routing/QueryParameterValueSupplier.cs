// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Components.Reflection;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Internal;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components.Routing;

internal sealed class QueryParameterValueSupplier
{
    public static void ClearCache() => _cacheByType.Clear();

    private static readonly ConcurrentDictionary<Type, QueryParameterValueSupplier?> _cacheByType = new();

    // These two arrays contain the same number of entries, and their corresponding positions refer to each other.
    // Holding the info like this means we can use Array.BinarySearch with less custom implementation.
    private readonly ReadOnlyMemory<char>[] _queryParameterNames;
    private readonly QueryParameterDestination[] _destinations;

    public static QueryParameterValueSupplier? ForType([DynamicallyAccessedMembers(Component)] Type componentType)
    {
        if (!_cacheByType.TryGetValue(componentType, out var instanceOrNull))
        {
            // If the component doesn't have any query parameters, store a null value for it
            // so we know the upstream code can't try to render query parameter frames for it.
            var sortedMappings = GetSortedMappings(componentType);
            instanceOrNull = sortedMappings == null ? null : new QueryParameterValueSupplier(sortedMappings);
            _cacheByType.TryAdd(componentType, instanceOrNull);
        }

        return instanceOrNull;
    }

    private QueryParameterValueSupplier(QueryParameterMapping[] sortedMappings)
    {
        _queryParameterNames = new ReadOnlyMemory<char>[sortedMappings.Length];
        _destinations = new QueryParameterDestination[sortedMappings.Length];
        for (var i = 0; i < sortedMappings.Length; i++)
        {
            ref var mapping = ref sortedMappings[i];
            _queryParameterNames[i] = mapping.QueryParameterName;
            _destinations[i] = mapping.Destination;
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
        var valuesByMapping = ArrayPool<StringSegmentAccumulator>.Shared.Rent(_destinations.Length);

        try
        {
            // Capture values by destination in a single pass through the querystring
            var queryStringEnumerable = new QueryStringEnumerable(queryString);
            foreach (var suppliedPair in queryStringEnumerable)
            {
                var decodedName = suppliedPair.DecodeName();
                var mappingIndex = Array.BinarySearch(_queryParameterNames, decodedName, QueryParameterNameComparer.Instance);
                if (mappingIndex >= 0)
                {
                    var decodedValue = suppliedPair.DecodeValue();

                    if (_destinations[mappingIndex].IsArray)
                    {
                        valuesByMapping[mappingIndex].Add(decodedValue);
                    }
                    else
                    {
                        valuesByMapping[mappingIndex].SetSingle(decodedValue);
                    }
                }
            }

            // Finally, emit the parameter attributes by parsing all the string segments and building arrays
            for (var mappingIndex = 0; mappingIndex < _destinations.Length; mappingIndex++)
            {
                ref var destination = ref _destinations[mappingIndex];
                ref var values = ref valuesByMapping[mappingIndex];

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
            ArrayPool<StringSegmentAccumulator>.Shared.Return(valuesByMapping, true);
        }
    }

    private static QueryParameterMapping[]? GetSortedMappings([DynamicallyAccessedMembers(Component)] Type componentType)
    {
        var candidateProperties = MemberAssignment.GetPropertiesIncludingInherited(componentType, ComponentProperties.BindablePropertyFlags);
        HashSet<ReadOnlyMemory<char>>? usedQueryParameterNames = null;
        List<QueryParameterMapping>? mappings = null;

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
                usedQueryParameterNames ??= new(QueryParameterNameComparer.Instance);
                if (usedQueryParameterNames.Contains(queryParameterName))
                {
                    throw new InvalidOperationException($"The component '{componentType}' declares more than one mapping for the query parameter '{queryParameterName}'.");
                }
                usedQueryParameterNames.Add(queryParameterName);

                mappings ??= new();
                mappings.Add(new QueryParameterMapping
                {
                    QueryParameterName = queryParameterName,
                    Destination = new QueryParameterDestination(componentParameterName, parser, isArray)
                });
            }
        }

        mappings?.Sort((a, b) => QueryParameterNameComparer.Instance.Compare(a.QueryParameterName, b.QueryParameterName));
        return mappings?.ToArray();
    }

    private readonly struct QueryParameterMapping
    {
        public ReadOnlyMemory<char> QueryParameterName { get; init; }
        public QueryParameterDestination Destination { get; init; }
    }

    private readonly struct QueryParameterDestination
    {
        public readonly string ComponentParameterName;
        public readonly UrlValueConstraint Parser;
        public readonly bool IsArray;

        public QueryParameterDestination(string componentParameterName, UrlValueConstraint parser, bool isArray)
        {
            ComponentParameterName = componentParameterName;
            Parser = parser;
            IsArray = isArray;
        }
    }
}
