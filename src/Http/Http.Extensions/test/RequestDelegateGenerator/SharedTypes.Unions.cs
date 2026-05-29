// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Http;

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

// Trivial classifier: always pick int. (Serialize tests don't exercise this path.)
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
