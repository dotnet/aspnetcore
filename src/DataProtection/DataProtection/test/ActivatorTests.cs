// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.DataProtection.Internal;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Microsoft.AspNetCore.DataProtection;

public class ActivatorTests
{
    [Fact]
    public void CreateInstance_WithServiceProvider_PrefersParameterfulCtor()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        var services = serviceCollection.BuildServiceProvider();
        var activator = services.GetActivator();

        // Act
        var retVal1 = (ClassWithParameterlessCtor)activator.CreateInstance<object>(typeof(ClassWithParameterlessCtor).AssemblyQualifiedName);
        var retVal2 = (ClassWithServiceProviderCtor)activator.CreateInstance<object>(typeof(ClassWithServiceProviderCtor).AssemblyQualifiedName);
        var retVal3 = (ClassWithBothCtors)activator.CreateInstance<object>(typeof(ClassWithBothCtors).AssemblyQualifiedName);

        // Assert
        Assert.NotNull(services);
        Assert.NotNull(retVal1);
        Assert.NotNull(retVal2);
        Assert.Same(services, retVal2.Services);
        Assert.NotNull(retVal3);
        Assert.False(retVal3.ParameterlessCtorCalled);
        Assert.Same(services, retVal3.Services);
    }

    [Fact]
    public void CreateInstance_WithoutServiceProvider_PrefersParameterlessCtor()
    {
        // Arrange
        var activator = ((IServiceProvider)null).GetActivator();

        // Act
        var retVal1 = (ClassWithParameterlessCtor)activator.CreateInstance<object>(typeof(ClassWithParameterlessCtor).AssemblyQualifiedName);
        var retVal2 = (ClassWithServiceProviderCtor)activator.CreateInstance<object>(typeof(ClassWithServiceProviderCtor).AssemblyQualifiedName);
        var retVal3 = (ClassWithBothCtors)activator.CreateInstance<object>(typeof(ClassWithBothCtors).AssemblyQualifiedName);

        // Assert
        Assert.NotNull(retVal1);
        Assert.NotNull(retVal2);
        Assert.Null(retVal2.Services);
        Assert.NotNull(retVal3);
        Assert.True(retVal3.ParameterlessCtorCalled);
        Assert.Null(retVal3.Services);
    }

    [Fact]
    public void CreateInstance_TypeDoesNotImplementInterface_ThrowsInvalidCast()
    {
        // Arrange
        var activator = ((IServiceProvider)null).GetActivator();

        // Act & assert
        var ex = Assert.Throws<InvalidCastException>(
            () => activator.CreateInstance<IDisposable>(typeof(ClassWithParameterlessCtor).AssemblyQualifiedName));
        Assert.Equal(Resources.FormatTypeExtensions_BadCast(typeof(IDisposable).AssemblyQualifiedName, typeof(ClassWithParameterlessCtor).AssemblyQualifiedName), ex.Message);
    }

    [Fact]
    public void GetActivator_ServiceProviderHasActivator_ReturnsSameInstance()
    {
        // Arrange
        var expectedActivator = new Mock<IActivator>().Object;
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IActivator>(expectedActivator);

        // Act
        var actualActivator = serviceCollection.BuildServiceProvider().GetActivator();

        // Assert
        Assert.Same(expectedActivator, actualActivator);
    }

    private class ClassWithParameterlessCtor
    {
    }

    private class ClassWithServiceProviderCtor
    {
        public readonly IServiceProvider Services;

        public ClassWithServiceProviderCtor(IServiceProvider services)
        {
            Services = services;
        }
    }

    private class ClassWithBothCtors
    {
        public readonly IServiceProvider Services;
        public readonly bool ParameterlessCtorCalled;

        public ClassWithBothCtors()
        {
            ParameterlessCtorCalled = true;
            Services = null;
        }

        public ClassWithBothCtors(IServiceProvider services)
        {
            ParameterlessCtorCalled = false;
            Services = services;
        }
    }
}
