// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Mvc.Formatters.Xml
{
    public class WrapperProviderFactoryTest
    {
        [Fact]
        public void GetProvider_ReturnsNull_IfTypeDoesNotMatch()
        {
            // Arrange
            var provider = new WrapperProviderFactory(
                typeof(ProblemDetails),
                typeof(ProblemDetailsWrapper),
                _ => null);
            var context = new WrapperProviderContext(typeof(SerializableError), isSerialization: true);

            // Act
            var result = provider.GetProvider(context);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetProvider_ReturnsNull_IfTypeIsSubtype()
        {
            // Arrange
            var provider = new WrapperProviderFactory(
                typeof(ProblemDetails),
                typeof(ProblemDetailsWrapper),
                _ => null);
            var context = new WrapperProviderContext(typeof(ValidationProblemDetails), isSerialization: true);

            // Act
            var result = provider.GetProvider(context);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetProvider_ReturnsValue_IfTypeMatches()
        {
            // Arrange
            var expected = new object();
            var providerFactory = new WrapperProviderFactory(
                typeof(ProblemDetails),
                typeof(ProblemDetailsWrapper),
                _ => expected);
            var context = new WrapperProviderContext(typeof(ProblemDetails), isSerialization: true);

            // Act
            var provider = providerFactory.GetProvider(context);
            var result = provider.Wrap(new ProblemDetails());

            // Assert
            Assert.Same(expected, result);
        }
    }
}
