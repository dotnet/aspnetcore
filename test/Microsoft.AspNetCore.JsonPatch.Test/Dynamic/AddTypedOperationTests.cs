// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.JsonPatch.Exceptions;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNet.JsonPatch.Test.Dynamic
{
    public class AddTypedOperationTests
    {
        [Fact]
        public void AddToListNegativePosition()
        {
            var doc = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            JsonPatchDocument patchDoc = new JsonPatchDocument();
            patchDoc.Add("IntegerList/-1", 4);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);

            var exception = Assert.Throws<JsonPatchException>(() =>
            {
                deserialized.ApplyTo(doc);
            });
            Assert.Equal(
               "For operation 'add' on array property at path '/IntegerList/-1', the index is negative.",
                exception.Message);
        }

        [Fact]
        public void AddToListInList()
        {
            var doc = new SimpleDTOWithNestedDTO()
            {
                ListOfSimpleDTO = new List<SimpleDTO>()
                {
                     new SimpleDTO()
                     {
                         IntegerList = new List<int>() { 1, 2, 3 }
                     }
                }
            };

            // create patch
            JsonPatchDocument patchDoc = new JsonPatchDocument();
            patchDoc.Add("ListOfSimpleDTO/0/IntegerList/0", 4);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);

            deserialized.ApplyTo(doc);
            Assert.Equal(new List<int>() { 4, 1, 2, 3 }, doc.ListOfSimpleDTO[0].IntegerList);
        }

        [Fact]
        public void AddToListInListInvalidPositionTooSmall()
        {
            var doc = new SimpleDTOWithNestedDTO()
            {
                ListOfSimpleDTO = new List<SimpleDTO>()
                {
                    new SimpleDTO()
                    {
                        IntegerList = new List<int>() { 1, 2, 3 }
                    }
                }
            };

            // create patch
            JsonPatchDocument patchDoc = new JsonPatchDocument();
            patchDoc.Add("ListOfSimpleDTO/-1/IntegerList/0", 4);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);

            var exception = Assert.Throws<JsonPatchException>(() =>
            {
                deserialized.ApplyTo(doc);
            });
            Assert.Equal(
                "The property at path '/ListOfSimpleDTO/-1/IntegerList/0' could not be added.",
                exception.Message);
        }

        [Fact]
        public void AddToListInListInvalidPositionTooLarge()
        {
            var doc = new SimpleDTOWithNestedDTO()
            {
                ListOfSimpleDTO = new List<SimpleDTO>()
                {
                     new SimpleDTO()
                     {
                        IntegerList = new List<int>() { 1, 2, 3 }
                     }
                }
            };
            // create patch
            JsonPatchDocument patchDoc = new JsonPatchDocument();
            patchDoc.Add("ListOfSimpleDTO/20/IntegerList/0", 4);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);

            var exception = Assert.Throws<JsonPatchException>(() =>
            {
                deserialized.ApplyTo(doc);
            });
            Assert.Equal(
                "The property at path '/ListOfSimpleDTO/20/IntegerList/0' could not be added.",
                exception.Message);
        }
    }
}
