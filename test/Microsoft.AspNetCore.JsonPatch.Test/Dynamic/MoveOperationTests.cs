// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Dynamic;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNetCore.JsonPatch.Test.Dynamic
{
    public class MoveOperationTests
    {
        [Fact]
        public void Move()
        {
            dynamic doc = new ExpandoObject();
            doc.StringProperty = "A";
            doc.AnotherStringProperty = "B";

            // create patch
            JsonPatchDocument patchDoc = new JsonPatchDocument();
            patchDoc.Move("StringProperty", "AnotherStringProperty");

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);

            deserialized.ApplyTo(doc);

            Assert.Equal("A", doc.AnotherStringProperty);

            var cont = doc as IDictionary<string, object>;
            object valueFromDictionary;
            cont.TryGetValue("StringProperty", out valueFromDictionary);
            Assert.Null(valueFromDictionary);
        }

        [Fact]
        public void MoveToNonExisting()
        {
            dynamic doc = new ExpandoObject();
            doc.StringProperty = "A";

            // create patch
            JsonPatchDocument patchDoc = new JsonPatchDocument();
            patchDoc.Move("StringProperty", "AnotherStringProperty");

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);

            deserialized.ApplyTo(doc);

            Assert.Equal("A", doc.AnotherStringProperty);

            var cont = doc as IDictionary<string, object>;
            object valueFromDictionary;
            cont.TryGetValue("StringProperty", out valueFromDictionary);
            Assert.Null(valueFromDictionary);
        }

        [Fact]
        public void MoveDynamicToTyped()
        {
            dynamic doc = new ExpandoObject();
            doc.StringProperty = "A";
            doc.SimpleDTO = new SimpleDTO() { AnotherStringProperty = "B" };

            // create patch
            JsonPatchDocument patchDoc = new JsonPatchDocument();
            patchDoc.Move("StringProperty", "SimpleDTO/AnotherStringProperty");

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);

            deserialized.ApplyTo(doc);

            Assert.Equal("A", doc.SimpleDTO.AnotherStringProperty);

            var cont = doc as IDictionary<string, object>;
            object valueFromDictionary;
            cont.TryGetValue("StringProperty", out valueFromDictionary);
            Assert.Null(valueFromDictionary);
        }

        [Fact]
        public void MoveTypedToDynamic()
        {
            dynamic doc = new ExpandoObject();
            doc.StringProperty = "A";
            doc.SimpleDTO = new SimpleDTO() { AnotherStringProperty = "B" };

            // create patch
            JsonPatchDocument patchDoc = new JsonPatchDocument();
            patchDoc.Move("SimpleDTO/AnotherStringProperty", "StringProperty");

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);

            deserialized.ApplyTo(doc);

            Assert.Equal("B", doc.StringProperty);
            Assert.Equal(null, doc.SimpleDTO.AnotherStringProperty);
        }

        [Fact]
        public void NestedMove()
        {
            dynamic doc = new ExpandoObject();
            doc.Nested = new SimpleDTO()
            {
                StringProperty = "A",
                AnotherStringProperty = "B"
            };

            // create patch
            JsonPatchDocument patchDoc = new JsonPatchDocument();
            patchDoc.Move("Nested/StringProperty", "Nested/AnotherStringProperty");

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);

            deserialized.ApplyTo(doc);

            Assert.Equal("A", doc.Nested.AnotherStringProperty);
            Assert.Equal(null, doc.Nested.StringProperty);
        }

        [Fact]
        public void MoveInList()
        {
            dynamic doc = new ExpandoObject();
            doc.IntegerList = new List<int>() { 1, 2, 3 };

            // create patch
            JsonPatchDocument patchDoc = new JsonPatchDocument();
            patchDoc.Move("IntegerList/0", "IntegerList/1");

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);

            deserialized.ApplyTo(doc);

            Assert.Equal(new List<int>() { 2, 1, 3 }, doc.IntegerList);
        }

        [Fact]
        public void NestedMoveInList()
        {
            dynamic doc = new ExpandoObject();
            doc.Nested = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            JsonPatchDocument patchDoc = new JsonPatchDocument();
            patchDoc.Move("Nested/IntegerList/0", "Nested/IntegerList/1");

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);

            deserialized.ApplyTo(doc);

            Assert.Equal(new List<int>() { 2, 1, 3 }, doc.Nested.IntegerList);
        }

        [Fact]
        public void MoveFromListToEndOfList()
        {
            dynamic doc = new ExpandoObject();
            doc.IntegerList = new List<int>() { 1, 2, 3 };

            // create patch
            JsonPatchDocument patchDoc = new JsonPatchDocument();
            patchDoc.Move("IntegerList/0", "IntegerList/-");

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);
            deserialized.ApplyTo(doc);

            Assert.Equal(new List<int>() { 2, 3, 1 }, doc.IntegerList);
        }

        [Fact]
        public void NestedMoveFromListToEndOfList()
        {
            dynamic doc = new ExpandoObject();
            doc.Nested = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            JsonPatchDocument patchDoc = new JsonPatchDocument();
            patchDoc.Move("Nested/IntegerList/0", "Nested/IntegerList/-");

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);
            deserialized.ApplyTo(doc);

            Assert.Equal(new List<int>() { 2, 3, 1 }, doc.Nested.IntegerList);
        }

        [Fact]
        public void MoveFomListToNonList()
        {
            dynamic doc = new ExpandoObject();
            doc.IntegerList = new List<int>() { 1, 2, 3 };

            // create patch
            JsonPatchDocument patchDoc = new JsonPatchDocument();
            patchDoc.Move("IntegerList/0", "IntegerValue");

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);

            deserialized.ApplyTo(doc);

            Assert.Equal(new List<int>() { 2, 3 }, doc.IntegerList);
            Assert.Equal(1, doc.IntegerValue);
        }

        [Fact]
        public void NestedMoveFomListToNonList()
        {
            dynamic doc = new ExpandoObject();
            doc.Nested = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            JsonPatchDocument patchDoc = new JsonPatchDocument();
            patchDoc.Move("Nested/IntegerList/0", "Nested/IntegerValue");

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);

            deserialized.ApplyTo(doc);

            Assert.Equal(new List<int>() { 2, 3 }, doc.Nested.IntegerList);
            Assert.Equal(1, doc.Nested.IntegerValue);
        }

        [Fact]
        public void MoveFromNonListToList()
        {
            dynamic doc = new ExpandoObject();
            doc.IntegerValue = 5;
            doc.IntegerList = new List<int>() { 1, 2, 3 };

            // create patch
            JsonPatchDocument patchDoc = new JsonPatchDocument();
            patchDoc.Move("IntegerValue", "IntegerList/0");

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);

            deserialized.ApplyTo(doc);

            var cont = doc as IDictionary<string, object>;
            object valueFromDictionary;
            cont.TryGetValue("IntegerValue", out valueFromDictionary);
            Assert.Null(valueFromDictionary);

            Assert.Equal(new List<int>() { 5, 1, 2, 3 }, doc.IntegerList);
        }

        [Fact]
        public void NestedMoveFromNonListToList()
        {
            dynamic doc = new ExpandoObject();
            doc.Nested = new SimpleDTO()
            {
                IntegerValue = 5,
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            JsonPatchDocument patchDoc = new JsonPatchDocument();
            patchDoc.Move("Nested/IntegerValue", "Nested/IntegerList/0");

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);

            deserialized.ApplyTo(doc);

            Assert.Equal(0, doc.Nested.IntegerValue);
            Assert.Equal(new List<int>() { 5, 1, 2, 3 }, doc.Nested.IntegerList);
        }

        [Fact]
        public void MoveToEndOfList()
        {
            dynamic doc = new ExpandoObject();
            doc.IntegerValue = 5;
            doc.IntegerList = new List<int>() { 1, 2, 3 };

            // create patch
            JsonPatchDocument patchDoc = new JsonPatchDocument();
            patchDoc.Move("IntegerValue", "IntegerList/-");

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);

            deserialized.ApplyTo(doc);

            var cont = doc as IDictionary<string, object>;
            object valueFromDictionary;
            cont.TryGetValue("IntegerValue", out valueFromDictionary);
            Assert.Null(valueFromDictionary);

            Assert.Equal(new List<int>() { 1, 2, 3, 5 }, doc.IntegerList);
        }

        [Fact]
        public void NestedMoveToEndOfList()
        {
            dynamic doc = new ExpandoObject();
            doc.Nested = new SimpleDTO()
            {
                IntegerValue = 5,
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            JsonPatchDocument patchDoc = new JsonPatchDocument();
            patchDoc.Move("Nested/IntegerValue", "Nested/IntegerList/-");

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);

            deserialized.ApplyTo(doc);

            Assert.Equal(0, doc.Nested.IntegerValue);
            Assert.Equal(new List<int>() { 1, 2, 3, 5 }, doc.Nested.IntegerList);
        }
    }
}
