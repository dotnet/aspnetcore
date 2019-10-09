// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;
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
    }
}
