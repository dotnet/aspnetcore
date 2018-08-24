// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Mvc.Formatters.Xml
{
    public class WrapperProviderFactoryExtensionsTest
    {
        [Fact]
        public void GetDefaultProviderFactories_GetsFactoriesUsedByInputAndOutputFormatters()
        {
            // Act
            var factoryProviders = WrapperProviderFactoriesExtensions.GetDefaultProviderFactories();

            // Assert
            Assert.Collection(
                factoryProviders,
                factory => Assert.IsType<SerializableErrorWrapperProviderFactory>(factory),
                factory =>
                {
                    var wrapperProviderFactory = Assert.IsType<WrapperProviderFactory>(factory);
                    Assert.Equal(typeof(ProblemDetails), wrapperProviderFactory.DeclaredType);
                    Assert.Equal(typeof(ProblemDetailsWrapper), wrapperProviderFactory.WrappingType);
                },
                factory =>
                {
                    var wrapperProviderFactory = Assert.IsType<WrapperProviderFactory>(factory);
                    Assert.Equal(typeof(ValidationProblemDetails), wrapperProviderFactory.DeclaredType);
                    Assert.Equal(typeof(ValidationProblemDetailsWrapper), wrapperProviderFactory.WrappingType);
                });
        }
    }
}
