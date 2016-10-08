// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Newtonsoft.Json;
using System.Linq;
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
            Assert.Equal(pathToCheck, "/anothername");
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

            Assert.Equal(doc.AnotherName, "John");
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

            Assert.Equal(doc.Name, "John");
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

            Assert.Equal(null, doc.Name);
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
            Assert.Equal(pathToCheck, "/anothername");
        }

        [Fact]
        public void Add_OnApplyFromJson_EscapingHandledOnComplexJsonPropertyNameOnJsonDocument()
        {
            var doc = new JsonPropertyComplexNameDTO()
            {
                FooSlashBars = "InitialName",
                FooSlashTilde = new  SimpleDTO
                {
                    StringProperty  = "Initial Value"
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
    }
}
