// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.JsonPatch.Exceptions;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNetCore.JsonPatch.Internal
{
    public class PatchDocumentTests
    {
        [Fact]
        public void InvalidPathAtBeginningShouldThrowException()
        {
            var patchDoc = new JsonPatchDocument();
            var exception = Assert.Throws<JsonPatchException>(() =>
            {
                patchDoc.Add("//NewInt", 1);
            });
            Assert.Equal(
               "The provided string '//NewInt' is an invalid path.",
                exception.Message);
        }

        [Fact]
        public void InvalidPathAtEndShouldThrowException()
        {
            var patchDoc = new JsonPatchDocument();
            var exception = Assert.Throws<JsonPatchException>(() =>
            {
                patchDoc.Add("NewInt//", 1);
            });
            Assert.Equal(
               "The provided string 'NewInt//' is an invalid path.",
                exception.Message);
        }

        [Fact]
        public void InvalidPathWithDotShouldThrowException()
        {
            var patchDoc = new JsonPatchDocument();
            var exception = Assert.Throws<JsonPatchException>(() =>
            {
                patchDoc.Add("NewInt.Test", 1);
            });
            Assert.Equal(
               "The provided string 'NewInt.Test' is an invalid path.",
                exception.Message);
        }

        [Fact]
        public void NonGenericPatchDocToGenericMustSerialize()
        {
            var doc = new SimpleObject()
            {
                StringProperty = "A",
                AnotherStringProperty = "B"
            };

            var patchDoc = new JsonPatchDocument();
            patchDoc.Copy("StringProperty", "AnotherStringProperty");

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleObject>>(serialized);

            deserialized.ApplyTo(doc);

            Assert.Equal("A", doc.AnotherStringProperty);
        }

        [Fact]
        public void GenericPatchDocToNonGenericMustSerialize()
        {
            var doc = new SimpleObject()
            {
                StringProperty = "A",
                AnotherStringProperty = "B"
            };

            var patchDocTyped = new JsonPatchDocument<SimpleObject>();
            patchDocTyped.Copy(o => o.StringProperty, o => o.AnotherStringProperty);

            var patchDocUntyped = new JsonPatchDocument();
            patchDocUntyped.Copy("StringProperty", "AnotherStringProperty");

            var serializedTyped = JsonConvert.SerializeObject(patchDocTyped);
            var serializedUntyped = JsonConvert.SerializeObject(patchDocUntyped);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument>(serializedTyped);

            deserialized.ApplyTo(doc);

            Assert.Equal("A", doc.AnotherStringProperty);
        }
    }
}