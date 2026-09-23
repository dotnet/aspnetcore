// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Microsoft.AspNetCore.OpenApi;

internal enum InferredSchemaShapeKind
{
    Scalar,
    Object,
    Collection,
    Dictionary,
    Union,
    Tuple,
}

internal enum InferredSchemaPurpose
{
    Neutral,
    Input,
    Output,
}

internal readonly record struct InferredSchemaRoot(Type Type, InferredSchemaPurpose Purpose);

internal readonly record struct InferredSchemaTypeIdentity(Type Type)
{
    public string Name { get; } = Type.FullName ?? Type.Name;
}

internal readonly record struct InferredSchemaTypeUse(InferredSchemaTypeIdentity Identity, bool AllowsNull);

internal readonly record struct InferredSchemaPropertyIdentity(
    InferredSchemaTypeIdentity DeclaringType,
    string MemberName,
    string JsonName);

internal sealed record InferredSchemaProperty(
    InferredSchemaPropertyIdentity Identity,
    Type DeclaredPropertyType,
    InferredSchemaTypeUse PropertyType,
    bool IsRequired,
    bool IsExtensionData);

internal sealed record InferredSchemaDerivedType(
    InferredSchemaTypeIdentity Identity,
    object? Discriminator);

internal sealed class InferredSchemaShape
{
    private readonly IReadOnlyDictionary<string, InferredSchemaProperty> _propertiesByJsonName;

    public InferredSchemaShape(
        InferredSchemaTypeIdentity identity,
        InferredSchemaShapeKind kind,
        Type converterType,
        bool hasCustomConverter,
        JsonNumberHandling numberHandling,
        InferredJsonFiniteDomainFact? finiteDomain,
        InferredScalarContractFact scalarContract,
        bool disallowsUnmappedMembers,
        string? discriminatorPropertyName,
        InferredSchemaTypeIdentity? baseType,
        InferredSchemaTypeUse? elementType,
        InferredSchemaTypeUse? additionalPropertiesType,
        IReadOnlyList<InferredSchemaProperty> properties,
        InferredSchemaProperty? extensionDataProperty,
        IReadOnlyList<InferredSchemaDerivedType> derivedTypes,
        IReadOnlyList<InferredSchemaTypeUse> unionCases,
        IReadOnlyList<InferredSchemaTypeUse> tupleElements)
    {
        Identity = identity;
        Kind = kind;
        ConverterType = converterType;
        HasCustomConverter = hasCustomConverter;
        NumberHandling = numberHandling;
        FiniteDomain = finiteDomain;
        ScalarContract = scalarContract;
        DisallowsUnmappedMembers = disallowsUnmappedMembers;
        DiscriminatorPropertyName = discriminatorPropertyName;
        BaseType = baseType;
        ElementType = elementType;
        AdditionalPropertiesType = additionalPropertiesType;
        Properties = properties;
        ExtensionDataProperty = extensionDataProperty;
        DerivedTypes = derivedTypes;
        UnionCases = unionCases;
        TupleElements = tupleElements;
        _propertiesByJsonName = new ReadOnlyDictionary<string, InferredSchemaProperty>(
            properties.ToDictionary(property => property.Identity.JsonName, StringComparer.Ordinal));
    }

    public InferredSchemaTypeIdentity Identity { get; }

    public InferredSchemaShapeKind Kind { get; }

    public Type ConverterType { get; }

    public bool HasCustomConverter { get; }

    public JsonNumberHandling NumberHandling { get; }

    public InferredJsonFiniteDomainFact? FiniteDomain { get; }

    public InferredScalarContractFact ScalarContract { get; }

    public bool DisallowsUnmappedMembers { get; }

    public string? DiscriminatorPropertyName { get; }

    public InferredSchemaTypeIdentity? BaseType { get; }

    public InferredSchemaTypeUse? ElementType { get; }

    public InferredSchemaTypeUse? AdditionalPropertiesType { get; }

    public IReadOnlyList<InferredSchemaProperty> Properties { get; }

    public InferredSchemaProperty? ExtensionDataProperty { get; }

    public IReadOnlyList<InferredSchemaDerivedType> DerivedTypes { get; }

    public IReadOnlyList<InferredSchemaTypeUse> UnionCases { get; }

    public IReadOnlyList<InferredSchemaTypeUse> TupleElements { get; }

    public InferredSchemaProperty GetProperty(string jsonName) => _propertiesByJsonName[jsonName];
}

internal sealed class InferredSchemaDocument
{
    private readonly IReadOnlyDictionary<Type, InferredSchemaShape> _shapes;

    public InferredSchemaDocument(
        InferredSchemaTypeUse root,
        InferredSchemaPurpose purpose,
        IReadOnlyList<InferredSchemaShape> shapes)
    {
        Root = root;
        Purpose = purpose;
        Shapes = shapes;
        _shapes = new ReadOnlyDictionary<Type, InferredSchemaShape>(
            shapes.ToDictionary(shape => shape.Identity.Type));
        CompositionDecisions = InferredSchemaCompositionDecisionBuilder.Build(this);
    }

    public InferredSchemaTypeUse Root { get; }

    public InferredSchemaPurpose Purpose { get; }

    public IReadOnlyList<InferredSchemaShape> Shapes { get; }

    public InferredSchemaCompositionDecisions CompositionDecisions { get; }

    public InferredSchemaShape this[Type type] => _shapes[GetUnderlyingType(type)];

    public bool TryGetShape(Type type, out InferredSchemaShape shape)
        => _shapes.TryGetValue(GetUnderlyingType(type), out shape!);

    private static Type GetUnderlyingType(Type type) => Nullable.GetUnderlyingType(type) ?? type;
}

internal static class InferredSchemaShapeBuilder
{
    public static InferredSchemaDocument Build(
        JsonSerializerOptions serializerOptions,
        Type rootType,
        InferredSchemaPurpose purpose = InferredSchemaPurpose.Neutral)
    {
        var root = CreateTypeUse(rootType);
        var pending = new Queue<Type>();
        var discovered = new HashSet<Type>();
        var shapes = new List<InferredSchemaShape>();
        AddType(root.Identity.Type);

        while (pending.TryDequeue(out var type))
        {
            var typeInfo = serializerOptions.GetTypeInfo(type);
            var converterType = typeInfo.Converter.GetType();
            var hasCustomConverter = converterType.Assembly != typeof(JsonSerializerOptions).Assembly;
            var (properties, extensionDataProperty) = CreateProperties(typeInfo, purpose);
            var derivedTypes = CreateDerivedTypes(typeInfo);
            var unionCases = CreateUnionCases(typeInfo);
            var tupleElements = CreateTupleElements(typeInfo);
            InferredSchemaTypeUse? elementType = typeInfo.ElementType is { } element ? CreateTypeUse(element) : null;
            var additionalPropertiesType = typeInfo.Kind == JsonTypeInfoKind.Dictionary
                ? elementType
                : GetExtensionDataType(serializerOptions, extensionDataProperty);
            InferredSchemaTypeIdentity? baseType = type.BaseType is { } candidate && candidate != typeof(object) && candidate != typeof(ValueType)
                ? new InferredSchemaTypeIdentity(candidate)
                : null;

            shapes.Add(new(
                new(type),
                GetShapeKind(typeInfo),
                converterType,
                hasCustomConverter,
                typeInfo.NumberHandling ?? serializerOptions.NumberHandling,
                InferredJsonFiniteDomainBuilder.Build(typeInfo, hasCustomConverter),
                InferredScalarContractFactBuilder.Build(typeInfo),
                typeInfo.UnmappedMemberHandling == JsonUnmappedMemberHandling.Disallow,
                typeInfo.PolymorphismOptions?.TypeDiscriminatorPropertyName,
                baseType,
                elementType,
                additionalPropertiesType,
                properties,
                extensionDataProperty,
                derivedTypes,
                unionCases,
                tupleElements));

            AddType(baseType?.Type);
            AddType(elementType?.Identity.Type);
            AddType(additionalPropertiesType?.Identity.Type);
            foreach (var property in properties)
            {
                AddType(property.PropertyType.Identity.Type);
            }
            foreach (var derivedType in derivedTypes)
            {
                AddType(derivedType.Identity.Type);
            }
            foreach (var unionCase in unionCases)
            {
                AddType(unionCase.Identity.Type);
            }
            foreach (var tupleElement in tupleElements)
            {
                AddType(tupleElement.Identity.Type);
            }
        }

        shapes.Sort(static (left, right) => StringComparer.Ordinal.Compare(left.Identity.Name, right.Identity.Name));
        return new(root, purpose, new ReadOnlyCollection<InferredSchemaShape>(shapes));

        void AddType(Type? type)
        {
            if (type is not null && discovered.Add(type))
            {
                pending.Enqueue(type);
            }
        }
    }

    private static (IReadOnlyList<InferredSchemaProperty> Properties, InferredSchemaProperty? ExtensionDataProperty) CreateProperties(
        JsonTypeInfo typeInfo,
        InferredSchemaPurpose purpose)
    {
        var properties = new List<InferredSchemaProperty>(typeInfo.Properties.Count);
        InferredSchemaProperty? extensionDataProperty = null;
        foreach (var property in typeInfo.Properties)
        {
            if (property is { Get: null, Set: null })
            {
                continue;
            }

            var memberName = property.AttributeProvider is MemberInfo memberInfo ? memberInfo.Name : property.Name;
            var inferredProperty = new InferredSchemaProperty(
                new(new(property.DeclaringType), memberName, property.Name),
                property.PropertyType,
                new(
                    new(Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType),
                    AllowsNull(property, purpose)),
                purpose == InferredSchemaPurpose.Output ? false : property.IsRequired,
                property.IsExtensionData);

            if (property.IsExtensionData)
            {
                extensionDataProperty = inferredProperty;
                continue;
            }

            if (IsIncluded(property, purpose))
            {
                properties.Add(inferredProperty);
            }
        }

        properties.Sort(static (left, right) =>
        {
            var result = StringComparer.Ordinal.Compare(left.Identity.JsonName, right.Identity.JsonName);
            return result != 0
                ? result
                : StringComparer.Ordinal.Compare(left.Identity.MemberName, right.Identity.MemberName);
        });
        return (new ReadOnlyCollection<InferredSchemaProperty>(properties), extensionDataProperty);

        static bool IsIncluded(JsonPropertyInfo property, InferredSchemaPurpose purpose)
            => purpose switch
            {
                InferredSchemaPurpose.Input => property.Set is not null ||
                    property.AssociatedParameter is not null ||
                    property.ObjectCreationHandling == JsonObjectCreationHandling.Populate,
                InferredSchemaPurpose.Output => property.Get is not null,
                _ => property.Get is not null || property.Set is not null,
            };

        static bool AllowsNull(JsonPropertyInfo property, InferredSchemaPurpose purpose)
            => purpose switch
            {
                InferredSchemaPurpose.Input when property.AssociatedParameter is { } parameter => parameter.IsNullable,
                InferredSchemaPurpose.Input => property.IsSetNullable,
                InferredSchemaPurpose.Output => property.IsGetNullable,
                _ => property.IsGetNullable || property.IsSetNullable,
            };
    }

    private static IReadOnlyList<InferredSchemaDerivedType> CreateDerivedTypes(JsonTypeInfo typeInfo)
    {
        if (typeInfo.PolymorphismOptions is not { } polymorphismOptions)
        {
            return Array.Empty<InferredSchemaDerivedType>();
        }

        var derivedTypes = polymorphismOptions.DerivedTypes
            .Select(derivedType => new InferredSchemaDerivedType(new(derivedType.DerivedType), derivedType.TypeDiscriminator))
            .ToArray();
        return Array.AsReadOnly(derivedTypes);
    }

    private static InferredSchemaTypeUse? GetExtensionDataType(
        JsonSerializerOptions serializerOptions,
        InferredSchemaProperty? extensionDataProperty)
    {
        if (extensionDataProperty is null)
        {
            return null;
        }

        var extensionDataTypeInfo = serializerOptions.GetTypeInfo(extensionDataProperty.DeclaredPropertyType);
        return extensionDataTypeInfo.ElementType is { } elementType ? CreateTypeUse(elementType) : null;
    }

    private static IReadOnlyList<InferredSchemaTypeUse> CreateUnionCases(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Union)
        {
            return Array.Empty<InferredSchemaTypeUse>();
        }

        var unionCases = typeInfo.UnionCases
            .Select(unionCase => new InferredSchemaTypeUse(
                new(Nullable.GetUnderlyingType(unionCase.CaseType) ?? unionCase.CaseType),
                unionCase.IsNullable))
            .ToArray();
        return Array.AsReadOnly(unionCases);
    }

    private static IReadOnlyList<InferredSchemaTypeUse> CreateTupleElements(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Converter is not IJsonArrayTupleConverter tupleConverter)
        {
            return Array.Empty<InferredSchemaTypeUse>();
        }

        return Array.AsReadOnly(
            tupleConverter.Contract.ElementTypes
                .Select(CreateTypeUse)
                .ToArray());
    }

    private static InferredSchemaShapeKind GetShapeKind(JsonTypeInfo typeInfo) => typeInfo.Kind switch
    {
        _ when typeInfo.Converter is IJsonArrayTupleConverter => InferredSchemaShapeKind.Tuple,
        JsonTypeInfoKind.Object => InferredSchemaShapeKind.Object,
        JsonTypeInfoKind.Enumerable => InferredSchemaShapeKind.Collection,
        JsonTypeInfoKind.Dictionary => InferredSchemaShapeKind.Dictionary,
        JsonTypeInfoKind.Union => InferredSchemaShapeKind.Union,
        _ => InferredSchemaShapeKind.Scalar,
    };

    private static InferredSchemaTypeUse CreateTypeUse(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type);
        return new(new(underlyingType ?? type), underlyingType is not null);
    }
}
