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
        return _enumMappings.GetOrAdd(
            enumDescriptor.ClrType,
            static (t, descriptor) => GetEnumMapping(descriptor.Name, t),
            enumDescriptor);
    }

    private static EnumMapping GetEnumMapping(string enumName, Type enumType)
    {
        var nameMappings = enumType.GetTypeInfo().DeclaredFields
            .Where(f => f.IsStatic)
            .Where(f => f.GetCustomAttributes<OriginalNameAttribute>().FirstOrDefault()?.PreferredAlias ?? true)
            .Select(f =>
            {
                // If the attribute hasn't been applied, fall back to the name of the field.
                var fieldName = f.GetCustomAttributes<OriginalNameAttribute>().FirstOrDefault()?.Name ?? f.Name;

                return new NameMapping
                {
                    Value = f.GetValue(null)!,
                    OriginalName = fieldName,
                    RemoveEnumPrefixName = GetEnumValueName(enumName, fieldName)
                };
            })
            .ToList();

        var writeMapping = nameMappings.ToDictionary(m => m.Value, m => m);

        // Add original names as fallback when mapping enum values with removed prefixes.
        // There are added to the dictionary first so they are overridden by the mappings with removed prefixes.
        var removeEnumPrefixMapping = nameMappings.ToDictionary(m => m.OriginalName, m => m.OriginalName);

        // Protobuf codegen prevents collision of enum names when the prefix is removed.
        // For example, the following enum will fail to build because both fields would resolve to "OK":
        //
        // enum Status {
        //     STATUS_OK = 0;
        //     OK = 1;
        // }
        //
        // Tooling error message:
        // ----------------------
        // Enum name OK has the same name as STATUS_OK if you ignore case and strip out the enum name prefix (if any).
        // (If you are using allow_alias, please assign the same number to each enum value name.)
        //
        // Just in case it does happen, map to the first value rather than error.
        foreach (var item in nameMappings.GroupBy(m => m.RemoveEnumPrefixName).Select(g => KeyValuePair.Create(g.Key, g.First().OriginalName)))
        {
            removeEnumPrefixMapping[item.Key] = item.Value;
        }

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
        public required object Value { get; init; }
        public required string OriginalName { get; init; }
        public required string RemoveEnumPrefixName { get; init; }
    }
}
