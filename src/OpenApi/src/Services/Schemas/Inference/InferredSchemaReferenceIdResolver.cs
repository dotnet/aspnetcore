// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Microsoft.AspNetCore.OpenApi;

internal sealed class InferredSchemaReferenceIdResolverSet
{
    private static readonly object JsonPatchAliasIdentity = new();
    private readonly IReadOnlyDictionary<InferredSchemaPurpose, InferredSchemaReferenceIdResolver> _resolvers;

    private InferredSchemaReferenceIdResolverSet(
        IReadOnlyDictionary<InferredSchemaPurpose, InferredSchemaReferenceIdResolver> resolvers)
    {
        _resolvers = resolvers;
    }

    public static InferredSchemaReferenceIdResolverSet Create(
        IEnumerable<InferredSchemaRoot> roots,
        JsonSerializerOptions serializerOptions,
        Func<Type, InferredSchemaPurpose, InferredSchemaDocument> getInferredSchema,
        Func<JsonTypeInfo, string?> createSchemaReferenceId,
        bool usesDefaultSchemaReferenceId)
    {
        var rootsByPurpose = roots
            .Distinct()
            .GroupBy(root => root.Purpose)
            .ToDictionary(
                group => group.Key,
                group => group.Select(root => root.Type).ToArray());
        var purposesByAlias = new Dictionary<object, HashSet<InferredSchemaPurpose>>();
        var shapesByContract = new Dictionary<(Type Type, InferredSchemaPurpose Purpose), InferredSchemaShape>();
        var plannedContracts = new HashSet<(Type Type, InferredSchemaPurpose Purpose)>();
        foreach (var (purpose, rootTypes) in rootsByPurpose)
        {
            foreach (var rootType in rootTypes)
            {
                foreach (var shape in getInferredSchema(rootType, purpose).Shapes)
                {
                    plannedContracts.Add((shape.Identity.Type, purpose));
                    shapesByContract.TryAdd((shape.Identity.Type, purpose), shape);
                    var alias = GetAliasIdentity(shape.Identity.Type);
                    if (!purposesByAlias.TryGetValue(alias, out var purposes))
                    {
                        purposes = [];
                        purposesByAlias.Add(alias, purposes);
                    }
                    purposes.Add(purpose);
                }
            }
        }

        var candidates = plannedContracts
            .Select(contract => contract.Type)
            .Distinct()
            .ToDictionary(
                type => type,
                type => createSchemaReferenceId(serializerOptions.GetTypeInfo(type)));
        var aliasesNeedingPurposeQualification = purposesByAlias
            .Where(entry => entry.Value.Count > 1 &&
                HasDirectionalDifference(entry.Key, entry.Value))
            .Select(entry => entry.Key)
            .ToHashSet();
        var addedQualification = true;
        while (addedQualification)
        {
            addedQualification = false;
            foreach (var (alias, purposes) in purposesByAlias)
            {
                if (purposes.Count < 2 || aliasesNeedingPurposeQualification.Contains(alias))
                {
                    continue;
                }

                if (GetShapes(alias, purposes)
                    .SelectMany(GetReferencedAliases)
                    .Any(aliasesNeedingPurposeQualification.Contains))
                {
                    aliasesNeedingPurposeQualification.Add(alias);
                    addedQualification = true;
                }
            }
        }

        if (usesDefaultSchemaReferenceId)
        {
            foreach (var collision in plannedContracts
                .Where(contract => candidates[contract.Type] is not null)
                .GroupBy(contract => candidates[contract.Type]!, StringComparer.Ordinal)
                .Where(group =>
                    group.Select(contract => contract.Purpose).Distinct().Count() > 1 &&
                    group.Select(contract => GetAliasIdentity(contract.Type)).Distinct().Count() > 1))
            {
                foreach (var contract in collision)
                {
                    aliasesNeedingPurposeQualification.Add(GetAliasIdentity(contract.Type));
                }
            }
        }

        if (!usesDefaultSchemaReferenceId)
        {
            var collisions = plannedContracts
                .Select(contract => (
                    contract.Type,
                    Alias: GetAliasIdentity(contract.Type),
                    Candidate: GetQualifiedCandidate(contract.Type, contract.Purpose)))
                .Where(entry => entry.Candidate is not null)
                .GroupBy(entry => entry.Candidate!, StringComparer.Ordinal)
                .FirstOrDefault(group => group.Select(entry => entry.Alias).Distinct().Count() > 1);
            if (collisions is not null)
            {
                var conflictingTypes = collisions
                    .Select(entry => entry.Type)
                    .Distinct()
                    .OrderBy(type => type.ToString(), StringComparer.Ordinal)
                    .Select(type => $"'{type}'");
                throw new InvalidOperationException(
                    $"The custom OpenAPI schema reference ID '{collisions.Key}' is used by distinct serializer contract identities: " +
                    string.Join(", ", conflictingTypes) + ".");
            }
        }

        var resolvers = new Dictionary<InferredSchemaPurpose, InferredSchemaReferenceIdResolver>();
        var endpointRootTypes = rootsByPurpose
            .Where(entry => entry.Key != InferredSchemaPurpose.Neutral)
            .SelectMany(entry => entry.Value)
            .Distinct()
            .ToArray();
        foreach (var purpose in Enum.GetValues<InferredSchemaPurpose>())
        {
            var rootTypes = rootsByPurpose.GetValueOrDefault(purpose) ??
                (purpose == InferredSchemaPurpose.Neutral ? endpointRootTypes : []);
            resolvers.Add(
                purpose,
                InferredSchemaReferenceIdResolver.Create(
                    rootTypes,
                    serializerOptions,
                    type => getInferredSchema(type, purpose),
                    typeInfo => GetQualifiedCandidate(typeInfo.Type, purpose),
                    usesDefaultSchemaReferenceId));
        }

        return new(new ReadOnlyDictionary<InferredSchemaPurpose, InferredSchemaReferenceIdResolver>(resolvers));

        string? GetQualifiedCandidate(Type type, InferredSchemaPurpose purpose)
        {
            var candidate = candidates.TryGetValue(type, out var plannedCandidate)
                ? plannedCandidate
                : createSchemaReferenceId(serializerOptions.GetTypeInfo(type));
            return candidate is not null &&
                aliasesNeedingPurposeQualification.Contains(GetAliasIdentity(type))
                    ? $"{candidate}.{purpose}"
                    : candidate;
        }

        bool HasDirectionalDifference(object alias, IEnumerable<InferredSchemaPurpose> purposes)
        {
            if (ReferenceEquals(alias, JsonPatchAliasIdentity))
            {
                return false;
            }

            using var enumerator = GetShapes(alias, purposes).GetEnumerator();
            if (!enumerator.MoveNext())
            {
                return false;
            }

            var first = enumerator.Current;
            while (enumerator.MoveNext())
            {
                if (!HaveSameDirectionalContract(first, enumerator.Current))
                {
                    return true;
                }
            }

            return false;
        }

        IEnumerable<InferredSchemaShape> GetShapes(
            object alias,
            IEnumerable<InferredSchemaPurpose> purposes)
        {
            foreach (var purpose in purposes)
            {
                foreach (var contract in plannedContracts)
                {
                    if (contract.Purpose == purpose &&
                        GetAliasIdentity(contract.Type).Equals(alias) &&
                        shapesByContract.TryGetValue(contract, out var shape))
                    {
                        yield return shape;
                        break;
                    }
                }
            }
        }
    }

    public InferredSchemaReferenceIdResolver Get(InferredSchemaPurpose purpose) => _resolvers[purpose];

    private static object GetAliasIdentity(Type type)
        => type.IsJsonPatchDocument() ? JsonPatchAliasIdentity : type;

    private static bool HaveSameDirectionalContract(InferredSchemaShape left, InferredSchemaShape right)
        => left.Properties.SequenceEqual(right.Properties) &&
            left.ExtensionDataProperty == right.ExtensionDataProperty;

    private static IEnumerable<object> GetReferencedAliases(InferredSchemaShape shape)
    {
        if (shape.BaseType is { } baseType)
        {
            yield return GetAliasIdentity(baseType.Type);
        }
        if (shape.ElementType is { } elementType)
        {
            yield return GetAliasIdentity(elementType.Identity.Type);
        }
        if (shape.AdditionalPropertiesType is { } additionalPropertiesType)
        {
            yield return GetAliasIdentity(additionalPropertiesType.Identity.Type);
        }
        foreach (var property in shape.Properties)
        {
            yield return GetAliasIdentity(property.PropertyType.Identity.Type);
        }
        foreach (var derivedType in shape.DerivedTypes)
        {
            yield return GetAliasIdentity(derivedType.Identity.Type);
        }
        foreach (var unionCase in shape.UnionCases)
        {
            yield return GetAliasIdentity(unionCase.Identity.Type);
        }
        foreach (var tupleElement in shape.TupleElements)
        {
            yield return GetAliasIdentity(tupleElement.Identity.Type);
        }
    }
}

internal sealed class InferredSchemaReferenceIdResolver
{
    private static readonly object JsonPatchAliasIdentity = new();
    private readonly IReadOnlyDictionary<Type, string?> _referenceIds;
    private readonly IReadOnlyDictionary<(Type BaseType, Type BranchType), string> _polymorphicReferenceIds;
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly Func<Type, InferredSchemaDocument> _getInferredSchema;
    private readonly Func<JsonTypeInfo, string?> _createSchemaReferenceId;
    private readonly bool _usesDefaultSchemaReferenceId;
    private readonly object _unplannedReferenceIdLock = new();
    private readonly Dictionary<Type, string?> _unplannedReferenceIds = [];
    private readonly Dictionary<(Type BaseType, Type BranchType), string> _unplannedPolymorphicReferenceIds = [];
    private readonly Dictionary<object, string> _aliasReferenceIds;
    private readonly Dictionary<string, ReferenceIdOwner> _occupiedReferenceIds;

    private InferredSchemaReferenceIdResolver(
        IReadOnlyDictionary<Type, string?> referenceIds,
        IReadOnlyDictionary<(Type BaseType, Type BranchType), string> polymorphicReferenceIds,
        JsonSerializerOptions serializerOptions,
        Func<Type, InferredSchemaDocument> getInferredSchema,
        Func<JsonTypeInfo, string?> createSchemaReferenceId,
        bool usesDefaultSchemaReferenceId)
    {
        _referenceIds = referenceIds;
        _polymorphicReferenceIds = polymorphicReferenceIds;
        _serializerOptions = serializerOptions;
        _getInferredSchema = getInferredSchema;
        _createSchemaReferenceId = createSchemaReferenceId;
        _usesDefaultSchemaReferenceId = usesDefaultSchemaReferenceId;
        _aliasReferenceIds = referenceIds
            .Where(entry => entry.Value is not null)
            .GroupBy(entry => GetAliasIdentity(entry.Key))
            .ToDictionary(group => group.Key, group => group.First().Value!);
        _occupiedReferenceIds = new(StringComparer.Ordinal);
        foreach (var alias in _aliasReferenceIds)
        {
            _occupiedReferenceIds.Add(alias.Value, new RegularReferenceIdOwner(alias.Key));
        }
        foreach (var polymorphicReferenceId in polymorphicReferenceIds)
        {
            _occupiedReferenceIds.Add(
                polymorphicReferenceId.Value,
                new PolymorphicReferenceIdOwner(polymorphicReferenceId.Key.BaseType, polymorphicReferenceId.Key.BranchType));
        }
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
                serializerOptions,
                getInferredSchema,
                createSchemaReferenceId,
                usesDefaultSchemaReferenceId);
        }

        var resolvedIds = ResolveDefaultReferenceIds(entries);
        return new(
            new ReadOnlyDictionary<Type, string?>(resolvedIds),
            new ReadOnlyDictionary<(Type BaseType, Type BranchType), string>(
                ResolvePolymorphicReferenceIds(polymorphicBranches, resolvedIds, usesDefaultSchemaReferenceId)),
            serializerOptions,
            getInferredSchema,
            createSchemaReferenceId,
            usesDefaultSchemaReferenceId);
    }

    public string? GetReferenceId(JsonTypeInfo typeInfo)
    {
        var type = Nullable.GetUnderlyingType(typeInfo.Type) ?? typeInfo.Type;
        if (_referenceIds.TryGetValue(type, out var referenceId))
        {
            return referenceId;
        }

        lock (_unplannedReferenceIdLock)
        {
            if (_unplannedReferenceIds.TryGetValue(type, out referenceId))
            {
                return referenceId;
            }

            PlanUnplannedGraph(type);
            return _unplannedReferenceIds[type];
        }
    }

    public string? GetPolymorphicReferenceId(Type baseType, Type branchType)
    {
        if (_polymorphicReferenceIds.TryGetValue((baseType, branchType), out var referenceId))
        {
            return referenceId;
        }

        lock (_unplannedReferenceIdLock)
        {
            return _unplannedPolymorphicReferenceIds.GetValueOrDefault((baseType, branchType));
        }
    }

    private void PlanUnplannedGraph(Type rootType)
    {
        var shapes = _getInferredSchema(rootType).Shapes
            .OrderBy(shape => GetCanonicalTypeName(shape.Identity.Type), StringComparer.Ordinal)
            .ToArray();
        var stagedReferenceIds = new Dictionary<Type, string?>();
        var stagedPolymorphicReferenceIds = new Dictionary<(Type BaseType, Type BranchType), string>();
        var aliases = new Dictionary<object, string>(_aliasReferenceIds);
        var occupiedReferenceIds = new Dictionary<string, ReferenceIdOwner>(_occupiedReferenceIds, StringComparer.Ordinal);

        foreach (var shape in shapes)
        {
            var type = shape.Identity.Type;
            if (_referenceIds.ContainsKey(type) || _unplannedReferenceIds.ContainsKey(type))
            {
                continue;
            }

            var aliasIdentity = GetAliasIdentity(type);
            if (aliases.TryGetValue(aliasIdentity, out var aliasReferenceId))
            {
                stagedReferenceIds.Add(type, aliasReferenceId);
                continue;
            }

            var candidate = _createSchemaReferenceId(_serializerOptions.GetTypeInfo(type));
            ValidateReferenceId(type, candidate);
            if (candidate is null)
            {
                stagedReferenceIds.Add(type, null);
                continue;
            }

            var referenceId = _usesDefaultSchemaReferenceId
                ? $"{candidate}-{GetStableHash(type)}"
                : candidate;
            ReserveReferenceId(
                occupiedReferenceIds,
                referenceId,
                new RegularReferenceIdOwner(aliasIdentity));
            stagedReferenceIds.Add(type, referenceId);
            aliases.Add(aliasIdentity, referenceId);
        }

        foreach (var shape in shapes)
        {
            foreach (var derivedType in shape.DerivedTypes)
            {
                var types = (shape.Identity.Type, derivedType.Identity.Type);
                if (_polymorphicReferenceIds.ContainsKey(types) ||
                    _unplannedPolymorphicReferenceIds.ContainsKey(types) ||
                    stagedPolymorphicReferenceIds.ContainsKey(types))
                {
                    continue;
                }

                var baseReferenceId = GetReferenceId(types.Item1, stagedReferenceIds);
                var branchReferenceId = GetReferenceId(types.Item2, stagedReferenceIds);
                if (baseReferenceId is null || branchReferenceId is null)
                {
                    continue;
                }

                var referenceId = _usesDefaultSchemaReferenceId
                    ? $"{baseReferenceId}.{branchReferenceId}-{GetStableHash(types.Item1, types.Item2)}"
                    : $"{baseReferenceId}{branchReferenceId}";
                ReserveReferenceId(
                    occupiedReferenceIds,
                    referenceId,
                    new PolymorphicReferenceIdOwner(types.Item1, types.Item2));
                stagedPolymorphicReferenceIds.Add(types, referenceId);
            }
        }

        foreach (var referenceId in stagedReferenceIds)
        {
            _unplannedReferenceIds.Add(referenceId.Key, referenceId.Value);
        }
        foreach (var referenceId in stagedPolymorphicReferenceIds)
        {
            _unplannedPolymorphicReferenceIds.Add(referenceId.Key, referenceId.Value);
        }
        foreach (var alias in aliases)
        {
            _aliasReferenceIds.TryAdd(alias.Key, alias.Value);
        }
        foreach (var occupiedReferenceId in occupiedReferenceIds)
        {
            _occupiedReferenceIds.TryAdd(occupiedReferenceId.Key, occupiedReferenceId.Value);
        }
    }

    private string? GetReferenceId(Type type, IReadOnlyDictionary<Type, string?> stagedReferenceIds)
    {
        if (_referenceIds.TryGetValue(type, out var referenceId) ||
            _unplannedReferenceIds.TryGetValue(type, out referenceId) ||
            stagedReferenceIds.TryGetValue(type, out referenceId))
        {
            return referenceId;
        }

        throw new InvalidOperationException($"The inferred schema graph for '{type}' is incomplete.");
    }

    private static void ReserveReferenceId(
        Dictionary<string, ReferenceIdOwner> occupiedReferenceIds,
        string referenceId,
        ReferenceIdOwner owner)
    {
        if (occupiedReferenceIds.TryGetValue(referenceId, out var existingOwner))
        {
            if (existingOwner != owner)
            {
                throw new InvalidOperationException(
                    $"The OpenAPI schema reference ID '{referenceId}' is used by distinct serializer contract identities.");
            }
            return;
        }

        occupiedReferenceIds.Add(referenceId, owner);
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
            return $"{definition.Assembly.FullName}:{definition.FullName}[{arguments}]";
        }

        return $"{type.Assembly.FullName}:{type.FullName ?? type.Name}";
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

    private abstract record ReferenceIdOwner;

    private sealed record RegularReferenceIdOwner(object AliasIdentity) : ReferenceIdOwner;

    private sealed record PolymorphicReferenceIdOwner(Type BaseType, Type BranchType) : ReferenceIdOwner;
}
