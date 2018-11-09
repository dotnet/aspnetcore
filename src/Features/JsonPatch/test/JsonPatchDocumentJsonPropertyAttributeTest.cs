// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace Microsoft.AspNetCore.JsonPatch
{
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
            [JsonProperty("AnotherName")]
            public string Name { get; set; }
        }

        private class JsonPropertyObjectWithStrangeNames
        {
            [JsonProperty("First Name.")]
            public string FirstName { get; set; }

            [JsonProperty("Last\\Name")]
            public string LastName { get; set; }
        }

        private class JsonPropertyWithNoPropertyName
        {
            [JsonProperty]
            public string StringProperty { get; set; }

            [JsonProperty]
            public string[] ArrayProperty { get; set; }

            [JsonProperty]
            public string StringProperty2 { get; set; }

            [JsonProperty]
            public string SSN { get; set; }
        }
    }
}
