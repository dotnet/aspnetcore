// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Reflection.Emit;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.Mvc.DependencyInjection;

public class MvcCoreBuilderExtensionsTest
{
    [Fact]
    public void AddApplicationPart_AddsAnApplicationPart_ToTheListOfPartsOnTheBuilder()
    {
        // Arrange
        var manager = new ApplicationPartManager();
        var builder = new MvcCoreBuilder(Mock.Of<IServiceCollection>(), manager);
        var assembly = typeof(MvcCoreBuilder).Assembly;

        // Act
        var result = builder.AddApplicationPart(assembly);

        // Assert
        Assert.Same(result, builder);
        var part = Assert.Single(builder.PartManager.ApplicationParts);
        var assemblyPart = Assert.IsType<AssemblyPart>(part);
        Assert.Equal(assembly, assemblyPart.Assembly);
    }

    [Fact]
    public void AddApplicationPart_UsesPartFactory_ToRetrieveApplicationParts()
    {
        // Arrange
        var manager = new ApplicationPartManager();
        var builder = new MvcCoreBuilder(Mock.Of<IServiceCollection>(), manager);
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Test"), AssemblyBuilderAccess.Run);

        var attribute = new CustomAttributeBuilder(typeof(ProvideApplicationPartFactoryAttribute).GetConstructor(
            new[] { typeof(Type) }),
            new[] { typeof(TestApplicationPartFactory) });

        assembly.SetCustomAttribute(attribute);

        // Act
        builder.AddApplicationPart(assembly);

        // Assert
        var part = Assert.Single(builder.PartManager.ApplicationParts);
        Assert.Same(TestApplicationPartFactory.TestPart, part);
    }

    [Fact]
    public void ConfigureApplicationParts_InvokesSetupAction()
    {
        // Arrange
        var builder = new MvcCoreBuilder(
            Mock.Of<IServiceCollection>(),
            new ApplicationPartManager());

        var part = new TestApplicationPart();

        // Act
        var result = builder.ConfigureApplicationPartManager(manager =>
        {
            manager.ApplicationParts.Add(part);
        });

        // Assert
        Assert.Same(result, builder);
        Assert.Equal(new ApplicationPart[] { part }, builder.PartManager.ApplicationParts.ToArray());
    }

    [Fact]
    public void ConfigureApiBehaviorOptions_InvokesSetupAction()
    {
        // Arrange
        var serviceCollection = new ServiceCollection()
            .AddOptions();

        var builder = new MvcCoreBuilder(
            serviceCollection,
            new ApplicationPartManager());

        var part = new TestApplicationPart();

        // Act
        var result = builder.ConfigureApiBehaviorOptions(o =>
        {
            o.SuppressMapClientErrors = true;
        });

        // Assert
        var options = serviceCollection.
            BuildServiceProvider()
            .GetRequiredService<IOptions<ApiBehaviorOptions>>()
            .Value;
        Assert.True(options.SuppressMapClientErrors);
    }

    private class TestApplicationPartFactory : ApplicationPartFactory
    {
        public static readonly ApplicationPart TestPart = Mock.Of<ApplicationPart>();

        public override IEnumerable<ApplicationPart> GetApplicationParts(Assembly assembly)
        {
            yield return TestPart;
        }
    }
}
