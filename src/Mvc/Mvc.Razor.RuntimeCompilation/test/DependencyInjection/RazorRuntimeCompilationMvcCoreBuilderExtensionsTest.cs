// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Shared;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection;

public class RazorRuntimeCompilationMvcCoreBuilderExtensionsTest
{
    [Fact]
    public void AddRazorRuntimeCompilationOverloads_AreObsolete()
    {
#pragma warning disable ASPDEPR003 // Type or member is obsolete
        var extensionTypes = new[]
        {
            typeof(RazorRuntimeCompilationMvcBuilderExtensions),
            typeof(RazorRuntimeCompilationMvcCoreBuilderExtensions),
        };
#pragma warning restore ASPDEPR003 // Type or member is obsolete

        var methods = extensionTypes.SelectMany(type => type.GetMethods(
            BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly));

        foreach (var method in methods)
        {
            var attribute = method.GetCustomAttribute<ObsoleteAttribute>();

            Assert.NotNull(attribute);
            Assert.Equal("ASPDEPR003", attribute.DiagnosticId);
            Assert.Equal(Obsoletions.AspNetCoreDeprecate003Url, attribute.UrlFormat);
            Assert.Contains("use Hot Reload instead", attribute.Message);
        }
    }

    [Fact]
    public void AddServices_ReplacesRazorViewCompiler()
    {
        // Arrange
        var services = new ServiceCollection()
            .AddSingleton<IViewCompilerProvider, DefaultViewCompilerProvider>();

        // Act
#pragma warning disable ASPDEPR003 // Type or member is obsolete
        RazorRuntimeCompilationMvcCoreBuilderExtensions.AddServices(services);
#pragma warning restore ASPDEPR003 // Type or member is obsolete

        // Assert
        var serviceDescriptor = Assert.Single(services, service => service.ServiceType == typeof(IViewCompilerProvider));
        Assert.Equal(typeof(RuntimeViewCompilerProvider), serviceDescriptor.ImplementationType);
    }

    [Fact]
    public void AddServices_ReplacesActionDescriptorProvider()
    {
        // Arrange
        var services = new ServiceCollection()
            .AddSingleton<IActionDescriptorProvider, CompiledPageActionDescriptorProvider>();

        // Act
#pragma warning disable ASPDEPR003 // Type or member is obsolete
        RazorRuntimeCompilationMvcCoreBuilderExtensions.AddServices(services);
#pragma warning restore ASPDEPR003 // Type or member is obsolete

        // Assert
        var serviceDescriptor = Assert.Single(services, service => service.ServiceType == typeof(IActionDescriptorProvider));
        Assert.Equal(typeof(PageActionDescriptorProvider), serviceDescriptor.ImplementationType);

        serviceDescriptor = Assert.Single(services, service => service.ServiceType == typeof(MatcherPolicy));
        Assert.Equal(typeof(PageLoaderMatcherPolicy), serviceDescriptor.ImplementationType);
    }
}
