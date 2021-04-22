// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language
{
    public class ItemCollectionTest
    {
        [Fact]
        public void Get_MissingValueReturnsNull()
        {
            // Arrange
            var items = new ItemCollection();

            // Act
            var value = items["foo"];

            // Assert
            Assert.Null(value);
        }

        [Fact]
        public void GetAndSet_ReturnsValue()
        {
            // Arrange
            var items = new ItemCollection();

            var expected = "bar";
            items["foo"] = expected;

            // Act
            var value = items["foo"];

            // Assert
            Assert.Same(expected, value);
        }

        [Fact]
        public void Set_CanSetValueToNull()
        {
            // Arrange
            var items = new ItemCollection();
            
            items["foo"] = "bar";

            // Act
            items["foo"] = null;

            // Assert
            Assert.Null(items["foo"]);
        }
    }
}
