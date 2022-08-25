// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace Microsoft.AspNetCore.Http.Extensions.Tests;

public class ProblemDetailsServiceCollectionExtensionsTest
{
    [Fact]
    public void AddProblemDetails_AddsNeededServices()
    {
        // Arrange
        var collection = new ServiceCollection();

        // Act
        collection.AddProblemDetails();

        // Assert
        Assert.Single(collection, (sd) => sd.ServiceType == typeof(IProblemDetailsService) && sd.ImplementationType == typeof(ProblemDetailsService));
        Assert.Single(collection, (sd) => sd.ServiceType == typeof(IProblemDetailsWriter) && sd.ImplementationType == typeof(DefaultProblemDetailsWriter));
    }

    [Fact]
    public void AddProblemDetails_AllowMultipleWritersRegistration()
    {
        // Arrange
        var collection = new ServiceCollection();
        var expectedCount = 2;
        var mockWriter = Mock.Of<IProblemDetailsWriter>();
        collection.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IProblemDetailsWriter), mockWriter));

        // Act
        collection.AddProblemDetails();

        // Assert
        var serviceDescriptors = collection.Where(serviceDescriptor => serviceDescriptor.ServiceType == typeof(IProblemDetailsWriter));
        Assert.True(
            (expectedCount == serviceDescriptors.Count()),
            $"Expected service type '{typeof(IProblemDetailsWriter)}' to be registered {expectedCount}" +
            $" time(s) but was actually registered {serviceDescriptors.Count()} time(s).");
    }

    [Fact]
    public void AddProblemDetails_KeepCustomRegisteredService()
    {
        // Arrange
        var collection = new ServiceCollection();
        var customService = Mock.Of<IProblemDetailsService>();
        collection.AddSingleton(typeof(IProblemDetailsService), customService);

        // Act
        collection.AddProblemDetails();

        // Assert
        var service = Assert.Single(collection, (sd) => sd.ServiceType == typeof(IProblemDetailsService));
        Assert.Same(customService, service.ImplementationInstance);
    }
}
