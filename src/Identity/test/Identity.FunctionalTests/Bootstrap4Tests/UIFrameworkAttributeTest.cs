// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Identity.DefaultUI.WebSite;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Identity.FunctionalTests.Bootstrap4Tests
{
    public class UIFramewrokAttributeTest : IClassFixture<WebApplicationFactory<Startup>>
    {
        public UIFramewrokAttributeTest(WebApplicationFactory<Startup> factory)
        {
            Factory = factory;
        }

        public WebApplicationFactory<Startup> Factory { get; }

        [Fact]
        public void DefaultWebSite_DefaultsToBootstrap5()
        {
            var hasV5Part = false;
            var hasV4Part = false;
            var factory = Factory.WithWebHostBuilder(
                whb => whb.ConfigureServices(
                    services => services.AddMvc().ConfigureApplicationPartManager(
                        apm => (hasV4Part, hasV5Part) = (HasPart(apm, "V4"), HasPart(apm, "V5")))));

            // Act
            var client = factory.CreateClient();

            // Assert
            Assert.True(hasV5Part, "Should have V5 part");
            Assert.False(hasV4Part, "Should not have V4 part");
        }

        private static bool HasPart(ApplicationPartManager apm, string name)
        {
            return apm.ApplicationParts
                .Any(p => p is CompiledRazorAssemblyPart cp && cp.Assembly.GetName().Name == "Microsoft.AspNetCore.Identity.UI.Views." + name);
        }
    }
}