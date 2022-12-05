// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Components.Reflection;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.Primitives;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components.Routing;

internal sealed class FormParameterValueSupplier
{
    public static void ClearCache() => _cacheByType.Clear();

    private static readonly ConcurrentDictionary<Type, FormParameterValueSupplier?> _cacheByType = new();

    // These two arrays contain the same number of entries, and their corresponding positions refer to each other.
    // Holding the info like this means we can use Array.BinarySearch with less custom implementation.
    private readonly string[] _formFieldNames;
    private readonly FormParameterDestination[] _destinations;

    public static FormParameterValueSupplier? ForType([DynamicallyAccessedMembers(Component)] Type componentType)
    {
        if (!_cacheByType.TryGetValue(componentType, out var instanceOrNull))
        {
            // If the component doesn't have any form parameters, store a null value for it
            // so we know the upstream code can't try to render query parameter frames for it.
            var sortedMappings = GetSortedMappings(componentType);
            instanceOrNull = sortedMappings == null ? null : new FormParameterValueSupplier(sortedMappings);
            _cacheByType.TryAdd(componentType, instanceOrNull);
        }

        return instanceOrNull;
    }

    private FormParameterValueSupplier(FormParameterMapping[] sortedMappings)
    {
        _formFieldNames = new string[sortedMappings.Length];
        _destinations = new FormParameterDestination[sortedMappings.Length];
        for (var i = 0; i < sortedMappings.Length; i++)
        {
            ref var mapping = ref sortedMappings[i];
            _formFieldNames[i] = mapping.FormParameterName;
            _destinations[i] = mapping.Destination;
        }
    }

    private static FormParameterMapping[]? GetSortedMappings([DynamicallyAccessedMembers(Component)] Type componentType)
    {
        var candidateProperties = MemberAssignment.GetPropertiesIncludingInherited(componentType, ComponentProperties.BindablePropertyFlags);
        HashSet<string>? usedFormParameterNames = null;
        List<FormParameterMapping>? mappings = null;

        foreach (var propertyInfo in candidateProperties)
        {
            if (!propertyInfo.IsDefined(typeof(ParameterAttribute)))
            {
                continue;
            }

            var fromFormAttribute = propertyInfo.GetCustomAttribute<SupplyParameterFromFormAttribute>();
            if (fromFormAttribute is not null)
            {
                // Found a parameter that's assignable from form
                var componentParameterName = propertyInfo.Name;
                var formParameterName = (string.IsNullOrEmpty(fromFormAttribute.Name)
                    ? componentParameterName
                    : fromFormAttribute.Name);

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
                    throw new NotSupportedException($"Form values cannot be parsed as type '{propertyInfo.PropertyType}'.");
                }

                // Add the destination for this component parameter name
                usedFormParameterNames ??= new(StringComparer.OrdinalIgnoreCase);
                if (usedFormParameterNames.Contains(formParameterName))
                {
                    throw new InvalidOperationException($"The component '{componentType}' declares more than one mapping for the form parameter '{formParameterName}'.");
                }
                usedFormParameterNames.Add(formParameterName);

                mappings ??= new();
                mappings.Add(new FormParameterMapping
                {
                    FormParameterName = formParameterName,
                    Destination = new FormParameterDestination(componentParameterName, parser, isArray)
                });
            }
        }

        mappings?.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.FormParameterName, b.FormParameterName));
        return mappings?.ToArray();
    }

    public void RenderParametersFromForm(RenderTreeBuilder builder, IEnumerable<KeyValuePair<string, StringValues>> form)
    {
        foreach (var (formFieldName, formFieldValues) in form)
        {

            var mappingIndex = Array.BinarySearch(_formFieldNames, formFieldName, StringComparer.OrdinalIgnoreCase);
            if (mappingIndex >= 0)
            {
                ref var destination = ref _destinations[mappingIndex];
                var parsedValue = destination.IsArray
                    ? destination.Parser.ParseMultiple(formFieldValues, destination.ComponentParameterName)
                    : formFieldValues.Count == 0
                        ? default
                        : destination.Parser.Parse(formFieldValues[0], destination.ComponentParameterName);

                builder.AddAttribute(0, destination.ComponentParameterName, parsedValue);
            }
        }
    }

    private readonly struct FormParameterMapping
    {
        public string FormParameterName { get; init; }
        public FormParameterDestination Destination { get; init; }
    }

    private readonly struct FormParameterDestination
    {
        public readonly string ComponentParameterName;
        public readonly UrlValueConstraint Parser;
        public readonly bool IsArray;

        public FormParameterDestination(string componentParameterName, UrlValueConstraint parser, bool isArray)
        {
            ComponentParameterName = componentParameterName;
            Parser = parser;
            IsArray = isArray;
        }
    }
}
