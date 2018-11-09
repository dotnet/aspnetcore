// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.JsonPatch.IntegrationTests
{
    public class SimpleObjectIntegrationTest
    {
        [Fact]
        public void TestDoubleValueProperty()
        {
            // Arrange
            var targetObject = new SimpleObject()
            {
                DoubleValue = 9.8
            };

            var patchDocument = new JsonPatchDocument();
            patchDocument.Test("DoubleValue", 9.8);

            // Act & Assert
            patchDocument.ApplyTo(targetObject);
        }

        [Fact]
        public void CopyStringProperty_ToAnotherStringProperty()
        {
            // Arrange
            var targetObject = new SimpleObject()
            {
                StringProperty = "A",
                AnotherStringProperty = "B"
            };

            var patchDocument = new JsonPatchDocument();
            patchDocument.Copy("StringProperty", "AnotherStringProperty");

            // Act
            patchDocument.ApplyTo(targetObject);

            // Assert
            Assert.Equal("A", targetObject.AnotherStringProperty);
        }

        [Fact]
        public void MoveIntegerProperty_ToAnotherIntegerProperty()
        {
            // Arrange
            var targetObject = new SimpleObject()
            {
                IntegerValue = 2,
                AnotherIntegerValue = 3
            };

            var patchDocument = new JsonPatchDocument();
            patchDocument.Move("IntegerValue", "AnotherIntegerValue");

            // Act
            patchDocument.ApplyTo(targetObject);

            // Assert
            Assert.Equal(2, targetObject.AnotherIntegerValue);
            Assert.Equal(0, targetObject.IntegerValue);
        }

        [Fact]
        public void RemoveDecimalPropertyValue()
        {
            // Arrange
            var targetObject = new SimpleObject()
            {
                DecimalValue = 9.8M
            };

            var patchDocument = new JsonPatchDocument();
            patchDocument.Remove("DecimalValue");

            // Act
            patchDocument.ApplyTo(targetObject);

            // Assert
            Assert.Equal(0, targetObject.DecimalValue);
        }

        [Fact]
        public void ReplaceGuid()
        {
            // Arrange
            var targetObject = new SimpleObject()
            {
                GuidValue = Guid.NewGuid()
            };

            var newGuid = Guid.NewGuid();
            var patchDocument = new JsonPatchDocument();
            patchDocument.Replace("GuidValue", newGuid);

            // Act
            patchDocument.ApplyTo(targetObject);

            // Assert
            Assert.Equal(newGuid, targetObject.GuidValue);
        }

        [Fact]
        public void AddReplacesGuid()
        {
            // Arrange
            var targetObject = new SimpleObject()
            {
                GuidValue = Guid.NewGuid()
            };

            var newGuid = Guid.NewGuid();
            var patchDocument = new JsonPatchDocument();
            patchDocument.Add("GuidValue", newGuid);

            // Act
            patchDocument.ApplyTo(targetObject);

            // Assert
            Assert.Equal(newGuid, targetObject.GuidValue);
        }

    }
}
