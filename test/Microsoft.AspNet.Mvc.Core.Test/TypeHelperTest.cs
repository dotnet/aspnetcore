// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class TypeHelperTest
    {
        [Fact]
        public void ObjectToDictionary_WithNullObject_ReturnsEmptyDictionary()
        {
            // Arrange
            object value = null;

            // Act
            var dictValues = TypeHelper.ObjectToDictionary(value);

            // Assert
            Assert.NotNull(dictValues);
            Assert.Equal(0, dictValues.Count);
        }

        [Fact]
        public void ObjectToDictionary_WithPlainObjectType_ReturnsEmptyDictionary()
        {
            // Arrange
            var value = new object();

            // Act
            var dictValues = TypeHelper.ObjectToDictionary(value);

            // Assert
            Assert.NotNull(dictValues);
            Assert.Equal(0, dictValues.Count);
        }

        [Fact]
        public void ObjectToDictionary_WithPrimitiveType_LooksUpPublicProperties()
        {
            // Arrange
            var value = "test";

            // Act
            var dictValues = TypeHelper.ObjectToDictionary(value);

            // Assert
            Assert.NotNull(dictValues);
            Assert.Equal(1, dictValues.Count);
            Assert.Equal(4, dictValues["Length"]);
        }

        [Fact]
        public void ObjectToDictionary_WithAnonymousType_LooksUpProperties()
        {
            // Arrange
            var value = new { test = "value", other = 1 };

            // Act
            var dictValues = TypeHelper.ObjectToDictionary(value);

            // Assert
            Assert.NotNull(dictValues);
            Assert.Equal(2, dictValues.Count);
            Assert.Equal("value", dictValues["test"]);
            Assert.Equal(1, dictValues["other"]);
        }

        [Fact]
        public void ObjectToDictionary_ReturnsCaseInsensitiveDictionary()
        {
            // Arrange
            var value = new { TEST = "value", oThEr = 1 };

            // Act
            var dictValues = TypeHelper.ObjectToDictionary(value);

            // Assert
            Assert.NotNull(dictValues);
            Assert.Equal(2, dictValues.Count);
            Assert.Equal("value", dictValues["test"]);
            Assert.Equal(1, dictValues["other"]);
        }

        [Fact]
        public void ObjectToDictionary_ReturnsInheritedProperties()
        {
            // Arrange
            var value = new ThreeDPoint() { X = 5, Y = 10, Z = 17 };

            // Act
            var dictValues = TypeHelper.ObjectToDictionary(value);

            // Assert
            Assert.NotNull(dictValues);
            Assert.Equal(3, dictValues.Count);
            Assert.Equal(5, dictValues["X"]);
            Assert.Equal(10, dictValues["Y"]);
            Assert.Equal(17, dictValues["Z"]);
        }

        private class Point
        {
            public int X { get; set; }
            public int Y { get; set; }
        }

        private class ThreeDPoint : Point
        {
            public int Z { get; set; }
        }
    }
}
