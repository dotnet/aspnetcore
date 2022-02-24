// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.JsonPatch.Exceptions;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNetCore.JsonPatch;

public class JsonPatchDocumentTest
{
    [Fact]
    public void InvalidPathAtBeginningShouldThrowException()
    {
        // Arrange
        var patchDocument = new JsonPatchDocument();

        // Act
        var exception = Assert.Throws<JsonPatchException>(() =>
        {
            patchDocument.Add("//NewInt", 1);
        });

        // Assert
        Assert.Equal(
           "The provided string '//NewInt' is an invalid path.",
            exception.Message);
    }

    [Fact]
    public void InvalidPathAtEndShouldThrowException()
    {
        // Arrange
        var patchDocument = new JsonPatchDocument();

        // Act
        var exception = Assert.Throws<JsonPatchException>(() =>
        {
            patchDocument.Add("NewInt//", 1);
        });

        // Assert
        Assert.Equal(
           "The provided string 'NewInt//' is an invalid path.",
            exception.Message);
    }

    [Fact]
    public void NonGenericPatchDocToGenericMustSerialize()
    {
        // Arrange
        var targetObject = new SimpleObject()
        {
            StringProperty = "A",
            AnotherStringProperty = "B"
        };

        var patchDocument = new JsonPatchDocument();
        patchDocument.Copy("StringProperty", "AnotherStringProperty");

        var serialized = JsonConvert.SerializeObject(patchDocument);
        var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleObject>>(serialized);

        // Act
        deserialized.ApplyTo(targetObject);

        // Assert
        Assert.Equal("A", targetObject.AnotherStringProperty);
    }

    [Fact]
    public void GenericPatchDocToNonGenericMustSerialize()
    {
        // Arrange
        var targetObject = new SimpleObject()
        {
            StringProperty = "A",
            AnotherStringProperty = "B"
        };

        var patchDocTyped = new JsonPatchDocument<SimpleObject>();
        patchDocTyped.Copy(o => o.StringProperty, o => o.AnotherStringProperty);

        var patchDocUntyped = new JsonPatchDocument();
        patchDocUntyped.Copy("StringProperty", "AnotherStringProperty");

        var serializedTyped = JsonConvert.SerializeObject(patchDocTyped);
        var serializedUntyped = JsonConvert.SerializeObject(patchDocUntyped);
        var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serializedTyped);

        // Act
        deserialized.ApplyTo(targetObject);

        // Assert
        Assert.Equal("A", targetObject.AnotherStringProperty);
    }

    [Fact]
    public void Deserialization_Successful_ForValidJsonPatchDocument()
    {
        // Arrange
        var doc = new SimpleObject()
        {
            StringProperty = "A",
            DecimalValue = 10,
            DoubleValue = 10,
            FloatValue = 10,
            IntegerValue = 10
        };

        var patchDocument = new JsonPatchDocument<SimpleObject>();
        patchDocument.Replace(o => o.StringProperty, "B");
        patchDocument.Replace(o => o.DecimalValue, 12);
        patchDocument.Replace(o => o.DoubleValue, 12);
        patchDocument.Replace(o => o.FloatValue, 12);
        patchDocument.Replace(o => o.IntegerValue, 12);

        // default: no envelope
        var serialized = JsonConvert.SerializeObject(patchDocument);

        // Act
        var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleObject>>(serialized);

        // Assert
        Assert.IsType<JsonPatchDocument<SimpleObject>>(deserialized);
    }

    [Fact]
    public void Deserialization_Fails_ForInvalidJsonPatchDocument()
    {
        // Arrange
        var serialized = "{\"Operations\": [{ \"op\": \"replace\", \"path\": \"/title\", \"value\": \"New Title\"}]}";

        // Act
        var exception = Assert.Throws<JsonSerializationException>(() =>
        {
            var deserialized
                = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);
        });

        // Assert
        Assert.Equal("The JSON patch document was malformed and could not be parsed.", exception.Message);
    }

    [Fact]
    public void Deserialization_Fails_ForInvalidTypedJsonPatchDocument()
    {
        // Arrange
        var serialized = "{\"Operations\": [{ \"op\": \"replace\", \"path\": \"/title\", \"value\": \"New Title\"}]}";

        // Act
        var exception = Assert.Throws<JsonSerializationException>(() =>
        {
            var deserialized
                = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleObject>>(serialized);
        });

        // Assert
        Assert.Equal("The JSON patch document was malformed and could not be parsed.", exception.Message);
    }
}
