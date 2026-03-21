// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Text.Json.Serialization;
using Xunit;

namespace Microsoft.AspNetCore.JsonPatch.SystemTextJson;

public class JsonPatchDocumentJsonPropertyAttributeTest
{
    [Fact]
    public void Add_RespectsJsonPropertyAttribute()
    {
        // Arrange
        var patchDocument = new JsonPatchDocument<JsonPropertyObject>();

        // Act
        patchDocument.Add(p => p.Name, "John");

        // Assert
        var pathToCheck = patchDocument.Operations.First().path;
        Assert.Equal("/AnotherName", pathToCheck);
    }

    [Fact]
    public void Add_RespectsJsonPropertyAttribute_WithDotWhitespaceAndBackslashInName()
    {
        // Arrange
        var obj = new JsonPropertyObjectWithStrangeNames();
        var patchDocument = new JsonPatchDocument();

        // Act
        patchDocument.Add("/First Name.", "John");
        patchDocument.Add("Last\\Name", "Doe");
        patchDocument.ApplyTo(obj);

        // Assert
        Assert.Equal("John", obj.FirstName);
        Assert.Equal("Doe", obj.LastName);
    }

    [Fact]
    public void Move_FallsbackToPropertyName_WhenJsonPropertyAttributeName_IsEmpty()
    {
        // Arrange
        var patchDocument = new JsonPatchDocument<JsonPropertyWithNoPropertyName>();

        // Act
        patchDocument.Move(m => m.StringProperty, m => m.StringProperty2);

        // Assert
        var fromPath = patchDocument.Operations.First().from;
        Assert.Equal("/StringProperty", fromPath);
        var toPath = patchDocument.Operations.First().path;
        Assert.Equal("/StringProperty2", toPath);
    }

    private class JsonPropertyObject
    {
        [JsonPropertyName("AnotherName")]
        public string Name { get; set; }
    }

    private class JsonPropertyObjectWithStrangeNames
    {
        [JsonPropertyName("First Name.")]
        public string FirstName { get; set; }

        [JsonPropertyName("Last\\Name")]
        public string LastName { get; set; }
    }

    private class JsonPropertyWithNoPropertyName
    {
        public string StringProperty { get; set; }

        public string[] ArrayProperty { get; set; }

        public string StringProperty2 { get; set; }

        public string SSN { get; set; }
    }
}
