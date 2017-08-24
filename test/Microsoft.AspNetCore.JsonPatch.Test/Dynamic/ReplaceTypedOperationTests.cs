// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNetCore.JsonPatch.Internal
{
    public class ReplaceTypedOperationTests
    {
        [Fact]
        public void ReplaceGuidTest()
        {
            var doc = new SimpleObject()
            {
                GuidValue = Guid.NewGuid()
            };

            var newGuid = Guid.NewGuid();
            // create patch
            var patchDoc = new JsonPatchDocument();
            patchDoc.Replace("GuidValue", newGuid);

            // serialize & deserialize
            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserizalized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);

            deserizalized.ApplyTo(doc);

            Assert.Equal(newGuid, doc.GuidValue);
        }

        [Fact]
        public void SerializeAndReplaceNestedObjectTest()
        {
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerValue = 5,
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            var newDTO = new SimpleObject()
            {
                DoubleValue = 1
            };

            // create patch
            var patchDoc = new JsonPatchDocument();
            patchDoc.Replace("SimpleObject", newDTO);

            // serialize & deserialize
            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);

            deserialized.ApplyTo(doc);

            Assert.Equal(1, doc.SimpleObject.DoubleValue);
            Assert.Equal(0, doc.SimpleObject.IntegerValue);
            Assert.Null(doc.SimpleObject.IntegerList);
        }

        [Fact]
        public void ReplaceInList()
        {
            var doc = new SimpleObject()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument();
            patchDoc.Replace("IntegerList/0", 5);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);

            deserialized.ApplyTo(doc);

            Assert.Equal(new List<int>() { 5, 2, 3 }, doc.IntegerList);
        }

        [Fact]
        public void ReplaceFullList()
        {
            var doc = new SimpleObject()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument();
            patchDoc.Replace("IntegerList", new List<int>() { 4, 5, 6 });

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);

            deserialized.ApplyTo(doc);

            Assert.Equal(new List<int>() { 4, 5, 6 }, doc.IntegerList);
        }

        [Fact]
        public void ReplaceInListInList()
        {
            var doc = new SimpleObject()
            {
                SimpleObjectList = new List<SimpleObject>() {
                new SimpleObject() {
                    IntegerList = new List<int>(){1,2,3}
                }}
            };

            // create patch
            var patchDoc = new JsonPatchDocument();
            patchDoc.Replace("SimpleObjectList/0/IntegerList/0", 4);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);

            deserialized.ApplyTo(doc);

            Assert.Equal(4, doc.SimpleObjectList.First().IntegerList.First());
        }

        [Fact]
        public void ReplaceFullListFromEnumerable()
        {
            var doc = new SimpleObject()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument();
            patchDoc.Replace("IntegerList", new List<int>() { 4, 5, 6 });

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);

            deserialized.ApplyTo(doc);

            Assert.Equal(new List<int>() { 4, 5, 6 }, doc.IntegerList);
        }

        [Fact]
        public void ReplaceFullListWithCollection()
        {
            var doc = new SimpleObject()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument();
            patchDoc.Replace("IntegerList", new Collection<int>() { 4, 5, 6 });

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);

            deserialized.ApplyTo(doc);

            Assert.Equal(new List<int>() { 4, 5, 6 }, doc.IntegerList);
        }

        [Fact]
        public void ReplaceAtEndOfList()
        {
            var doc = new SimpleObject()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument();
            patchDoc.Replace("IntegerList/-", 5);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);
            deserialized.ApplyTo(doc);

            Assert.Equal(new List<int>() { 1, 2, 5 }, doc.IntegerList);
        }
    }
}
