// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Components;

// Verifies that C# union types work as component [Parameter] values. Parameters passed from a
// parent to a child component (Razor markup / RenderTreeBuilder.AddComponentParameter) are plain
// in-process CLR object assignment — no JSON serialization is involved — so any union shape works.
public partial class ParameterViewTest
{
    [Fact]
    public void IncomingUnionParameter_UnambiguousIntCase_SetsValue()
    {
        var parameters = new ParameterViewBuilder
        {
            { nameof(HasUnionParameters.IntStringValue), new UnionIntString(42) },
        }.Build();
        var target = new HasUnionParameters();

        parameters.SetParameterProperties(target);

        Assert.Equal(new UnionIntString(42), target.IntStringValue);
    }

    [Fact]
    public void IncomingUnionParameter_UnambiguousStringCase_SetsValue()
    {
        var parameters = new ParameterViewBuilder
        {
            { nameof(HasUnionParameters.IntStringValue), new UnionIntString("hi") },
        }.Build();
        var target = new HasUnionParameters();

        parameters.SetParameterProperties(target);

        Assert.Equal(new UnionIntString("hi"), target.IntStringValue);
    }

    [Fact]
    public void IncomingUnionParameter_NullableNullCase_SetsValue()
    {
        var parameters = new ParameterViewBuilder
        {
            { nameof(HasUnionParameters.NullableValue), new UnionNullableIntString((int?)null) },
        }.Build();
        var target = new HasUnionParameters();

        parameters.SetParameterProperties(target);

        Assert.Equal(new UnionNullableIntString((int?)null), target.NullableValue);
    }

    [Fact]
    public void IncomingUnionParameter_ReferenceTypeCase_SetsSameInstance()
    {
        var cat = new Cat("Whiskers");
        var parameters = new ParameterViewBuilder
        {
            { nameof(HasUnionParameters.Pet), new UnionPet(cat) },
        }.Build();
        var target = new HasUnionParameters();

        parameters.SetParameterProperties(target);

        Assert.Equal(new UnionPet(cat), target.Pet);
        Assert.Same(cat, target.Pet.Value);
    }

    [Fact]
    public void IncomingUnionParameter_FromDictionary_SetsValue()
    {
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object>
        {
            [nameof(HasUnionParameters.IntStringValue)] = new UnionIntString(7),
        });
        var target = new HasUnionParameters();

        parameters.SetParameterProperties(target);

        Assert.Equal(new UnionIntString(7), target.IntStringValue);
    }

    private sealed class HasUnionParameters
    {
        [Parameter] public UnionIntString IntStringValue { get; set; }
        [Parameter] public UnionNullableIntString NullableValue { get; set; }
        [Parameter] public UnionPet Pet { get; set; }
    }
}

// --- Test union types (shared across the Components.Tests union tests) ---

// Unambiguous primitive-paired union.
public union UnionIntString(int, string);

// Nullable value-type case.
public union UnionNullableIntString(int?, string);

// Reference-type cases.
public record Cat(string Name);
public record Dog(string Breed);
public union UnionPet(Cat, Dog);

// Classifier-disambiguated variant of UnionPet, used by the persisted-state round-trip test where
// the read side needs to disambiguate two object cases. Null is handled by the runtime fast-path,
// so the classifier does not branch on JsonTokenType.Null.
[JsonUnion(TypeClassifier = typeof(UnionPetClassifierFactory))]
public union UnionPetWithClassifier(Cat, Dog);

public sealed class UnionPetClassifierFactory : JsonTypeClassifierFactory<UnionPetWithClassifier>
{
    public override JsonTypeClassifier CreateJsonClassifier(JsonTypeClassifierContext context, System.Text.Json.JsonSerializerOptions options) =>
        static (ref System.Text.Json.Utf8JsonReader reader) =>
        {
            if (reader.TokenType != System.Text.Json.JsonTokenType.StartObject)
            {
                return null;
            }

            var clone = reader;
            clone.Read();
            while (clone.TokenType == System.Text.Json.JsonTokenType.PropertyName)
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

// Envelope record that holds a union as a property.
public record UnionEnvelope(string CorrelationId, UnionIntString Payload);
