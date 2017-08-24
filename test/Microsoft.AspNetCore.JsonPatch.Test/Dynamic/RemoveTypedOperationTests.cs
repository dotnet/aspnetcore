// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.JsonPatch.Exceptions;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNetCore.JsonPatch.Internal
{
    public class RemoveTypedOperationTests
    {
        [Fact]
        public void Remove()
        {
            var doc = new SimpleObject()
            {
                StringProperty = "A"
            };

            // create patch
            var patchDoc = new JsonPatchDocument();
            patchDoc.Remove("StringProperty");

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);

            deserialized.ApplyTo(doc);

            Assert.Null(doc.StringProperty);
        }

        [Fact]
        public void RemoveFromList()
        {
            var doc = new SimpleObject()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument();
            patchDoc.Remove("IntegerList/2");

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);

            deserialized.ApplyTo(doc);

            Assert.Equal(new List<int>() { 1, 2 }, doc.IntegerList);
        }

        [Fact]
        public void RemoveFromListInvalidPositionTooLarge()
        {
            var doc = new SimpleObject()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument();
            patchDoc.Remove("IntegerList/3");

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);

            var exception = Assert.Throws<JsonPatchException>(() =>
            {
                deserialized.ApplyTo(doc);
            });
            Assert.Equal(
               string.Format("The index value provided by path segment '{0}' is out of bounds of the array size.", "3"),
                exception.Message);
        }

        [Fact]
        public void RemoveFromListInvalidPositionTooSmall()
        {
            var doc = new SimpleObject()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument();
            patchDoc.Remove("IntegerList/-1");

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);

            var exception = Assert.Throws<JsonPatchException>(() =>
            {
                deserialized.ApplyTo(doc);
            });
            Assert.Equal(
               string.Format("The index value provided by path segment '{0}' is out of bounds of the array size.", "-1"),
                exception.Message);
        }

        [Fact]
        public void RemoveFromEndOfList()
        {
            var doc = new SimpleObject()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument();
            patchDoc.Remove("IntegerList/-");

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);

            deserialized.ApplyTo(doc);

            Assert.Equal(new List<int>() { 1, 2 }, doc.IntegerList);
        }

        [Fact]
        public void NestedRemove()
        {
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    StringProperty = "A"
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument();
            patchDoc.Remove("SimpleObject/StringProperty");

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);

            deserialized.ApplyTo(doc);

            Assert.Null(doc.SimpleObject.StringProperty);
        }

        [Fact]
        public void NestedRemoveFromList()
        {
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument();
            patchDoc.Remove("SimpleObject/IntegerList/2");

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);

            deserialized.ApplyTo(doc);

            Assert.Equal(new List<int>() { 1, 2 }, doc.SimpleObject.IntegerList);
        }

        [Fact]
        public void NestedRemoveFromListInvalidPositionTooLarge()
        {
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument();
            patchDoc.Remove("SimpleObject/IntegerList/3");

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);

            var exception = Assert.Throws<JsonPatchException>(() =>
            {
                deserialized.ApplyTo(doc);
            });
            Assert.Equal(
               string.Format("The index value provided by path segment '{0}' is out of bounds of the array size.", "3"),
               exception.Message);
        }

        [Fact]
        public void NestedRemoveFromListInvalidPositionTooSmall()
        {
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument();
            patchDoc.Remove("SimpleObject/IntegerList/-1");

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);

            var exception = Assert.Throws<JsonPatchException>(() =>
            {
                deserialized.ApplyTo(doc);
            });
            Assert.Equal(
               string.Format("The index value provided by path segment '{0}' is out of bounds of the array size.", "-1"),
                exception.Message);
        }

        [Fact]
        public void NestedRemoveFromEndOfList()
        {
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument();
            patchDoc.Remove("SimpleObject/IntegerList/-");

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);

            deserialized.ApplyTo(doc);

            Assert.Equal(new List<int>() { 1, 2 }, doc.SimpleObject.IntegerList);
        }
    }
}
