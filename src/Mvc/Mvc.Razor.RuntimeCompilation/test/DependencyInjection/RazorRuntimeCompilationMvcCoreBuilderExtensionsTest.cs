// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection
{
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

        [Fact]
        public void AddServices_DoesNotPageActionDescriptor_IfItWasNotPreviouslyFound()
        {
            // we want to make sure Page specific featurees are only added if AddRazorPages was called by the user.
            // Arrange
            var services = new ServiceCollection();

            // Act
            RazorRuntimeCompilationMvcCoreBuilderExtensions.AddServices(services);

            // Assert
            Assert.Empty(services.Where(service => service.ServiceType == typeof(IActionDescriptorProvider)));
            Assert.Empty(services.Where(service => service.ServiceType == typeof(MatcherPolicy)));
        }
    }
}
