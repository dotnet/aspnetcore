// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Microsoft.AspNetCore.OpenApi;

internal sealed class InferredSchemaReferenceIdResolver
{
    private static readonly object JsonPatchAliasIdentity = new();
    private readonly IReadOnlyDictionary<Type, string?> _referenceIds;
    private readonly IReadOnlyDictionary<(Type BaseType, Type BranchType), string> _polymorphicReferenceIds;
    private readonly Func<JsonTypeInfo, string?> _createSchemaReferenceId;

    private InferredSchemaReferenceIdResolver(
        IReadOnlyDictionary<Type, string?> referenceIds,
        IReadOnlyDictionary<(Type BaseType, Type BranchType), string> polymorphicReferenceIds,
        Func<JsonTypeInfo, string?> createSchemaReferenceId)
    {
        _referenceIds = referenceIds;
        _polymorphicReferenceIds = polymorphicReferenceIds;
        _createSchemaReferenceId = createSchemaReferenceId;
    }

    public static InferredSchemaReferenceIdResolver Create(
        IEnumerable<Type> types,
        JsonSerializerOptions serializerOptions,
        Func<Type, InferredSchemaDocument> getInferredSchema,
        Func<JsonTypeInfo, string?> createSchemaReferenceId,
        bool usesDefaultSchemaReferenceId)
    {
        var allTypes = new HashSet<Type>();
        var polymorphicBranches = new HashSet<(Type BaseType, Type BranchType)>();
        foreach (var rootType in types)
        {
            foreach (var shape in getInferredSchema(rootType).Shapes)
            {
                allTypes.Add(shape.Identity.Type);
                foreach (var derivedType in shape.DerivedTypes)
                {
                    polymorphicBranches.Add((shape.Identity.Type, derivedType.Identity.Type));
                }
            }
        }

        var entries = allTypes
            .OrderBy(GetCanonicalTypeName, StringComparer.Ordinal)
            .Select(type => new ReferenceIdEntry(
                type,
                createSchemaReferenceId(serializerOptions.GetTypeInfo(type)),
                GetAliasIdentity(type)))
            .ToArray();

        if (!usesDefaultSchemaReferenceId)
        {
            ValidateCustomReferenceIds(entries);
            var referenceIds = entries.ToDictionary(entry => entry.Type, entry => entry.Candidate);
            return new(
                new ReadOnlyDictionary<Type, string?>(referenceIds),
                new ReadOnlyDictionary<(Type BaseType, Type BranchType), string>(
                    ResolvePolymorphicReferenceIds(polymorphicBranches, referenceIds, usesDefaultSchemaReferenceId)),
                createSchemaReferenceId);
        }

        var resolvedIds = ResolveDefaultReferenceIds(entries);
        return new(
            new ReadOnlyDictionary<Type, string?>(resolvedIds),
            new ReadOnlyDictionary<(Type BaseType, Type BranchType), string>(
                ResolvePolymorphicReferenceIds(polymorphicBranches, resolvedIds, usesDefaultSchemaReferenceId)),
            createSchemaReferenceId);
    }

    public string? GetReferenceId(JsonTypeInfo typeInfo)
    {
        var type = Nullable.GetUnderlyingType(typeInfo.Type) ?? typeInfo.Type;
        if (_referenceIds.TryGetValue(type, out var referenceId))
        {
            return referenceId;
        }

        var candidate = _createSchemaReferenceId(typeInfo);
        ValidateReferenceId(type, candidate);
        return candidate;
    }

    public string? GetPolymorphicReferenceId(Type baseType, Type branchType)
    {
        if (_polymorphicReferenceIds.TryGetValue((baseType, branchType), out var referenceId))
        {
            return referenceId;
        }

        return null;
    }

    private static Dictionary<Type, string?> ResolveDefaultReferenceIds(ReferenceIdEntry[] entries)
    {
        var resolvedIds = entries.ToDictionary(entry => entry.Type, entry => entry.Candidate);
        var aliasGroups = entries
            .Where(entry => entry.Candidate is not null)
            .GroupBy(entry => entry.AliasIdentity)
            .Select(group =>
            {
                var orderedEntries = group.OrderBy(entry => GetCanonicalTypeName(entry.Type), StringComparer.Ordinal).ToArray();
                return new ReferenceIdAliasGroup(
                    group.Key,
                    orderedEntries,
                    CreateProgressiveNames(orderedEntries[0]));
            })
            .ToDictionary(group => group.AliasIdentity);
        var positions = aliasGroups.Keys.ToDictionary(aliasIdentity => aliasIdentity, _ => 0);

        while (true)
        {
            var collisions = aliasGroups.Values
                .GroupBy(
                    group => group.Candidates[positions[group.AliasIdentity]],
                    StringComparer.Ordinal)
                .Where(group => group.Skip(1).Any())
                .ToArray();
            if (collisions.Length == 0)
            {
                break;
            }

            foreach (var collision in collisions)
            {
                foreach (var group in collision)
                {
                    positions[group.AliasIdentity]++;
                    if (positions[group.AliasIdentity] >= group.Candidates.Count)
                    {
                        throw new InvalidOperationException(
                            $"Unable to create a unique OpenAPI schema reference ID for '{group.Entries[0].Type}'.");
                    }
                }
            }
        }

        foreach (var group in aliasGroups.Values)
        {
            var resolvedId = group.Candidates[positions[group.AliasIdentity]];
            foreach (var entry in group.Entries)
            {
                resolvedIds[entry.Type] = resolvedId;
            }
        }

        return resolvedIds;
    }

    private static IReadOnlyList<string> CreateProgressiveNames(ReferenceIdEntry entry)
    {
        var candidate = entry.Candidate!;
        var qualifiers = GetQualifierSegments(entry.Type);
        var names = new List<string>(qualifiers.Count + 2) { candidate };
        var prefix = string.Empty;
        foreach (var qualifier in qualifiers)
        {
            prefix = prefix.Length == 0 ? qualifier : $"{qualifier}.{prefix}";
            names.Add($"{prefix}.{candidate}");
        }

        names.Add($"{candidate}-{GetStableHash(entry.Type)}");
        return names;
    }

    private static IReadOnlyList<string> GetQualifierSegments(Type type)
    {
        var segments = new List<string>();
        for (var declaringType = type.DeclaringType; declaringType is not null; declaringType = declaringType.DeclaringType)
        {
            segments.Add(SanitizeReferenceId(RemoveGenericArity(declaringType.Name)));
        }

        if (type.Namespace is { Length: > 0 } @namespace)
        {
            var namespaceSegments = @namespace.Split('.');
            for (var i = namespaceSegments.Length - 1; i >= 0; i--)
            {
                segments.Add(SanitizeReferenceId(namespaceSegments[i]));
            }
        }

        return segments;
    }

    private static void ValidateCustomReferenceIds(ReferenceIdEntry[] entries)
    {
        foreach (var entry in entries)
        {
            ValidateReferenceId(entry.Type, entry.Candidate);
        }

        foreach (var collision in entries
            .Where(entry => entry.Candidate is not null)
            .GroupBy(entry => entry.Candidate!, StringComparer.Ordinal)
            .Where(group => group.Select(entry => entry.AliasIdentity).Distinct().Skip(1).Any()))
        {
            var types = string.Join(", ", collision.Select(entry => $"'{entry.Type}'"));
            throw new InvalidOperationException(
                $"The custom OpenAPI schema reference ID '{collision.Key}' is used by distinct serializer contract types: {types}.");
        }
    }

    private static Dictionary<(Type BaseType, Type BranchType), string> ResolvePolymorphicReferenceIds(
        IEnumerable<(Type BaseType, Type BranchType)> branches,
        IReadOnlyDictionary<Type, string?> referenceIds,
        bool usesDefaultSchemaReferenceId)
    {
        var occupiedIds = referenceIds
            .Where(entry => entry.Value is not null)
            .GroupBy(entry => entry.Value!, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => GetAliasIdentity(group.First().Key), StringComparer.Ordinal);
        var entries = branches
            .OrderBy(
                static branch => $"{GetCanonicalTypeName(branch.BaseType)}:{GetCanonicalTypeName(branch.BranchType)}",
                StringComparer.Ordinal)
            .Where(branch => referenceIds[branch.BaseType] is not null && referenceIds[branch.BranchType] is not null)
            .Select(branch =>
            {
                var baseId = referenceIds[branch.BaseType]!;
                var branchId = referenceIds[branch.BranchType]!;
                return new PolymorphicReferenceIdEntry(
                    branch,
                    [
                        $"{baseId}{branchId}",
                        $"{baseId}.{branchId}",
                        $"{baseId}.{branchId}-{GetStableHash(branch.BaseType, branch.BranchType)}",
                    ]);
            })
            .ToArray();
        var positions = entries.ToDictionary(entry => entry.Types, _ => 0);

        while (true)
        {
            var collisions = entries
                .GroupBy(entry => entry.Candidates[positions[entry.Types]], StringComparer.Ordinal)
                .Where(group => group.Skip(1).Any() || occupiedIds.ContainsKey(group.Key))
                .ToArray();
            if (collisions.Length == 0)
            {
                break;
            }

            if (!usesDefaultSchemaReferenceId)
            {
                var collision = collisions[0];
                throw new InvalidOperationException(
                    $"The custom OpenAPI schema reference IDs produce the duplicate component ID '{collision.Key}'.");
            }

            foreach (var collision in collisions)
            {
                foreach (var entry in collision)
                {
                    positions[entry.Types]++;
                    if (positions[entry.Types] >= entry.Candidates.Count)
                    {
                        throw new InvalidOperationException(
                            $"Unable to create a unique OpenAPI schema reference ID for '{entry.Types.BaseType}' and '{entry.Types.BranchType}'.");
                    }
                }
            }
        }

        return entries.ToDictionary(entry => entry.Types, entry => entry.Candidates[positions[entry.Types]]);
    }

    private static void ValidateReferenceId(Type type, string? referenceId)
    {
        if (referenceId is null)
        {
            return;
        }

        if (referenceId.Length == 0 || referenceId.Any(character =>
            !char.IsAsciiLetterOrDigit(character) &&
            character is not '.' and not '-' and not '_'))
        {
            throw new InvalidOperationException(
                $"The OpenAPI schema reference ID '{referenceId}' for '{type}' must contain only ASCII letters, digits, '.', '-', or '_'.");
        }
    }

    private static object GetAliasIdentity(Type type)
        => type.IsJsonPatchDocument() ? JsonPatchAliasIdentity : type;

    private static string GetCanonicalTypeName(Type type)
    {
        if (type.IsArray)
        {
            return $"{GetCanonicalTypeName(type.GetElementType()!)}Array{type.GetArrayRank()}";
        }

        if (type.IsGenericType)
        {
            var definition = type.GetGenericTypeDefinition();
            var arguments = string.Join(",", type.GetGenericArguments().Select(GetCanonicalTypeName));
            return $"{definition.Assembly.GetName().Name}:{definition.FullName}[{arguments}]";
        }

        return $"{type.Assembly.GetName().Name}:{type.FullName ?? type.Name}";
    }

    private static string SanitizeReferenceId(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            builder.Append(char.IsAsciiLetterOrDigit(character) || character is '.' or '-' or '_' ? character : '.');
        }

        return builder.ToString();
    }

    private static string GetStableHash(Type type)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(GetCanonicalTypeName(type)));
        return Convert.ToHexString(bytes.AsSpan(0, 6));
    }

    private static string GetStableHash(Type baseType, Type branchType)
    {
        var canonicalName = $"{GetCanonicalTypeName(baseType)}->{GetCanonicalTypeName(branchType)}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonicalName));
        return Convert.ToHexString(bytes.AsSpan(0, 6));
    }

    private static string RemoveGenericArity(string name)
        => name.LastIndexOf('`') is var index && index >= 0 ? name[..index] : name;

    private sealed record ReferenceIdEntry(Type Type, string? Candidate, object AliasIdentity);

    private sealed record ReferenceIdAliasGroup(
        object AliasIdentity,
        IReadOnlyList<ReferenceIdEntry> Entries,
        IReadOnlyList<string> Candidates);

    private sealed record PolymorphicReferenceIdEntry(
        (Type BaseType, Type BranchType) Types,
        IReadOnlyList<string> Candidates);
}
