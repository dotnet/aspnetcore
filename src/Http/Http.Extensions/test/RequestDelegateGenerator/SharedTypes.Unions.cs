// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.AspNetCore.Http.Generators.Tests;

#nullable enable

// By convention every union type in this file starts with "Union".

// Simple unambiguous primitive-paired unions
public union UnionIntString(int, string);
public union UnionByteString(byte, string);
public union UnionShortString(short, string);
public union UnionLongString(long, string);
public union UnionDecimalString(decimal, string);
public union UnionDoubleString(double, string);
public union UnionBoolString(bool, string);
public union UnionGuidInt(Guid, int);          // String + Number
public union UnionDateTimeInt(DateTime, int);  // String + Number
public union UnionCharInt(char, int);          // String + Number (char serializes as JSON string)

// Nullable case
public union UnionNullableIntString(int?, string);

// Object-case union. Both cases share the JSON Object value kind, which makes UnionPet
// implicitly ambiguous on the deserialize side (resolved here via property-name dispatch
// when the user supplies a classifier). For serialization the deconstructor dispatches by
// .NET runtime type, so no classifier is needed.
public record Cat(string Name);
public record Dog(string Breed);
public union UnionPet(Cat, Dog);

// Derived type that is NOT a declared case of UnionPet — used to verify STJ resolves to
// the nearest declared ancestor (Dog) when handed a SausageDog.
public record SausageDog(string Breed, double Length) : Dog(Breed);

// Nested-union scenarios.
public union UnionInner(int, string);
public union UnionOuter(UnionInner, bool); // union case is itself a union

// Ambiguous unions 
#pragma warning disable SYSLIB1227
public union UnionIntShort(int, short);            // both → Number
public union UnionDateTimeString(DateTime, string); // both → String
public union UnionPolyCatDog(PolyCat, PolyDog);    // both → Object, declared as the concrete derived types
#pragma warning restore SYSLIB1227

// Envelope: union used as a property of another model.
public record UnionEnvelope(string CorrelationId, UnionIntString Payload);

// Polymorphism on a union case type: PolyAnimal is a JSON-polymorphic base with two
// derived types. When used as a union case, returning a derived instance should emit
// the "$type" discriminator from the polymorphic contract.
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(PolyCat), "cat")]
[JsonDerivedType(typeof(PolyDog), "dog")]
public record PolyAnimal();
public record PolyCat(string Name) : PolyAnimal();
public record PolyDog(string Breed) : PolyAnimal();
public union UnionAnimalString(PolyAnimal, string);

// Ambiguous numeric union with a user-supplied classifier. The classifier only affects
// deserialization; serialization should be identical to UnionIntShort. Verifies the
// JsonUnionAttribute is wired through the metadata pipeline without breaking writes.
#pragma warning disable SYSLIB1227
[JsonUnion(TypeClassifier = typeof(IntFirstClassifierFactory))]
public union UnionIntShortWithClassifier(int, short);
#pragma warning restore SYSLIB1227

// Trivial classifier: always pick int
public sealed class IntFirstClassifierFactory : JsonTypeClassifierFactory<UnionIntShortWithClassifier>
{
    public override JsonTypeClassifier CreateJsonClassifier(JsonTypeClassifierContext context, JsonSerializerOptions options) =>
        static (ref Utf8JsonReader reader) => typeof(int);
}

// Primitive-case unions paired with a classifier that disambiguates the cases

[JsonUnion(TypeClassifier = typeof(UnionByteStringClassifierFactory))]
public union UnionByteStringWithClassifier(byte, string);
public sealed class UnionByteStringClassifierFactory : JsonTypeClassifierFactory<UnionByteStringWithClassifier>
{
    public override JsonTypeClassifier CreateJsonClassifier(JsonTypeClassifierContext context, JsonSerializerOptions options) =>
        static (ref Utf8JsonReader reader) => reader.TokenType switch
        {
            JsonTokenType.Number => typeof(byte),
            JsonTokenType.String => typeof(string),
            _ => null,
        };
}

[JsonUnion(TypeClassifier = typeof(UnionShortStringClassifierFactory))]
public union UnionShortStringWithClassifier(short, string);
public sealed class UnionShortStringClassifierFactory : JsonTypeClassifierFactory<UnionShortStringWithClassifier>
{
    public override JsonTypeClassifier CreateJsonClassifier(JsonTypeClassifierContext context, JsonSerializerOptions options) =>
        static (ref Utf8JsonReader reader) => reader.TokenType switch
        {
            JsonTokenType.Number => typeof(short),
            JsonTokenType.String => typeof(string),
            _ => null,
        };
}

[JsonUnion(TypeClassifier = typeof(UnionIntStringClassifierFactory))]
public union UnionIntStringWithClassifier(int, string);
public sealed class UnionIntStringClassifierFactory : JsonTypeClassifierFactory<UnionIntStringWithClassifier>
{
    public override JsonTypeClassifier CreateJsonClassifier(JsonTypeClassifierContext context, JsonSerializerOptions options) =>
        static (ref Utf8JsonReader reader) => reader.TokenType switch
        {
            JsonTokenType.Number => typeof(int),
            JsonTokenType.String => typeof(string),
            _ => null,
        };
}

[JsonUnion(TypeClassifier = typeof(UnionLongStringClassifierFactory))]
public union UnionLongStringWithClassifier(long, string);
public sealed class UnionLongStringClassifierFactory : JsonTypeClassifierFactory<UnionLongStringWithClassifier>
{
    public override JsonTypeClassifier CreateJsonClassifier(JsonTypeClassifierContext context, JsonSerializerOptions options) =>
        static (ref Utf8JsonReader reader) => reader.TokenType switch
        {
            JsonTokenType.Number => typeof(long),
            JsonTokenType.String => typeof(string),
            _ => null,
        };
}

[JsonUnion(TypeClassifier = typeof(UnionDecimalStringClassifierFactory))]
public union UnionDecimalStringWithClassifier(decimal, string);
public sealed class UnionDecimalStringClassifierFactory : JsonTypeClassifierFactory<UnionDecimalStringWithClassifier>
{
    public override JsonTypeClassifier CreateJsonClassifier(JsonTypeClassifierContext context, JsonSerializerOptions options) =>
        static (ref Utf8JsonReader reader) => reader.TokenType switch
        {
            JsonTokenType.Number => typeof(decimal),
            JsonTokenType.String => typeof(string),
            _ => null,
        };
}

[JsonUnion(TypeClassifier = typeof(UnionDoubleStringClassifierFactory))]
public union UnionDoubleStringWithClassifier(double, string);
public sealed class UnionDoubleStringClassifierFactory : JsonTypeClassifierFactory<UnionDoubleStringWithClassifier>
{
    public override JsonTypeClassifier CreateJsonClassifier(JsonTypeClassifierContext context, JsonSerializerOptions options) =>
        static (ref Utf8JsonReader reader) => reader.TokenType switch
        {
            JsonTokenType.Number => typeof(double),
            JsonTokenType.String => typeof(string),
            _ => null,
        };
}

[JsonUnion(TypeClassifier = typeof(UnionGuidIntClassifierFactory))]
public union UnionGuidIntWithClassifier(Guid, int);
public sealed class UnionGuidIntClassifierFactory : JsonTypeClassifierFactory<UnionGuidIntWithClassifier>
{
    public override JsonTypeClassifier CreateJsonClassifier(JsonTypeClassifierContext context, JsonSerializerOptions options) =>
        static (ref Utf8JsonReader reader) => reader.TokenType switch
        {
            JsonTokenType.Number => typeof(int),
            JsonTokenType.String => typeof(Guid),
            _ => null,
        };
}

[JsonUnion(TypeClassifier = typeof(UnionDateTimeIntClassifierFactory))]
public union UnionDateTimeIntWithClassifier(DateTime, int);
public sealed class UnionDateTimeIntClassifierFactory : JsonTypeClassifierFactory<UnionDateTimeIntWithClassifier>
{
    public override JsonTypeClassifier CreateJsonClassifier(JsonTypeClassifierContext context, JsonSerializerOptions options) =>
        static (ref Utf8JsonReader reader) => reader.TokenType switch
        {
            JsonTokenType.Number => typeof(int),
            JsonTokenType.String => typeof(DateTime),
            _ => null,
        };
}

[JsonUnion(TypeClassifier = typeof(UnionCharIntClassifierFactory))]
public union UnionCharIntWithClassifier(char, int);
public sealed class UnionCharIntClassifierFactory : JsonTypeClassifierFactory<UnionCharIntWithClassifier>
{
    public override JsonTypeClassifier CreateJsonClassifier(JsonTypeClassifierContext context, JsonSerializerOptions options) =>
        static (ref Utf8JsonReader reader) => reader.TokenType switch
        {
            JsonTokenType.Number => typeof(int),
            JsonTokenType.String => typeof(char),
            _ => null,
        };
}

// Nullable-case union with classifier. The String token is ambiguous, so the classifier is needed to route String → string.
// Number unambiguously maps to int?. Null is handled by the runtime's value-based fast-path — the classifier intentionally does not handle Null.
[JsonUnion(TypeClassifier = typeof(UnionNullableIntStringClassifierFactory))]
public union UnionNullableIntStringWithClassifier(int?, string);
public sealed class UnionNullableIntStringClassifierFactory : JsonTypeClassifierFactory<UnionNullableIntStringWithClassifier>
{
    public override JsonTypeClassifier CreateJsonClassifier(JsonTypeClassifierContext context, JsonSerializerOptions options) =>
        static (ref Utf8JsonReader reader) => reader.TokenType switch
        {
            JsonTokenType.Number => typeof(int?),
            JsonTokenType.String => typeof(string),
            _ => null,
        };
}

// Object-case union resolved by property-name dispatch. Cat has "name", Dog has "breed".
// The classifier clones the reader, walks the first object members until it finds a property
// that identifies the case, and returns the matching type. Property comparison is case-insensitive
// across the standard policies (PascalCase declaration + camelCase from web defaults).
[JsonUnion(TypeClassifier = typeof(UnionPetClassifierFactory))]
public union UnionPetWithClassifier(Cat, Dog);
public sealed class UnionPetClassifierFactory : JsonTypeClassifierFactory<UnionPetWithClassifier>
{
    public override JsonTypeClassifier CreateJsonClassifier(JsonTypeClassifierContext context, JsonSerializerOptions options) =>
        static (ref Utf8JsonReader reader) =>
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                return null;
            }

            var clone = reader;
            clone.Read();
            while (clone.TokenType == JsonTokenType.PropertyName)
            {
                if (clone.ValueTextEquals("name") || clone.ValueTextEquals("Name"))
                {
                    return typeof(Cat);
                }
                if (clone.ValueTextEquals("breed") || clone.ValueTextEquals("Breed"))
                {
                    return typeof(Dog);
                }

                clone.Read();
                clone.Skip();
                clone.Read();
            }

            return null;
        };
}

// Inner union with a classifier — used as a case type of a nested outer union below.
[JsonUnion(TypeClassifier = typeof(UnionInnerWithClassifierFactory))]
public union UnionInnerWithClassifier(int, string);
public sealed class UnionInnerWithClassifierFactory : JsonTypeClassifierFactory<UnionInnerWithClassifier>
{
    public override JsonTypeClassifier CreateJsonClassifier(JsonTypeClassifierContext context, JsonSerializerOptions options) =>
        static (ref Utf8JsonReader reader) => reader.TokenType switch
        {
            JsonTokenType.Number => typeof(int),
            JsonTokenType.String => typeof(string),
            _ => null,
        };
}

// Outer nested union with a classifier'd inner union case. The outer pair (UnionInnerWithClassifier, bool)
// is token-distinct (Boolean vs {Number, String}), so the outer itself needs no classifier.
public union UnionOuterWithClassifier(UnionInnerWithClassifier, bool);

// Outer nested union with classifiers on BOTH the outer and the inner. The outer classifier maps
// non-Boolean tokens to the inner union type so STJ recurses into UnionInnerWithClassifier, which
// then resolves the Number/String ambiguity. Demonstrates that nested unions are reachable end-to-end
// when callers opt in to classifiers at every level.
[JsonUnion(TypeClassifier = typeof(UnionOuterWithBothClassifiersFactory))]
public union UnionOuterWithBothClassifiers(UnionInnerWithClassifier, bool);
public sealed class UnionOuterWithBothClassifiersFactory : JsonTypeClassifierFactory<UnionOuterWithBothClassifiers>
{
    public override JsonTypeClassifier CreateJsonClassifier(JsonTypeClassifierContext context, JsonSerializerOptions options) =>
        static (ref Utf8JsonReader reader) => reader.TokenType switch
        {
            JsonTokenType.True or JsonTokenType.False => typeof(bool),
            JsonTokenType.Number or JsonTokenType.String => typeof(UnionInnerWithClassifier),
            _ => null,
        };
}

// Envelope that wraps a classifier'd inner union as one of its properties. The record itself is
// unambiguous; the classifier resolves the inner-union ambiguity that surfaces during binding.
public record UnionEnvelopeWithClassifier(string CorrelationId, UnionIntStringWithClassifier Payload);

// Same as UnionEnvelopeWithClassifier but the union property is marked [JsonRequired].
// Used to verify STJ rejects a missing "payload" key on read, instead of silently producing
// a default(union) value that would later trip the union converter on the write path.
public record UnionEnvelopeWithRequiredPayload(
    string CorrelationId,
    [property: JsonRequired] UnionIntStringWithClassifier Payload);

// Container for [AsParameters] tests where a union property is the body slot and
// a sibling property comes from the route. Verifies that the generator unwraps the
// container and applies the standard body-inference cascade to the union property
// while the non-union property is routed/queried independently.
public record UnionAsParametersList(HttpContext HttpContext, [FromRoute] int TenantId, UnionIntString Payload);

// Same shape as UnionAsParametersList but the union has a JsonUnion classifier wired
// up. Used to verify both case types (int and string) bind through the body inside an
// [AsParameters] container — the bare UnionIntString variant is ambiguous on the String
// token under web defaults, so we need a classifier to cover the string-case path.
public record UnionWithClassifierAsParametersList(HttpContext HttpContext, [FromRoute] int TenantId, UnionIntStringWithClassifier Payload);
