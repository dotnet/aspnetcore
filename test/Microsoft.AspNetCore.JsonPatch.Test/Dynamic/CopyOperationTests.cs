// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Dynamic;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNetCore.JsonPatch.Internal
{
    public class CopyOperationTests
    {
        [Fact]
        public void Copy()
        {
            dynamic doc = new ExpandoObject();

            doc.StringProperty = "A";
            doc.AnotherStringProperty = "B";

            var patchDoc = new JsonPatchDocument();
            patchDoc.Copy("StringProperty", "AnotherStringProperty");

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);
            deserialized.ApplyTo(doc);

            Assert.Equal("A", doc.AnotherStringProperty);
        }

        [Fact]
        public void CopyInList()
        {
            dynamic doc = new ExpandoObject();
            doc.IntegerList = new List<int>() { 1, 2, 3 };

            // create patch
            var patchDoc = new JsonPatchDocument();
            patchDoc.Copy("IntegerList/0", "IntegerList/1");

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);

            deserialized.ApplyTo(doc);

            Assert.Equal(new List<int>() { 1, 1, 2, 3 }, doc.IntegerList);
        }

        [Fact]
        public void CopyFromListToEndOfList()
        {
            dynamic doc = new ExpandoObject();
            doc.IntegerList = new List<int>() { 1, 2, 3 };

            // create patch
            var patchDoc = new JsonPatchDocument();
            patchDoc.Copy("IntegerList/0", "IntegerList/-");

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);
            deserialized.ApplyTo(doc);

            Assert.Equal(new List<int>() { 1, 2, 3, 1 }, doc.IntegerList);
        }

        [Fact]
        public void CopyFromListToNonList()
        {
            dynamic doc = new ExpandoObject();
            doc.IntegerList = new List<int>() { 1, 2, 3 };

            // create patch
            var patchDoc = new JsonPatchDocument();
            patchDoc.Copy("IntegerList/0", "IntegerValue");

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);

            deserialized.ApplyTo(doc);

            Assert.Equal(1, doc.IntegerValue);
        }

        [Fact]
        public void CopyFromNonListToList()
        {
            dynamic doc = new ExpandoObject();
            doc.IntegerValue = 5;
            doc.IntegerList = new List<int>() { 1, 2, 3 };

            // create patch
            var patchDoc = new JsonPatchDocument();
            patchDoc.Copy("IntegerValue", "IntegerList/0");

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);
            deserialized.ApplyTo(doc);

            Assert.Equal(new List<int>() { 5, 1, 2, 3 }, doc.IntegerList);
        }

        [Fact]
        public void CopyToEndOfList()
        {
            dynamic doc = new ExpandoObject();
            doc.IntegerValue = 5;
            doc.IntegerList = new List<int>() { 1, 2, 3 };

            // create patch
            var patchDoc = new JsonPatchDocument();
            patchDoc.Copy("IntegerValue", "IntegerList/-");

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);

            deserialized.ApplyTo(doc);

            Assert.Equal(new List<int>() { 1, 2, 3, 5 }, doc.IntegerList);
        }

        [Fact]
        public void NestedCopy()
        {
            dynamic doc = new ExpandoObject();
            doc.SimpleObject = new SimpleObject()
            {
                StringProperty = "A",
                AnotherStringProperty = "B"
            };

            // create patch
            var patchDoc = new JsonPatchDocument();
            patchDoc.Copy("SimpleObject/StringProperty", "SimpleObject/AnotherStringProperty");

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);

            deserialized.ApplyTo(doc);

            Assert.Equal("A", doc.SimpleObject.AnotherStringProperty);
        }

        [Fact]
        public void NestedCopyInList()
        {
            dynamic doc = new ExpandoObject();
            doc.SimpleObject = new SimpleObject()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument();
            patchDoc.Copy("SimpleObject/IntegerList/0", "SimpleObject/IntegerList/1");

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);
            deserialized.ApplyTo(doc);

            Assert.Equal(new List<int>() { 1, 1, 2, 3 }, doc.SimpleObject.IntegerList);
        }

        [Fact]
        public void NestedCopyFromListToEndOfList()
        {
            dynamic doc = new ExpandoObject();
            doc.SimpleObject = new SimpleObject()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument();
            patchDoc.Copy("SimpleObject/IntegerList/0", "SimpleObject/IntegerList/-");

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);

            deserialized.ApplyTo(doc);

            Assert.Equal(new List<int>() { 1, 2, 3, 1 }, doc.SimpleObject.IntegerList);
        }

        [Fact]
        public void NestedCopyFromListToNonList()
        {
            dynamic doc = new ExpandoObject();
            doc.SimpleObject = new SimpleObject()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument();
            patchDoc.Copy("SimpleObject/IntegerList/0", "SimpleObject/IntegerValue");

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);
            deserialized.ApplyTo(doc);

            Assert.Equal(1, doc.SimpleObject.IntegerValue);
        }

        [Fact]
        public void NestedCopyFromNonListToList()
        {
            dynamic doc = new ExpandoObject();
            doc.SimpleObject = new SimpleObject()
            {
                IntegerValue = 5,
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument();
            patchDoc.Copy("SimpleObject/IntegerValue", "SimpleObject/IntegerList/0");
            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);
            deserialized.ApplyTo(doc);

            Assert.Equal(new List<int>() { 5, 1, 2, 3 }, doc.SimpleObject.IntegerList);
        }

        [Fact]
        public void NestedCopyToEndOfList()
        {
            dynamic doc = new ExpandoObject();
            doc.SimpleObject = new SimpleObject()
            {
                IntegerValue = 5,
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument();
            patchDoc.Copy("SimpleObject/IntegerValue", "SimpleObject/IntegerList/-");

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);
            deserialized.ApplyTo(doc);

            Assert.Equal(new List<int>() { 1, 2, 3, 5 }, doc.SimpleObject.IntegerList);
        }
    }
}
