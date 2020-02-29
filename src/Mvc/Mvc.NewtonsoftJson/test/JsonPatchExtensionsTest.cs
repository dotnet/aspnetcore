// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Xunit;

namespace Microsoft.AspNetCore.Mvc
{
    public class JsonPatchExtensionsTest
    {
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