// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Xunit;

namespace Microsoft.AspNet.Razor.Test
{
    public class RazorDirectiveAttributeTest
    {
        [Fact]
        public void ConstructorThrowsIfNameIsNullOrEmpty()
        {
            // Act and Assert
            Assert.Throws<ArgumentException>("name", () => new RazorDirectiveAttribute(name: null, value: "blah"));
            Assert.Throws<ArgumentException>("name", () => new RazorDirectiveAttribute(name: "", value: "blah"));
        }

        [Fact]
        public void EnsureRazorDirectiveProperties()
        {
            // Arrange
            var attribute = (AttributeUsageAttribute)typeof(RazorDirectiveAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), inherit: false)
                                                                                     .SingleOrDefault();

            // Assert
            Assert.True(attribute.AllowMultiple);
            Assert.True(attribute.ValidOn == AttributeTargets.Class);
            Assert.True(attribute.Inherited);
        }

        [Fact]
        public void EqualsAndGetHashCodeIgnoresCase()
        {
            // Arrange
            var attribute1 = new RazorDirectiveAttribute("foo", "bar");
            var attribute2 = new RazorDirectiveAttribute("fOo", "BAr");

            // Act
            var hashCode1 = attribute1.GetHashCode();
            var hashCode2 = attribute2.GetHashCode();

            // Assert
            Assert.Equal(attribute1, attribute2);
            Assert.Equal(hashCode1, hashCode2);
        }

        [Fact]
        public void EqualsAndGetHashCodeDoNotThrowIfValueIsNullOrEmpty()
        {
            // Arrange
            var attribute1 = new RazorDirectiveAttribute("foo", null);
            var attribute2 = new RazorDirectiveAttribute("foo", "BAr");

            // Act
            bool result = attribute1.Equals(attribute2);
            var hashCode = attribute1.GetHashCode();

            // Assert
            Assert.False(result);
            // If we've got this far, GetHashCode did not throw
        }

        [Fact]
        public void EqualsAndGetHashCodeReturnDifferentValuesForNullAndEmpty()
        {
            // Arrange
            var attribute1 = new RazorDirectiveAttribute("foo", null);
            var attribute2 = new RazorDirectiveAttribute("foo", "");

            // Act
            bool result = attribute1.Equals(attribute2);
            var hashCode1 = attribute1.GetHashCode();
            var hashCode2 = attribute2.GetHashCode();

            // Assert
            Assert.False(result);
            Assert.NotEqual(hashCode1, hashCode2);
        }
    }
}
