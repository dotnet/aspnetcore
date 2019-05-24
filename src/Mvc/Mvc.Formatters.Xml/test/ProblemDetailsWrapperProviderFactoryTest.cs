// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Mvc.Formatters.Xml
{
    public class ProblemDetailsWrapperProviderFactoryTest
    {
        [Fact]
        public void GetProvider_ReturnsNull_IfTypeDoesNotMatch()
        {
            // Arrange
            var providerFactory = new ProblemDetailsWrapperProviderFactory();
            var context = new WrapperProviderContext(typeof(SerializableError), isSerialization: true);

            // Act
            var provider = providerFactory.GetProvider(context);

            // Assert
            Assert.Null(provider);
        }

        [Fact]
        public void GetProvider_ReturnsWrapper_ForProblemDetails()
        {
            // Arrange
            var providerFactory = new ProblemDetailsWrapperProviderFactory();
            var instance = new ProblemDetails();
            var context = new WrapperProviderContext(instance.GetType(), isSerialization: true);

            // Act
            var provider = providerFactory.GetProvider(context);

            // Assert
            var result = provider.Wrap(instance);
            var wrapper = Assert.IsType<ProblemDetailsWrapper>(result);
            Assert.Same(instance, wrapper.ProblemDetails);
        }

        [Fact]
        public void GetProvider_ReturnsWrapper_ForValidationProblemDetails()
        {
            // Arrange
            var providerFactory = new ProblemDetailsWrapperProviderFactory();
            var instance = new ValidationProblemDetails();
            var context = new WrapperProviderContext(instance.GetType(), isSerialization: true);

            // Act
            var provider = providerFactory.GetProvider(context);

            // Assert
            var result = provider.Wrap(instance);
            var wrapper = Assert.IsType<ValidationProblemDetailsWrapper>(result);
            Assert.Same(instance, wrapper.ProblemDetails);
        }

        [Fact]
        public void GetProvider_ReturnsNull_ForCustomProblemDetails()
        {
            // Arrange
            var providerFactory = new ProblemDetailsWrapperProviderFactory();
            var instance = new CustomProblemDetails();
            var context = new WrapperProviderContext(instance.GetType(), isSerialization: true);

            // Act
            var provider = providerFactory.GetProvider(context);

            // Assert
            Assert.Null(provider);
        }

        private class CustomProblemDetails : ProblemDetails { }
    }
}
