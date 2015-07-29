// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Hosting.Internal;
using Microsoft.Dnx.Runtime.Infrastructure;
using Xunit;

namespace Microsoft.AspNet.Hosting
{
    public class WebHostBuilderTests
    {
        [Fact]
        public void Build_uses_application_for_startup_assembly_by_default()
        {
            var builder = CreateWebHostBuilder();

            var engine = (HostingEngine)builder.Build();

            Assert.Equal("Microsoft.AspNet.Hosting.Tests", engine.StartupAssemblyName);
        }

        [Fact]
        public void Build_honors_UseStartup_with_string()
        {
            var builder = CreateWebHostBuilder();

            var engine = (HostingEngine)builder.UseStartup("MyStartupAssembly").Build();

            Assert.Equal("MyStartupAssembly", engine.StartupAssemblyName);
        }

        private WebHostBuilder CreateWebHostBuilder() => new WebHostBuilder(CallContextServiceLocator.Locator.ServiceProvider);
    }
}
