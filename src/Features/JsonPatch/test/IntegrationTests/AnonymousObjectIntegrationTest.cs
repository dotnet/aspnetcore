// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.JsonPatch.Exceptions;
using Xunit;

namespace Microsoft.AspNetCore.JsonPatch.IntegrationTests
{
    public class AnonymousObjectIntegrationTest
    {
        [Fact]
        public void AddNewProperty_ShouldFail()
        {
            // Arrange
            var targetObject = new { };

            var patchDocument = new JsonPatchDocument();
            patchDocument.Add("NewProperty", 4);

            // Act
            var exception = Assert.Throws<JsonPatchException>(() =>
            {
                patchDocument.ApplyTo(targetObject);
            });

            // Assert
            Assert.Equal("The target location specified by path segment 'NewProperty' was not found.",
                exception.Message);
        }

        [Fact]
        public void AddNewProperty_ToNestedAnonymousObject_ShouldFail()
        {
            // Arrange
            dynamic targetObject = new
            {
                Test = 1,
                nested = new { }
            };

            var patchDocument = new JsonPatchDocument();
            patchDocument.Add("Nested/NewInt", 1);

            // Act
            var exception = Assert.Throws<JsonPatchException>(() =>
            {
                patchDocument.ApplyTo(targetObject);
            });

            // Assert
            Assert.Equal("The target location specified by path segment 'NewInt' was not found.",
                exception.Message);
        }

        [Fact]
        public void AddDoesNotReplace()
        {
            // Arrange
            var targetObject = new
            {
                StringProperty = "A"
            };

            var patchDocument = new JsonPatchDocument();
            patchDocument.Add("StringProperty", "B");

            // Act
            var exception = Assert.Throws<JsonPatchException>(() =>
            {
                patchDocument.ApplyTo(targetObject);
            });

            // Assert
            Assert.Equal("The property at path 'StringProperty' could not be updated.",
                exception.Message);
        }

        [Fact]
        public void RemoveProperty_ShouldFail()
        {
            // Arrange
            dynamic targetObject = new
            {
                Test = 1
            };

            var patchDocument = new JsonPatchDocument();
            patchDocument.Remove("Test");

            // Act
            var exception = Assert.Throws<JsonPatchException>(() =>
            {
                patchDocument.ApplyTo(targetObject);
            });

            // Assert
            Assert.Equal("The property at path 'Test' could not be updated.",
                exception.Message);
        }

        [Fact]
        public void ReplaceProperty_ShouldFail()
        {
            // Arrange
            var targetObject = new
            {
                StringProperty = "A",
                AnotherStringProperty = "B"
            };

            var patchDocument = new JsonPatchDocument();
            patchDocument.Replace("StringProperty", "AnotherStringProperty");

            // Act
            var exception = Assert.Throws<JsonPatchException>(() =>
            {
                patchDocument.ApplyTo(targetObject);
            });

            // Assert
            Assert.Equal("The property at path 'StringProperty' could not be updated.",
                exception.Message);
        }

        [Fact]
        public void MoveProperty_ShouldFail()
        {
            // Arrange
            var targetObject = new
            {
                StringProperty = "A",
                AnotherStringProperty = "B"
            };

            var patchDocument = new JsonPatchDocument();
            patchDocument.Move("StringProperty", "AnotherStringProperty");

            // Act
            var exception = Assert.Throws<JsonPatchException>(() =>
            {
                patchDocument.ApplyTo(targetObject);
            });

            // Assert
            Assert.Equal("The property at path 'StringProperty' could not be updated.",
                exception.Message);
        }

        [Fact]
        public void TestStringProperty_IsSuccessful()
        {
            // Arrange
            var targetObject = new
            {
                StringProperty = "A",
                AnotherStringProperty = "B"
            };

            var patchDocument = new JsonPatchDocument();
            patchDocument.Test("StringProperty", "A");

            // Act & Assert
            patchDocument.ApplyTo(targetObject);
        }

        [Fact]
        public void TestStringProperty_Fails()
        {
            // Arrange
            var targetObject = new
            {
                StringProperty = "A",
                AnotherStringProperty = "B"
            };

            var patchDocument = new JsonPatchDocument();
            patchDocument.Test("StringProperty", "B");

            // Act
            var exception = Assert.Throws<JsonPatchException>(() =>
            {
                patchDocument.ApplyTo(targetObject);
            });

            // Assert
            Assert.Equal("The current value 'A' at path 'StringProperty' is not equal to the test value 'B'.",
                exception.Message);
        }
    }
}
