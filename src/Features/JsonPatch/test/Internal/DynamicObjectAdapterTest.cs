// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace Microsoft.AspNetCore.JsonPatch.Internal
{
    public class DynamicObjectAdapterTest
    {
        [Fact]
        public void TryAdd_AddsNewProperty()
        {
            // Arrange
            var resolver = new DefaultContractResolver();
            var adapter = new DynamicObjectAdapter(resolver);
            dynamic target = new DynamicTestObject();
            var segment = "NewProperty";

            // Act
            var status = adapter.TryAdd(target, segment, "new", out string errorMessage);

            // Assert
            Assert.True(status);
            Assert.Null(errorMessage);
            Assert.Equal("new", target.NewProperty);
        }

        [Fact]
        public void TryAdd_ReplacesExistingPropertyValue()
        {
            // Arrange
            var resolver = new DefaultContractResolver();
            var adapter = new DynamicObjectAdapter(resolver);
            dynamic target = new DynamicTestObject();
            target.List = new List<int>() { 1, 2, 3 };
            var value = new List<string>() { "stringValue1", "stringValue2" };
            var segment = "List";

            // Act
            var status = adapter.TryAdd(target, segment, value, out string errorMessage);

            // Assert
            Assert.True(status);
            Assert.Null(errorMessage);
            Assert.Equal(value, target.List);
        }

        [Fact]
        public void TryGet_GetsPropertyValue_ForExistingProperty()
        {
            // Arrange
            var resolver = new DefaultContractResolver();
            var adapter = new DynamicObjectAdapter(resolver);
            dynamic target = new DynamicTestObject();
            var segment = "NewProperty";

            // Act 1
            var addStatus = adapter.TryAdd(target, segment, "new", out string errorMessage);

            // Assert 1
            Assert.True(addStatus);
            Assert.Null(errorMessage);
            Assert.Equal("new", target.NewProperty);

            // Act 2
            var getStatus = adapter.TryGet(target, segment, out object getValue, out string getErrorMessage);

            // Assert 2
            Assert.True(getStatus);
            Assert.Null(getErrorMessage);
            Assert.Equal(getValue, target.NewProperty);
        }

        [Fact]
        public void TryGet_ThrowsPathNotFoundException_ForNonExistingProperty()
        {
            // Arrange
            var resolver = new DefaultContractResolver();
            var adapter = new DynamicObjectAdapter(resolver);
            dynamic target = new DynamicTestObject();
            var segment = "NewProperty";

            // Act
            var getStatus = adapter.TryGet(target, segment, out object getValue, out string getErrorMessage);

            // Assert
            Assert.False(getStatus);
            Assert.Null(getValue);
            Assert.Equal($"The target location specified by path segment '{segment}' was not found.", getErrorMessage);
        }

        [Fact]
        public void TryTraverse_FindsNextTarget()
        {
            // Arrange
            var resolver = new DefaultContractResolver();
            var adapter = new DynamicObjectAdapter(resolver);
            dynamic target = new DynamicTestObject();
            target.NestedObject = new DynamicTestObject();
            target.NestedObject.NewProperty = "A";
            var segment = "NestedObject";

            // Act
            var status = adapter.TryTraverse(target, segment, out object nextTarget, out string errorMessage);

            // Assert
            Assert.True(status);
            Assert.Null(errorMessage);
            Assert.Equal(target.NestedObject, nextTarget);
        }

        [Fact]
        public void TryTraverse_ThrowsPathNotFoundException_ForNonExistingProperty()
        {
            // Arrange
            var resolver = new DefaultContractResolver();
            var adapter = new DynamicObjectAdapter(resolver);
            dynamic target = new DynamicTestObject();
            target.NestedObject = new DynamicTestObject();
            var segment = "NewProperty";

            // Act
            var status = adapter.TryTraverse(target.NestedObject, segment, out object nextTarget, out string errorMessage);

            // Assert
            Assert.False(status);
            Assert.Equal($"The target location specified by path segment '{segment}' was not found.", errorMessage);
        }

        [Fact]
        public void TryReplace_RemovesExistingValue_BeforeAddingNewValue()
        {
            // Arrange
            var resolver = new DefaultContractResolver();
            var adapter = new DynamicObjectAdapter(resolver);
            dynamic target = new WriteOnceDynamicTestObject();
            target.NewProperty = new object();
            var segment = "NewProperty";

            // Act
            var status = adapter.TryReplace(target, segment, "new", out string errorMessage);

            // Assert
            Assert.True(status);
            Assert.Null(errorMessage);
            Assert.Equal("new", target.NewProperty);
        }

        [Fact]
        public void TryReplace_ThrowsPathNotFoundException_ForNonExistingProperty()
        {
            // Arrange
            var resolver = new DefaultContractResolver();
            var adapter = new DynamicObjectAdapter(resolver);
            dynamic target = new DynamicTestObject();
            var segment = "NewProperty";

            // Act
            var status = adapter.TryReplace(target, segment, "test", out string errorMessage);

            // Assert
            Assert.False(status);
            Assert.Equal($"The target location specified by path segment '{segment}' was not found.", errorMessage);
        }

        [Fact]
        public void TryReplace_ThrowsPropertyInvalidException_IfNewValueIsNotTheSameTypeAsInitialValue()
        {
            // Arrange
            var resolver = new DefaultContractResolver();
            var adapter = new DynamicObjectAdapter(resolver);
            dynamic target = new DynamicTestObject();
            target.NewProperty = 1;
            var segment = "NewProperty";

            // Act
            var status = adapter.TryReplace(target, segment, "test", out string errorMessage);

            // Assert
            Assert.False(status);
            Assert.Equal($"The value 'test' is invalid for target location.", errorMessage);
        }

        [Theory]
        [InlineData(1, 0)]
        [InlineData("new", null)]
        public void TryRemove_SetsPropertyToDefaultOrNull(object value, object expectedValue)
        {
            // Arrange
            var resolver = new DefaultContractResolver();
            var adapter = new DynamicObjectAdapter(resolver);
            dynamic target = new DynamicTestObject();
            var segment = "NewProperty";

            // Act 1
            var addStatus = adapter.TryAdd(target, segment, value, out string errorMessage);

            // Assert 1
            Assert.True(addStatus);
            Assert.Null(errorMessage);
            Assert.Equal(value, target.NewProperty);

            // Act 2
            var removeStatus = adapter.TryRemove(target, segment, out string removeErrorMessage);

            // Assert 2
            Assert.True(removeStatus);
            Assert.Null(removeErrorMessage);
            Assert.Equal(expectedValue, target.NewProperty);
        }

        [Fact]
        public void TryRemove_ThrowsPathNotFoundException_ForNonExistingProperty()
        {
            // Arrange
            var resolver = new DefaultContractResolver();
            var adapter = new DynamicObjectAdapter(resolver);
            dynamic target = new DynamicTestObject();
            var segment = "NewProperty";

            // Act
            var removeStatus = adapter.TryRemove(target, segment, out string removeErrorMessage);

            // Assert
            Assert.False(removeStatus);
            Assert.Equal($"The target location specified by path segment '{segment}' was not found.", removeErrorMessage);
        }

        [Fact]
        public void TryTest_DoesNotThrowException_IfTestSuccessful()
        {
            var resolver = new DefaultContractResolver();
            var adapter = new DynamicObjectAdapter(resolver);
            dynamic target = new DynamicTestObject();
            var value = new List<object>()
            {
                "Joana",
                2,
                new Customer("Joana", 25)
            };
            target.NewProperty = value;
            var segment = "NewProperty";

            // Act
            var testStatus = adapter.TryTest(target, segment, value, out string errorMessage);

            // Assert
            Assert.Equal(value, target.NewProperty);
            Assert.True(testStatus);
            Assert.True(string.IsNullOrEmpty(errorMessage), "Expected no error message");
        }

        [Fact]
        public void TryTest_ThrowsJsonPatchException_IfTestFails()
        {
            // Arrange
            var resolver = new DefaultContractResolver();
            var adapter = new DynamicObjectAdapter(resolver);
            dynamic target = new DynamicTestObject();
            target.NewProperty = "Joana";
            var segment = "NewProperty";
            var expectedErrorMessage = $"The current value 'Joana' at path '{segment}' is not equal to the test value 'John'.";

            // Act
            var testStatus = adapter.TryTest(target, segment, "John", out string errorMessage);

            // Assert
            Assert.False(testStatus);
            Assert.Equal(expectedErrorMessage, errorMessage);
        }
    }
}
