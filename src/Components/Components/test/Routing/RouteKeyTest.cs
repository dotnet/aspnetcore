// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Components.Routing
{
    public class RouteKeyTest
    {
        [Fact]
        public void RouteKey_Default_Equality()
        {
            // Arrange
            var key1 = default(RouteKey);
            var key2 = default(RouteKey);

            // Act & Assert
            Assert.Equal(key1.GetHashCode(), key2.GetHashCode());
            Assert.True(key1.Equals(key2));
        }

        [Fact]
        public void RouteKey_WithNoAdditionalAssemblies_Equality()
        {
            // Arrange
            var key1 = new RouteKey(typeof(string).Assembly, null);
            var key2 = new RouteKey(typeof(string).Assembly, null);

            // Act & Assert
            Assert.Equal(key1.GetHashCode(), key2.GetHashCode());
            Assert.True(key1.Equals(key2));
        }

        [Fact]
        public void RouteKey_WithNoAdditionalAssemblies_DifferentAssemblies()
        {
            // Arrange
            var key1 = new RouteKey(typeof(string).Assembly, null);
            var key2 = new RouteKey(typeof(ComponentBase).Assembly, null);

            // Act & Assert
            Assert.NotEqual(key1.GetHashCode(), key2.GetHashCode());
            Assert.False(key1.Equals(key2));
        }

        [Fact]
        public void RouteKey_DefaultAgainstNonDefault()
        {
            // Arrange
            var key1 = default(RouteKey);
            var key2 = new RouteKey(typeof(string).Assembly, null);

            // Act & Assert
            Assert.NotEqual(key1.GetHashCode(), key2.GetHashCode());
            Assert.False(key1.Equals(key2));
        }

        [Fact]
        public void RouteKey_WithAdditionalAssemblies()
        {
            // Arrange
            var key1 = new RouteKey(typeof(string).Assembly, new[] { typeof(ComponentBase).Assembly, GetType().Assembly });
            var key2 = new RouteKey(typeof(string).Assembly, new[] { typeof(ComponentBase).Assembly, GetType().Assembly });

            // Act & Assert
            Assert.Equal(key1.GetHashCode(), key2.GetHashCode());
            Assert.True(key1.Equals(key2));
        }

        [Fact]
        public void RouteKey_WithAdditionalAssemblies_DifferentOrder()
        {
            // Arrange
            var key1 = new RouteKey(typeof(string).Assembly, new[] { typeof(ComponentBase).Assembly, GetType().Assembly });
            var key2 = new RouteKey(typeof(string).Assembly, new[] { GetType().Assembly, typeof(ComponentBase).Assembly });

            // Act & Assert
            Assert.Equal(key1.GetHashCode(), key2.GetHashCode());
            Assert.True(key1.Equals(key2));
        }

        [Fact]
        public void RouteKey_WithAdditionalAssemblies_DifferentAppAssemblies()
        {
            // Arrange
            var key1 = new RouteKey(typeof(string).Assembly, new[] { GetType().Assembly });
            var key2 = new RouteKey(typeof(ComponentBase).Assembly, new[] { GetType().Assembly, });

            // Act & Assert
            Assert.NotEqual(key1.GetHashCode(), key2.GetHashCode());
            Assert.False(key1.Equals(key2));
        }

        [Fact]
        public void RouteKey_WithAdditionalAssemblies_DifferentAdditionalAssemblies()
        {
            // Arrange
            var key1 = new RouteKey(typeof(ComponentBase).Assembly, new[] { typeof(object).Assembly });
            var key2 = new RouteKey(typeof(ComponentBase).Assembly, new[] { GetType().Assembly, });

            // Act & Assert
            Assert.Equal(key1.GetHashCode(), key2.GetHashCode());
            Assert.False(key1.Equals(key2));
        }
    }
}
