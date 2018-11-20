// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Http.Abstractions.Tests
{
    public class FragmentStringTests
    {
        [Fact]
        public void Equals_EmptyFragmentStringAndDefaultFragmentString()
        {
            // Act and Assert
            Assert.Equal(default(FragmentString), FragmentString.Empty);
            Assert.Equal(default(FragmentString), FragmentString.Empty);
            // explicitly checking == operator
            Assert.True(FragmentString.Empty == default(FragmentString));
            Assert.True(default(FragmentString) == FragmentString.Empty);
        }

        [Fact]
        public void NotEquals_DefaultFragmentStringAndNonNullFragmentString()
        {
            // Arrange
            var fragmentString = new FragmentString("#col=1");

            // Act and Assert
            Assert.NotEqual(default(FragmentString), fragmentString);
        }

        [Fact]
        public void NotEquals_EmptyFragmentStringAndNonNullFragmentString()
        {
            // Arrange
            var fragmentString = new FragmentString("#col=1");

            // Act and Assert
            Assert.NotEqual(fragmentString, FragmentString.Empty);
        }
    }
}
