// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace Microsoft.AspNetCore.JsonPatch
{
    public class JsonPatchDocumentJsonPropertyAttributeTest
    {
        [Fact]
        public void Add_WithExpression_RespectsJsonPropertyName_ForModelProperty()
        {
            var patchDoc = new JsonPatchDocument<JsonPropertyDTO>();
            patchDoc.Add(p => p.Name, "John");

            var serialized = JsonConvert.SerializeObject(patchDoc);
            // serialized value should have "AnotherName" as path
            // deserialize to a JsonPatchDocument<JsonPropertyWithAnotherNameDTO> to check
            var deserialized =
                JsonConvert.DeserializeObject<JsonPatchDocument<JsonPropertyWithAnotherNameDTO>>(serialized);

            // get path
            var pathToCheck = deserialized.Operations.First().path;
            Assert.Equal("/anothername", pathToCheck);
        }

        [Fact]
        public void Add_WithExpressionOnStringProperty_FallsbackToPropertyName_WhenJsonPropertyName_IsEmpty()
        {
            // Arrange
            var patchDoc = new JsonPatchDocument<JsonPropertyWithNoPropertyName>();
            patchDoc.Add(m => m.StringProperty, "Test");
            var serialized = JsonConvert.SerializeObject(patchDoc);

            // Act
            var deserialized =
                JsonConvert.DeserializeObject<JsonPatchDocument<JsonPropertyWithNoPropertyName>>(serialized);

            // Assert
            var pathToCheck = deserialized.Operations.First().path;
            Assert.Equal("/stringproperty", pathToCheck);
        }

        [Fact]
        public void Add_WithExpressionOnArrayProperty_FallsbackToPropertyName_WhenJsonPropertyName_IsEmpty()
        {
            // Arrange
            var patchDoc = new JsonPatchDocument<JsonPropertyWithNoPropertyName>();
            patchDoc.Add(m => m.ArrayProperty, "James");
            var serialized = JsonConvert.SerializeObject(patchDoc);

            // Act
            var deserialized =
                JsonConvert.DeserializeObject<JsonPatchDocument<JsonPropertyWithNoPropertyName>>(serialized);

            // Assert
            var pathToCheck = deserialized.Operations.First().path;
            Assert.Equal("/arrayproperty/-", pathToCheck);
        }

        [Fact]
        public void Add_WithExpression_RespectsJsonPropertyName_WhenApplyingToDifferentlyTypedClassWithPropertyMatchingJsonPropertyName()
        {
            var patchDocToSerialize = new JsonPatchDocument<JsonPropertyDTO>();
            patchDocToSerialize.Add(p => p.Name, "John");

            // the patchdoc will deserialize to "anothername".  We should thus be able to apply
            // it to a class that HAS that other property name.
            var doc = new JsonPropertyWithAnotherNameDTO()
            {
                AnotherName = "InitialValue"
            };

            var serialized = JsonConvert.SerializeObject(patchDocToSerialize);
            var deserialized =
                JsonConvert.DeserializeObject<JsonPatchDocument<JsonPropertyWithAnotherNameDTO>>
                (serialized);

            deserialized.ApplyTo(doc);

            Assert.Equal("John", doc.AnotherName);
        }

        [Fact]
        public void Add_WithExpression_RespectsJsonPropertyName_WhenApplyingToSameTypedClassWithMatchingJsonPropertyName()
        {
            var patchDocToSerialize = new JsonPatchDocument<JsonPropertyDTO>();
            patchDocToSerialize.Add(p => p.Name, "John");

            // the patchdoc will deserialize to "anothername".  As JsonPropertyDTO has
            // a JsonProperty signifying that "Name" should be deseriallized from "AnotherName",
            // we should be able to apply the patchDoc.

            var doc = new JsonPropertyDTO()
            {
                Name = "InitialValue"
            };

            var serialized = JsonConvert.SerializeObject(patchDocToSerialize);
            var deserialized =
                JsonConvert.DeserializeObject<JsonPatchDocument<JsonPropertyDTO>>
                (serialized);

            deserialized.ApplyTo(doc);

            Assert.Equal("John", doc.Name);
        }

        [Fact]
        public void Add_OnApplyFromJson_RespectsJsonPropertyNameOnJsonDocument()
        {
            var doc = new JsonPropertyDTO()
            {
                Name = "InitialValue"
            };

            // serialization should serialize to "AnotherName"
            var serialized = "[{\"value\":\"Kevin\",\"path\":\"/AnotherName\",\"op\":\"add\"}]";
            var deserialized =
                JsonConvert.DeserializeObject<JsonPatchDocument<JsonPropertyDTO>>(serialized);

            deserialized.ApplyTo(doc);

            Assert.Equal("Kevin", doc.Name);
        }

        [Fact]
        public void Remove_OnApplyFromJson_RespectsJsonPropertyNameOnJsonDocument()
        {
            var doc = new JsonPropertyDTO()
            {
                Name = "InitialValue"
            };

            // serialization should serialize to "AnotherName"
            var serialized = "[{\"path\":\"/AnotherName\",\"op\":\"remove\"}]";
            var deserialized =
                JsonConvert.DeserializeObject<JsonPatchDocument<JsonPropertyDTO>>(serialized);

            deserialized.ApplyTo(doc);

            Assert.Null(doc.Name);
        }

        [Fact]
        public void Add_OnApplyFromJson_RespectsInheritedJsonPropertyNameOnJsonDocument()
        {
            var doc = new JsonPropertyWithInheritanceDTO()
            {
                Name = "InitialName"
            };

            // serialization should serialize to "AnotherName"
            var serialized = "[{\"value\":\"Kevin\",\"path\":\"/AnotherName\",\"op\":\"add\"}]";
            var deserialized =
                JsonConvert.DeserializeObject<JsonPatchDocument<JsonPropertyWithInheritanceDTO>>(serialized);

            deserialized.ApplyTo(doc);

            Assert.Equal("Kevin", doc.Name);
        }

        [Fact]
        public void Add_WithExpression_RespectsJsonPropertyName_ForInheritedModelProperty()
        {
            var patchDoc = new JsonPatchDocument<JsonPropertyWithInheritanceDTO>();
            patchDoc.Add(p => p.Name, "John");

            var serialized = JsonConvert.SerializeObject(patchDoc);
            // serialized value should have "AnotherName" as path
            // deserialize to a JsonPatchDocument<JsonPropertyWithAnotherNameDTO> to check
            var deserialized =
                JsonConvert.DeserializeObject<JsonPatchDocument<JsonPropertyWithAnotherNameDTO>>(serialized);

            // get path
            var pathToCheck = deserialized.Operations.First().path;
            Assert.Equal("/anothername", pathToCheck);
        }

        [Fact]
        public void Add_OnApplyFromJson_EscapingHandledOnComplexJsonPropertyNameOnJsonDocument()
        {
            var doc = new JsonPropertyComplexNameDTO()
            {
                FooSlashBars = "InitialName",
                FooSlashTilde = new SimpleDTO
                {
                    StringProperty = "Initial Value"
                }
            };

            // serialization should serialize to "AnotherName"
            var serialized = "[{\"value\":\"Kevin\",\"path\":\"/foo~1bar~0\",\"op\":\"add\"},{\"value\":\"Final Value\",\"path\":\"/foo~1~0/StringProperty\",\"op\":\"replace\"}]";
            var deserialized =
                JsonConvert.DeserializeObject<JsonPatchDocument<JsonPropertyComplexNameDTO>>(serialized);

            deserialized.ApplyTo(doc);

            Assert.Equal("Kevin", doc.FooSlashBars);
            Assert.Equal("Final Value", doc.FooSlashTilde.StringProperty);
        }

        [Fact]
        public void Move_WithExpression_FallsbackToPropertyName_WhenJsonPropertyName_IsEmpty()
        {
            // Arrange
            var patchDoc = new JsonPatchDocument<JsonPropertyWithNoPropertyName>();
            patchDoc.Move(m => m.StringProperty, m => m.StringProperty2);
            var serialized = JsonConvert.SerializeObject(patchDoc);

            // Act
            var deserialized =
                JsonConvert.DeserializeObject<JsonPatchDocument<JsonPropertyWithNoPropertyName>>(serialized);

            // Assert
            var fromPath = deserialized.Operations.First().from;
            Assert.Equal("/stringproperty", fromPath);
            var toPath = deserialized.Operations.First().path;
            Assert.Equal("/stringproperty2", toPath);
        }

        [Fact]
        public void Add_WithExpression_AndCustomContractResolver_UsesPropertyName_SetByContractResolver()
        {
            // Arrange
            var patchDoc = new JsonPatchDocument<JsonPropertyWithNoPropertyName>();
            patchDoc.ContractResolver = new CustomContractResolver();
            patchDoc.Add(m => m.SSN, "123-45-6789");
            var serialized = JsonConvert.SerializeObject(patchDoc);

            // Act
            var deserialized =
                JsonConvert.DeserializeObject<JsonPatchDocument<JsonPropertyWithNoPropertyName>>(serialized);

            // Assert
            var path = deserialized.Operations.First().path;
            Assert.Equal("/socialsecuritynumber", path);
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

        private class CustomContractResolver : DefaultContractResolver
        {
            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                var jsonProperty = base.CreateProperty(member, memberSerialization);

                if (jsonProperty.PropertyName == "SSN")
                {
                    jsonProperty.PropertyName = "SocialSecurityNumber";
                }

                return jsonProperty;
            }
        }
    }
}
