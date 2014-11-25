// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting.Fakes;
using Microsoft.AspNet.Hosting.Startup;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.Framework.OptionsModel;
using Xunit;

namespace Microsoft.AspNet.Hosting.Tests
{

    public class StartupManagerTests : IFakeStartupCallback
    {
        private readonly IList<object> _configurationMethodCalledList = new List<object>();

        [Fact]
        public void StartupClassMayHaveHostingServicesInjected()
        {
            var serviceCollection = new ServiceCollection().AddHosting();
            serviceCollection.AddInstance<IFakeStartupCallback>(this);
            var services = serviceCollection.BuildServiceProvider();

            var manager = services.GetRequiredService<IStartupManager>();

            var startup = manager.LoadStartup("Microsoft.AspNet.Hosting.Tests", "WithServices");

            startup.Invoke(null);

            Assert.Equal(2, _configurationMethodCalledList.Count);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("Dev")]
        [InlineData("Retail")]
        [InlineData("Static")]
        [InlineData("StaticProvider")]
        [InlineData("Provider")]
        [InlineData("ProviderArgs")]
        public void StartupClassAddsConfigureServicesToApplicationServices(string environment)
        {
            var services = HostingServices.Create().BuildServiceProvider();
            var manager = services.GetRequiredService<IStartupManager>();

            var startup = manager.LoadStartup("Microsoft.AspNet.Hosting.Tests", environment ?? "");

            var app = new ApplicationBuilder(services);

            startup.Invoke(app);

            var options = app.ApplicationServices.GetRequiredService<IOptions<FakeOptions>>().Options;
            Assert.NotNull(options);
            Assert.True(options.Configured);
            Assert.Equal(environment, options.Environment);
        }

        [Theory]
        [InlineData("Null")]
        [InlineData("FallbackProvider")]
        public void StartupClassConfigureServicesThatFallsbackToApplicationServices(string env)
        {
            var services = HostingServices.Create().BuildServiceProvider();
            var manager = services.GetRequiredService<IStartupManager>();

            var startup = manager.LoadStartup("Microsoft.AspNet.Hosting.Tests", env);

            var app = new ApplicationBuilder(services);

            startup.Invoke(app);

            Assert.Equal(services, app.ApplicationServices);
        }

        // REVIEW: With the manifest change, Since the ConfigureServices are not imported, UseServices will mask what's in ConfigureServices
        // This will throw since ConfigureServices consumes manifest and then UseServices will blow up
        [Fact(Skip = "Review Failure")]
        public void StartupClassWithConfigureServicesAndUseServicesHidesConfigureServices()
        {
            var services = HostingServices.Create().BuildServiceProvider();
            var manager = services.GetRequiredService<IStartupManager>();

            var startup = manager.LoadStartup("Microsoft.AspNet.Hosting.Tests", "UseServices");

            var app = new ApplicationBuilder(services);

            startup.Invoke(app);

            Assert.NotNull(app.ApplicationServices.GetRequiredService<FakeService>());
            Assert.Null(app.ApplicationServices.GetService<IFakeService>());

            var options = app.ApplicationServices.GetRequiredService<IOptions<FakeOptions>>().Options;
            Assert.NotNull(options);
            Assert.Equal("Configured", options.Message);
            Assert.False(options.Configured); // Options never resolved from inner containers
        }

        [Fact]
        public void StartupWithNoConfigureThrows()
        {
            var serviceCollection = HostingServices.Create();
            serviceCollection.AddInstance<IFakeStartupCallback>(this);
            var services = serviceCollection.BuildServiceProvider();
            var manager = services.GetRequiredService<IStartupManager>();

            var ex = Assert.Throws<Exception>(() => manager.LoadStartup("Microsoft.AspNet.Hosting.Tests", "Boom"));
            Assert.True(ex.Message.Contains("ConfigureBoom or Configure method not found"));
        }

        public void ConfigurationMethodCalled(object instance)
        {
            _configurationMethodCalledList.Add(instance);
        }
    }
}
