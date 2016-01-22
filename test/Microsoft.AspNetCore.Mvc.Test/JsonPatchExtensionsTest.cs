// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.JsonPatch;
using Microsoft.AspNet.JsonPatch.Operations;
using Microsoft.AspNet.Mvc.ModelBinding;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class JsonPatchExtensionsTest
    {
        [Fact]
        public void ApplyTo_JsonPatchDocument_ModelState()
        {
            // Arrange
            var operation = new Operation<Customer>("add", "Customer/CustomerId", from: null, value: "TestName");
            var patchDoc = new JsonPatchDocument<Customer>();
            patchDoc.Operations.Add(operation);

            var modelState = new ModelStateDictionary();

            // Act
            patchDoc.ApplyTo(new Customer(), modelState);

            // Assert
            var error = Assert.Single(modelState["Customer"].Errors);
            Assert.Equal("The property at path 'Customer/CustomerId' could not be added.", error.ErrorMessage);
        }

        [Fact]
        public void ApplyTo_JsonPatchDocument_PrefixModelState()
        {
            // Arrange
            var operation = new Operation<Customer>("add", "Customer/CustomerId", from: null, value: "TestName");
            var patchDoc = new JsonPatchDocument<Customer>();
            patchDoc.Operations.Add(operation);

            var modelState = new ModelStateDictionary();

            // Act
            patchDoc.ApplyTo(new Customer(), modelState, "jsonpatch");

            // Assert
            var error = Assert.Single(modelState["jsonpatch.Customer"].Errors);
            Assert.Equal("The property at path 'Customer/CustomerId' could not be added.", error.ErrorMessage);
        }

        public class Customer
        {
            public string CustomerName { get; set; }
        }
    }
}