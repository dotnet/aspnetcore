// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.JsonPatch.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.AspNetCore.JsonPatch;

public class JsonPatchDocumentTest
{
    [Fact]
    public void DoubleSlashAtBeginningShouldParseCorrectly()
    {
        // Arrange
        var patchDocument = new JsonPatchDocument();
        var targetObject = new JObject { [""] = new JObject() };

        // Act
        patchDocument.Add("//NewInt", 1);
        patchDocument.ApplyTo(targetObject);

        // Assert
        var operation = Assert.Single(patchDocument.Operations);
        Assert.Equal("add", operation.op);
        Assert.Equal("//NewInt", operation.path);
        Assert.Equal(1, operation.value);
        Assert.Equal(1, targetObject[""]["NewInt"].Value<int>());
    }

    [Fact]
    public void DoubleSlashAtEndShouldParseCorrectly()
    {
        // Arrange
        var patchDocument = new JsonPatchDocument();
        var targetObject = new JObject { ["NewInt"] = new JObject { [""] = new JObject() } };

        // Act
        patchDocument.Add("NewInt//", 1);
        patchDocument.ApplyTo(targetObject);

        // Assert
        var operation = Assert.Single(patchDocument.Operations);
        Assert.Equal("add", operation.op);
        Assert.Equal("/NewInt//", operation.path);
        Assert.Equal(1, operation.value);
        Assert.Equal(1, targetObject["NewInt"][""][""].Value<int>());
    }

    [Fact]
    public void SingleSlashShouldReferToEmptyStringKey()
    {
        // Arrange
        // Per RFC 6901, "/" references the member with the empty string ("") as its key,
        // which is distinct from an empty path that references the whole document.
        var patchDocument = new JsonPatchDocument();
        var targetObject = new JObject { [""] = 0 };

        // Act
        patchDocument.Replace("/", 1);
        patchDocument.ApplyTo(targetObject);

        // Assert
        var operation = Assert.Single(patchDocument.Operations);
        Assert.Equal("replace", operation.op);
        Assert.Equal("/", operation.path);
        Assert.Equal(1, operation.value);
        Assert.Equal(1, targetObject[""].Value<int>());
    }

    [Fact]
    public void SingleSlashShouldAddEmptyStringKey()
    {
        // Arrange
        var patchDocument = new JsonPatchDocument();
        var targetObject = new JObject();

        // Act
        patchDocument.Add("/", 1);
        patchDocument.ApplyTo(targetObject);

        // Assert
        var operation = Assert.Single(patchDocument.Operations);
        Assert.Equal("add", operation.op);
        Assert.Equal("/", operation.path);
        Assert.Equal(1, operation.value);
        Assert.Equal(1, targetObject[""].Value<int>());
    }

    [Fact]
    public void EmptyStringPathIsPreservedAndDistinctFromSingleSlash()
    {
        // Arrange
        // Per RFC 6901, an empty string references the whole document and must never be
        // normalized to "/", which references the member with the empty string key.
        var patchDocument = new JsonPatchDocument();

        // Act
        patchDocument.Add("", 1);
        patchDocument.Add("/", 2);

        // Assert
        Assert.Collection(patchDocument.Operations,
            operation =>
            {
                Assert.Equal("add", operation.op);
                Assert.Equal("", operation.path);
                Assert.Equal(1, operation.value);
            },
            operation =>
            {
                Assert.Equal("add", operation.op);
                Assert.Equal("/", operation.path);
                Assert.Equal(2, operation.value);
            });
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

    [Fact]
    public void Serialization_ShouldExcludeFrom_WhenNullAndNotMoveOrCopy()
    {
        // Arrange
        JsonPatchDocument patchDocument = new();
        patchDocument.Add("/a/b/c", "foo");
        patchDocument.Remove("/x/y/z");
        patchDocument.Replace("/d/e", "bar");
        patchDocument.Test("/f/e", "t1");
        patchDocument.Replace("/a/b/c", null);

        var json = JsonConvert.SerializeObject(patchDocument);

        // Assert
        var expectedJson = """
        [{"value":"foo","path":"/a/b/c","op":"add"},{"path":"/x/y/z","op":"remove"},
        {"value":"bar","path":"/d/e","op":"replace"},{ "value":"t1","path":"/f/e","op":"test"},
        {"value":null,"path":"/a/b/c","op":"replace"}]
        """;

        // Act
        Assert.True(JToken.DeepEquals(JArray.Parse(expectedJson), JArray.Parse(json)));
    }
}
