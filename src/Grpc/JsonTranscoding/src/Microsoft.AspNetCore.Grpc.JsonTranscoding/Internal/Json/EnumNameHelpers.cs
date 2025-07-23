// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using Google.Protobuf.Reflection;

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal.Json;

internal static class JsonNamingHelpers
{
    private static readonly ConcurrentDictionary<Type, EnumMapping> _enumMappings = new ConcurrentDictionary<Type, EnumMapping>();

    internal static EnumValueDescriptor? GetEnumFieldReadValue(EnumDescriptor enumDescriptor, string value, GrpcJsonSettings settings)
    {
        string resolvedName;
        if (settings.RemoveEnumPrefix)
        {
            var nameMapping = GetEnumMapping(enumDescriptor);
            if (!nameMapping.RemoveEnumPrefixMapping.TryGetValue(value, out var n))
            {
                return null;
            }

            resolvedName = n;
        }
        else
        {
            resolvedName = value;
        }

        return enumDescriptor.FindValueByName(resolvedName);
    }

    internal static string? GetEnumFieldWriteName(EnumDescriptor enumDescriptor, object value, GrpcJsonSettings settings)
    {
        var enumMapping = GetEnumMapping(enumDescriptor);

        // If this returns false, name will be null, which is what we want.
        if (!enumMapping.WriteMapping.TryGetValue(value, out var mapping))
        {
            return null;
        }

        return settings.RemoveEnumPrefix ? mapping.RemoveEnumPrefixName : mapping.OriginalName;
    }

    private static EnumMapping GetEnumMapping(EnumDescriptor enumDescriptor)
    {
        var enumType = enumDescriptor.ClrType;

        EnumMapping? enumMapping;
        lock (_enumMappings)
        {
            if (!_enumMappings.TryGetValue(enumType, out enumMapping))
            {
                _enumMappings[enumType] = enumMapping = GetEnumMapping(enumDescriptor.Name, enumType);
            }
        }

        return enumMapping;
    }

    private static EnumMapping GetEnumMapping(string enumName, Type enumType)
    {
        var enumFields = enumType.GetTypeInfo().DeclaredFields
            .Where(f => f.IsStatic)
            .Where(f => f.GetCustomAttributes<OriginalNameAttribute>().FirstOrDefault()?.PreferredAlias ?? true)
            .ToList();

        var writeMapping = enumFields.ToDictionary(
            f => f.GetValue(null)!,
            f =>
            {
                // If the attribute hasn't been applied, fall back to the name of the field.
                var fieldName = f.GetCustomAttributes<OriginalNameAttribute>().FirstOrDefault()?.Name ?? f.Name;

                return new NameMapping
                {
                    OriginalName = fieldName,
                    RemoveEnumPrefixName = GetEnumValueName(enumName, fieldName)
                };
            });

        var removeEnumPrefixMapping = writeMapping.Values.ToDictionary(
            m => m.RemoveEnumPrefixName,
            m => m.OriginalName);

        return new EnumMapping { WriteMapping = writeMapping, RemoveEnumPrefixMapping = removeEnumPrefixMapping };
    }

    // Remove the prefix from the specified value. Ignore case and underscores in the comparison.
    private static string TryRemovePrefix(string prefix, string value)
    {
        var normalizedPrefix = prefix.Replace("_", string.Empty, StringComparison.Ordinal).ToLowerInvariant();

        var prefixIndex = 0;
        var valueIndex = 0;

        while (prefixIndex < normalizedPrefix.Length && valueIndex < value.Length)
        {
            if (value[valueIndex] == '_')
            {
                valueIndex++;
                continue;
            }

            if (char.ToLowerInvariant(value[valueIndex]) != normalizedPrefix[prefixIndex])
            {
                return value;
            }

            prefixIndex++;
            valueIndex++;
        }

        if (prefixIndex < normalizedPrefix.Length)
        {
            return value;
        }

        while (valueIndex < value.Length && value[valueIndex] == '_')
        {
            valueIndex++;
        }

        return valueIndex == value.Length ? value : value.Substring(valueIndex);
    }

    private static string GetEnumValueName(string enumName, string valueName)
    {
        var result = TryRemovePrefix(enumName, valueName);

        // Prefix name starting with a digit with an underscore to ensure it is a valid identifier.
        return result.Length > 0 && char.IsDigit(result[0])
            ? $"_{result}"
            : result;
    }

    private sealed class EnumMapping
    {
        public required Dictionary<object, NameMapping> WriteMapping { get; init; }
        public required Dictionary<string, string> RemoveEnumPrefixMapping { get; init; }
    }

    private sealed class NameMapping
    {
        public required string OriginalName { get; init; }
        public required string RemoveEnumPrefixName { get; init; }
    }
}
