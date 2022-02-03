// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.Extensions.DependencyInjection;

public class RazorRuntimeCompilationMvcCoreBuilderExtensionsTest
{
    [Fact]
    public void AddServices_ReplacesRazorViewCompiler()
    {
        // Arrange
        var services = new ServiceCollection()
            .AddSingleton<IViewCompilerProvider, DefaultViewCompilerProvider>();

        // Act
        RazorRuntimeCompilationMvcCoreBuilderExtensions.AddServices(services);

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
        RazorRuntimeCompilationMvcCoreBuilderExtensions.AddServices(services);

        // Assert
        var serviceDescriptor = Assert.Single(services, service => service.ServiceType == typeof(IActionDescriptorProvider));
        Assert.Equal(typeof(PageActionDescriptorProvider), serviceDescriptor.ImplementationType);

        serviceDescriptor = Assert.Single(services, service => service.ServiceType == typeof(MatcherPolicy));
        Assert.Equal(typeof(PageLoaderMatcherPolicy), serviceDescriptor.ImplementationType);
    }
}
