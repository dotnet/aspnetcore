// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Dynamic;
using Microsoft.AspNetCore.JsonPatch.Exceptions;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNetCore.JsonPatch.Test.Dynamic
{
    public class RemoveOperationTests
    {
        [Fact]
        public void RemovePropertyShouldFailIfRootIsAnonymous()
        {
            dynamic doc = new
            {
                Test = 1
            };

            // create patch
            var patchDoc = new JsonPatchDocument();
            patchDoc.Remove("Test");

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);

            var exception = Assert.Throws<JsonPatchException>(() =>
            {
                deserialized.ApplyTo(doc);
            });
            Assert.Equal(
                string.Format("The property at path '{0}' could not be updated.", "Test"),
                exception.Message);
        }

        [Fact]
        public void RemovePropertyShouldFailIfItDoesntExist()
        {
            dynamic doc = new ExpandoObject();
            doc.Test = 1;

            // create patch
            var patchDoc = new JsonPatchDocument();
            patchDoc.Remove("NonExisting");

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);

            var exception = Assert.Throws<JsonPatchException>(() =>
            {
                deserialized.ApplyTo(doc);
            });
            Assert.Equal(
                string.Format("The target location specified by path segment '{0}' was not found.", "NonExisting"),
                exception.Message);
        }

        [Fact]
        public void RemovePropertyFromExpandoObject()
        {
            dynamic obj = new ExpandoObject();
            obj.Test = 1;

            // create patch
            var patchDoc = new JsonPatchDocument();
            patchDoc.Remove("Test");

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);

            deserialized.ApplyTo(obj);

            var cont = obj as IDictionary<string, object>;
            cont.TryGetValue("Test", out var valueFromDictionary);
            Assert.Null(valueFromDictionary);
        }

        [Fact]
        public void RemoveProperty_FromExpandoObject_MixedCase_ThrowsPathNotFoundException()
        {
            dynamic obj = new ExpandoObject();
            obj.Test = 1;

            // create patch
            var patchDoc = new JsonPatchDocument();
            patchDoc.Remove("test");

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);

            var exception = Assert.Throws<JsonPatchException>(() =>
            {
                deserialized.ApplyTo(obj);
            });
            Assert.Equal(
                string.Format("The target location specified by path segment '{0}' was not found.",
                "test"),
                exception.Message);
        }

        [Fact]
        public void RemoveNestedPropertyFromExpandoObject()
        {
            dynamic obj = new ExpandoObject();
            obj.Test = new ExpandoObject();
            obj.Test.AnotherTest = "A";

            // create patch
            var patchDoc = new JsonPatchDocument();
            patchDoc.Remove("Test");

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);

            deserialized.ApplyTo(obj);

            var cont = obj as IDictionary<string, object>;
            cont.TryGetValue("Test", out var valueFromDictionary);
            Assert.Null(valueFromDictionary);
        }

        [Fact]
        public void RemoveNestedProperty_FromExpandoObject_MixedCase_ThrowsPathNotFoundException()
        {
            dynamic obj = new ExpandoObject();
            obj.Test = new ExpandoObject();
            obj.Test.AnotherTest = "A";

            // create patch
            var patchDoc = new JsonPatchDocument();
            patchDoc.Remove("test");

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);

            var exception = Assert.Throws<JsonPatchException>(() =>
            {
                deserialized.ApplyTo(obj);
            });
            Assert.Equal(
                string.Format("The target location specified by path segment '{0}' was not found.", "test"),
                exception.Message);
        }

        [Fact]
        public void NestedRemove()
        {
            dynamic doc = new ExpandoObject();
            doc.SimpleDTO = new SimpleDTO()
            {
                StringProperty = "A"
            };

            // create patch
            var patchDoc = new JsonPatchDocument();
            patchDoc.Remove("SimpleDTO/StringProperty");

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);

            deserialized.ApplyTo(doc);
            Assert.Null(doc.SimpleDTO.StringProperty);
        }

        [Fact]
        public void NestedRemove_MixedCase_ThrowsPathNotFoundException()
        {
            dynamic doc = new ExpandoObject();
            doc.SimpleDTO = new SimpleDTO()
            {
                StringProperty = "A"
            };

            // create patch
            var patchDoc = new JsonPatchDocument();
            patchDoc.Remove("Simpledto/stringProperty");

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);

            var exception = Assert.Throws<JsonPatchException>(() =>
            {
                deserialized.ApplyTo(doc);
            });
            Assert.Equal(
                string.Format("For operation '{0}', the target location specified by path '{1}' was not found.",
                "remove",
                "/Simpledto/stringProperty"),
                exception.Message);
        }

        [Fact]
        public void NestedRemoveFromList()
        {
            dynamic doc = new ExpandoObject();
            doc.SimpleDTO = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument();
            patchDoc.Remove("SimpleDTO/IntegerList/2");

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);

            deserialized.ApplyTo(doc);

            Assert.Equal(new List<int>() { 1, 2 }, doc.SimpleDTO.IntegerList);
        }

        [Fact]
        public void NestedRemoveFromListMixedCase()
        {
            dynamic doc = new ExpandoObject();
            doc.SimpleDTO = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument();
            patchDoc.Remove("SimpleDTO/Integerlist/2");

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);

            deserialized.ApplyTo(doc);

            Assert.Equal(new List<int>() { 1, 2 }, doc.SimpleDTO.IntegerList);
        }

        [Fact]
        public void NestedRemoveFromListInvalidPositionTooLarge()
        {
            dynamic doc = new ExpandoObject();
            doc.SimpleDTO = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument();
            patchDoc.Remove("SimpleDTO/IntegerList/3");

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
            dynamic doc = new ExpandoObject();
            doc.SimpleDTO = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument();
            patchDoc.Remove("SimpleDTO/IntegerList/-1");

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
            dynamic doc = new ExpandoObject();
            doc.SimpleDTO = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument();
            patchDoc.Remove("SimpleDTO/IntegerList/-");

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);

            deserialized.ApplyTo(doc);
            Assert.Equal(new List<int>() { 1, 2 }, doc.SimpleDTO.IntegerList);
        }
    }
}
