// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.JsonPatch.Exceptions;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Newtonsoft.Json;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Xunit;

namespace Microsoft.AspNetCore.JsonPatch
{
    public class JsonPatchDocumentTest
    {
        [Fact]
        public void InvalidPathAtBeginningShouldThrowException()
        {
            // Arrange
            var patchDocument = new JsonPatchDocument();

            // Act
            var exception = Assert.Throws<JsonPatchException>(() =>
            {
                patchDocument.Add("//NewInt", 1);
            });

            // Assert
            Assert.Equal(
               "The provided string '//NewInt' is an invalid path.",
                exception.Message);
        }

        [Fact]
        public void InvalidPathAtEndShouldThrowException()
        {
            // Arrange
            var patchDocument = new JsonPatchDocument();

            // Act
            var exception = Assert.Throws<JsonPatchException>(() =>
            {
                patchDocument.Add("NewInt//", 1);
            });

            // Assert
            Assert.Equal(
               "The provided string 'NewInt//' is an invalid path.",
                exception.Message);
        }

        [Fact]
        public void NonGenericPatchDocToGenericMustSerialize()
        {
            // Arrange
            var targetObject = new SimpleObject()
            {
                StringProperty = "A",
                AnotherStringProperty = "B"
            };

            var patchDocument = new JsonPatchDocument();
            patchDocument.Copy("StringProperty", "AnotherStringProperty");

            var serialized = JsonConvert.SerializeObject(patchDocument);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleObject>>(serialized);

            // Act
            deserialized.ApplyTo(targetObject);

            // Assert
            Assert.Equal("A", targetObject.AnotherStringProperty);
        }

        [Fact]
        public void GenericPatchDocToNonGenericMustSerialize()
        {
            // Arrange
            var targetObject = new SimpleObject()
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

            // Act
            deserialized.ApplyTo(targetObject);

            // Assert
            Assert.Equal("A", targetObject.AnotherStringProperty);
        }

        [Fact]
        public void Deserialization_Successful_ForValidJsonPatchDocument()
        {
            // Arrange
            var doc = new SimpleObject()
            {
                StringProperty = "A",
                DecimalValue = 10,
                DoubleValue = 10,
                FloatValue = 10,
                IntegerValue = 10
            };

            var patchDocument = new JsonPatchDocument<SimpleObject>();
            patchDocument.Replace(o => o.StringProperty, "B");
            patchDocument.Replace(o => o.DecimalValue, 12);
            patchDocument.Replace(o => o.DoubleValue, 12);
            patchDocument.Replace(o => o.FloatValue, 12);
            patchDocument.Replace(o => o.IntegerValue, 12);

            // default: no envelope
            var serialized = JsonConvert.SerializeObject(patchDocument);

            // Act
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleObject>>(serialized);

            // Assert
            Assert.IsType<JsonPatchDocument<SimpleObject>>(deserialized);
        }

        [Fact]
        public void Deserialization_Fails_ForInvalidJsonPatchDocument()
        {
            // Arrange
            var serialized = "{\"Operations\": [{ \"op\": \"replace\", \"path\": \"/title\", \"value\": \"New Title\"}]}";

            // Act
            var exception = Assert.Throws<JsonSerializationException>(() =>
            {
                var deserialized
                    = JsonConvert.DeserializeObject<JsonPatchDocument>(serialized);
            });

            // Assert
            Assert.Equal("The JSON patch document was malformed and could not be parsed.", exception.Message);
        }

        [Fact]
        public void Deserialization_Fails_ForInvalidTypedJsonPatchDocument()
        {
            // Arrange
            var serialized = "{\"Operations\": [{ \"op\": \"replace\", \"path\": \"/title\", \"value\": \"New Title\"}]}";

            // Act
            var exception = Assert.Throws<JsonSerializationException>(() =>
            {
                var deserialized
                    = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleObject>>(serialized);
            });

            // Assert
            Assert.Equal("The JSON patch document was malformed and could not be parsed.", exception.Message);
        }

        [Fact]
        public void ApplyTo_JsonPatchDocument_ModelState()
        {
            // Arrange
            var operation = new Operation<Customer>("add", "CustomerId", from: null, value: "TestName");
            var patchDoc = new JsonPatchDocument<Customer>();
            patchDoc.Operations.Add(operation);

            var modelState = new ModelStateDictionary();

            // Act
            patchDoc.ApplyTo(new Customer(), modelState);

            // Assert
            var error = Assert.Single(modelState["Customer"].Errors);
            Assert.Equal("The target location specified by path segment 'CustomerId' was not found.", error.ErrorMessage);
        }

        [Fact]
        public void ApplyTo_JsonPatchDocument_PrefixModelState()
        {
            // Arrange
            var operation = new Operation<Customer>("add", "CustomerId", from: null, value: "TestName");
            var patchDoc = new JsonPatchDocument<Customer>();
            patchDoc.Operations.Add(operation);

            var modelState = new ModelStateDictionary();

            // Act
            patchDoc.ApplyTo(new Customer(), modelState, "jsonpatch");

            // Assert
            var error = Assert.Single(modelState["jsonpatch.Customer"].Errors);
            Assert.Equal("The target location specified by path segment 'CustomerId' was not found.", error.ErrorMessage);
        }

        [Fact]
        public void ApplyTo_ValidPatchOperation_NoErrorsAdded()
        {
            // Arrange
            var patch = new JsonPatchDocument<Customer>();
            patch.Operations.Add(new Operation<Customer>("replace", "/CustomerName", null, "James"));
            var model = new Customer();
            var modelState = new ModelStateDictionary();

            // Act
            patch.ApplyTo(model, modelState);

            // Assert
            Assert.Equal(0, modelState.ErrorCount);
            Assert.Equal("James", model.CustomerName);
        }

        [Theory]
        [InlineData("test", "/CustomerName", null, "James", "The current value '' at path 'CustomerName' is not equal to the test value 'James'.")]
        [InlineData("invalid", "/CustomerName", null, "James", "Invalid JsonPatch operation 'invalid'.")]
        [InlineData("", "/CustomerName", null, "James", "Invalid JsonPatch operation ''.")]
        public void ApplyTo_InvalidPatchOperations_AddsModelStateError(
            string op,
            string path,
            string from,
            string value,
            string error)
        {
            // Arrange
            var patch = new JsonPatchDocument<Customer>();
            patch.Operations.Add(new Operation<Customer>(op, path, from, value));
            var model = new Customer();
            var modelState = new ModelStateDictionary();

            // Act
            patch.ApplyTo(model, modelState);

            // Assert
            Assert.Equal(1, modelState.ErrorCount);
            Assert.Equal(nameof(Customer), modelState.First().Key);
            Assert.Single(modelState.First().Value.Errors);
            Assert.Equal(error, modelState.First().Value.Errors.First().ErrorMessage);
        }

        private class Customer
        {
            public string CustomerName { get; set; }
        }
    }
}
