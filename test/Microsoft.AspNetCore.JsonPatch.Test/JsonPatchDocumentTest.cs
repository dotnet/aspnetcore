// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.JsonPatch.Exceptions;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNetCore.JsonPatch.Test
{
    public class JsonPatchDocumentTest
    {
        [Fact]
        public void TestOperation_ThrowsException_CallsIntoLogErrorAction()
        {
            // Arrange
            var serialized = "[{\"value\":\"John\",\"path\":\"/Name\",\"op\":\"test\"}]";
            var jsonPatchDocument = JsonConvert.DeserializeObject<JsonPatchDocument<Customer>>(serialized);
            var model = new Customer();
            var expectedErrorMessage = "The test operation is not supported.";
            string actualErrorMessage = null;

            // Act
            jsonPatchDocument.ApplyTo(model, (jsonPatchError) =>
            {
                actualErrorMessage = jsonPatchError.ErrorMessage;
            });

            // Assert
            Assert.Equal(expectedErrorMessage, actualErrorMessage);
        }

        [Fact]
        public void TestOperation_NoLogErrorAction_ThrowsJsonPatchException()
        {
            // Arrange
            var serialized = "[{\"value\":\"John\",\"path\":\"/Name\",\"op\":\"test\"}]";
            var jsonPatchDocument = JsonConvert.DeserializeObject<JsonPatchDocument<Customer>>(serialized);
            var model = new Customer();
            var expectedErrorMessage = "The test operation is not supported.";

            // Act
            var jsonPatchException = Assert.Throws<JsonPatchException>(() => jsonPatchDocument.ApplyTo(model));

            // Assert
            Assert.Equal(expectedErrorMessage, jsonPatchException.Message);
        }

        [Fact]
        public void InvalidOperation_ThrowsException_CallsIntoLogErrorAction()
        {
            // Arrange
            var operationName = "foo";
            var serialized = "[{\"value\":\"John\",\"path\":\"/Name\",\"op\":\"" + operationName + "\"}]";
            var jsonPatchDocument = JsonConvert.DeserializeObject<JsonPatchDocument<Customer>>(serialized);
            var model = new Customer();
            var expectedErrorMessage = $"Invalid JsonPatch operation '{operationName}'.";
            string actualErrorMessage = null;

            // Act
            jsonPatchDocument.ApplyTo(model, (jsonPatchError) =>
            {
                actualErrorMessage = jsonPatchError.ErrorMessage;
            });

            // Assert
            Assert.Equal(expectedErrorMessage, actualErrorMessage);
        }

        [Fact]
        public void InvalidOperation_NoLogErrorAction_ThrowsJsonPatchException()
        {
            // Arrange
            var operationName = "foo";
            var serialized = "[{\"value\":\"John\",\"path\":\"/Name\",\"op\":\"" + operationName + "\"}]";
            var jsonPatchDocument = JsonConvert.DeserializeObject<JsonPatchDocument<Customer>>(serialized);
            var model = new Customer();
            var expectedErrorMessage = $"Invalid JsonPatch operation '{operationName}'.";

            // Act
            var jsonPatchException = Assert.Throws<JsonPatchException>(() => jsonPatchDocument.ApplyTo(model));

            // Assert
            Assert.Equal(expectedErrorMessage, jsonPatchException.Message);
        }

        private class Customer
        {
            public string Name { get; set; }
        }
    }
}
