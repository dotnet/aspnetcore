// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Castle.Core.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Microsoft.AspNetCore.Mvc.Filters;

public class MiddlewareFilterConfigurationProviderTest
{
    [Theory]
    [InlineData(typeof(AbstractType))]
    [InlineData(typeof(NoParameterlessConstructor))]
    [InlineData(typeof(IDisposable))]
    public void CreateConfigureDelegate_ThrowsIfTypeCannotBeInstantiated(Type configurationType)
    {
        // Arrange
        var provider = new MiddlewareFilterConfigurationProvider();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => MiddlewareFilterConfigurationProvider.CreateConfigureDelegate(configurationType));

        // Assert
        Assert.Equal($"Unable to create an instance of type '{configurationType}'. The type specified in configurationType must not be abstract and must have a parameterless constructor.", exception.Message);
    }

    [Fact]
    public void ValidConfigure_DoesNotThrow()
    {
        // Arrange
        var provider = new MiddlewareFilterConfigurationProvider();

        // Act
        var configureDelegate = MiddlewareFilterConfigurationProvider.CreateConfigureDelegate(typeof(ValidConfigure_WithNoEnvironment));

        // Assert
        Assert.NotNull(configureDelegate);
    }

    [Fact]
    public void ValidConfigure_AndAdditionalServices_DoesNotThrow()
    {
        // Arrange
        var loggerFactory = Mock.Of<ILoggerFactory>();
        var services = new ServiceCollection();
        services.AddSingleton(loggerFactory);
        services.AddSingleton(Mock.Of<IWebHostEnvironment>());
        var applicationBuilder = GetApplicationBuilder(services);
        var provider = new MiddlewareFilterConfigurationProvider();

        // Act
        var configureDelegate = MiddlewareFilterConfigurationProvider.CreateConfigureDelegate(typeof(ValidConfigure_WithNoEnvironment_AdditionalServices));

        // Assert
        Assert.NotNull(configureDelegate);
    }

    [Fact]
    public void InvalidType_NoConfigure_Throws()
    {
        // Arrange
        var type = typeof(InvalidType_NoConfigure);
        var provider = new MiddlewareFilterConfigurationProvider();
        var expected = $"A public method named 'Configure' could not be found in the '{type.FullName}' type.";

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            MiddlewareFilterConfigurationProvider.CreateConfigureDelegate(type);
        });
        Assert.Equal(expected, exception.Message);
    }

    [Fact]
    public void InvalidType_NoPublicConfigure_Throws()
    {
        // Arrange
        var type = typeof(InvalidType_NoPublic_Configure);
        var provider = new MiddlewareFilterConfigurationProvider();
        var expected = $"A public method named 'Configure' could not be found in the '{type.FullName}' type.";

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            MiddlewareFilterConfigurationProvider.CreateConfigureDelegate(type);
        });
        Assert.Equal(expected, exception.Message);
    }

    private IApplicationBuilder GetApplicationBuilder(ServiceCollection services = null)
    {
        if (services == null)
        {
            services = new ServiceCollection();
        }
        var serviceProvider = services.BuildServiceProvider();

        var applicationBuilder = new Mock<IApplicationBuilder>();
        applicationBuilder
            .SetupGet(a => a.ApplicationServices)
            .Returns(serviceProvider);

        return applicationBuilder.Object;
    }

    private class ValidConfigure_WithNoEnvironment
    {
        public void Configure(IApplicationBuilder appBuilder) { }
    }

    private class ValidConfigure_WithNoEnvironment_AdditionalServices
    {
        public void Configure(
            IApplicationBuilder appBuilder,
            IWebHostEnvironment hostingEnvironment,
            ILoggerFactory loggerFactory)
        {
            ArgumentNullException.ThrowIfNull(hostingEnvironment);
            ArgumentNullException.ThrowIfNull(loggerFactory);
        }
    }

    private class ValidConfigure_WithEnvironment
    {
        public void ConfigureProduction(IApplicationBuilder appBuilder) { }
    }

    private class ValidConfigure_WithEnvironment_AdditionalServices
    {
        public void ConfigureProduction(
            IApplicationBuilder appBuilder,
            IWebHostEnvironment hostingEnvironment,
            ILoggerFactory loggerFactory)
        {
            ArgumentNullException.ThrowIfNull(hostingEnvironment);
            ArgumentNullException.ThrowIfNull(loggerFactory);
        }
    }

    private class MultipleConfigureWithEnvironments
    {
        public void ConfigureDevelopment(IApplicationBuilder appBuilder)
        {

        }

        public void ConfigureProduction(IApplicationBuilder appBuilder)
        {

        }
    }

    private class InvalidConfigure_NoParameters
    {
        public void Configure()
        {

        }
    }

    private class InvalidType_NoConfigure
    {
        public void Foo(IApplicationBuilder appBuilder)
        {

        }
    }

    private class InvalidType_NoPublic_Configure
    {
        private void Configure(IApplicationBuilder appBuilder)
        {

        }
    }

    private abstract class AbstractType
    {
    }

    private class NoParameterlessConstructor
    {
        public NoParameterlessConstructor(object a)
        {
        }
    }
}
