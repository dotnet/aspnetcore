// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FormatterWebSite.Models;

#nullable enable

// Naturally unambiguous primitive-paired unions (Number vs String, Boolean vs String, etc.)
public union UnionIntString(int, string);
public union UnionByteString(byte, string);
public union UnionShortString(short, string);
public union UnionLongString(long, string);
public union UnionDecimalString(decimal, string);
public union UnionDoubleString(double, string);
public union UnionBoolString(bool, string);
public union UnionGuidInt(Guid, int);
public union UnionDateTimeInt(DateTime, int);
public union UnionCharInt(char, int);

// Nullable-case union: int? vs string.
public union UnionNullableIntString(int?, string);

// Object-case union: both cases serialize as JSON objects, ambiguous on the read path
// (resolved via a classifier in UnionPetWithClassifier below). Serialization is
// unambiguous because dispatch is by runtime .NET type.
public record Cat(string Name);
public record Dog(string Breed);
public union UnionPet(Cat, Dog);

// Nested union: the inner union is itself a case of the outer.
public union UnionInner(int, string);
public union UnionOuter(UnionInner, bool);

// Envelope: union used as a property of another model.
public record UnionEnvelope(string CorrelationId, UnionIntString Payload);

// Ambiguous numeric union (both cases share the Number token).
#pragma warning disable SYSLIB1227
public union UnionIntShort(int, short);
#pragma warning restore SYSLIB1227

// Same shape, but with a trivial classifier that always picks int. Demonstrates that
// MVC honors [JsonUnion] classifiers on the deserialize path.
#pragma warning disable SYSLIB1227
[JsonUnion(TypeClassifier = typeof(IntFirstClassifierFactory))]
public union UnionIntShortWithClassifier(int, short);
#pragma warning restore SYSLIB1227

public sealed class IntFirstClassifierFactory : JsonTypeClassifierFactory<UnionIntShortWithClassifier>
{
    public override JsonTypeClassifier CreateJsonClassifier(JsonTypeClassifierContext context, JsonSerializerOptions options) =>
        static (ref Utf8JsonReader reader) => typeof(int);
}

// Object-case union resolved by property-name dispatch.
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

// UnionIntString paired with a classifier that resolves the String-token ambiguity
// (NumberHandling.AllowReadingFromString makes int eligible for a String payload).
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

// The classifier must return the exact declared case type: typeof(int?), NOT typeof(int).
// Post-fix https://github.com/dotnet/runtime/issues/128688 behavior: the resolver keeps Foo(T) and Foo(Nullable<T>)
// as separate cases keyed by their declared type
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
