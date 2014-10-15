// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Hosting.Fakes;
using Microsoft.AspNet.Hosting.Startup;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Xunit;
using Microsoft.Framework.OptionsModel;
using Microsoft.AspNet.Builder;
using System;

namespace Microsoft.AspNet.Hosting
{

    public class StartupManagerTests : IFakeStartupCallback
    {
        private readonly IList<object> _configurationMethodCalledList = new List<object>();

        [Fact]
        public void StartupClassMayHaveHostingServicesInjected()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.Add(HostingServices.GetDefaultServices());
            serviceCollection.AddInstance<IFakeStartupCallback>(this);
            var services = serviceCollection.BuildServiceProvider();

            var manager = services.GetService<IStartupManager>();

            var startup = manager.LoadStartup("Microsoft.AspNet.Hosting.Tests", "WithServices");

            startup.Invoke(null);

            Assert.Equal(2, _configurationMethodCalledList.Count);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("Dev")]
        [InlineData("Retail")]
        [InlineData("Static")]
        public void StartupClassAddsConfigureServicesToApplicationServices(string environment)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.Add(HostingServices.GetDefaultServices());
            var services = serviceCollection.BuildServiceProvider();
            var manager = services.GetService<IStartupManager>();

            var startup = manager.LoadStartup("Microsoft.AspNet.Hosting.Tests", environment ?? "");

            var app = new ApplicationBuilder(services);

            startup.Invoke(app);

            var options = app.ApplicationServices.GetService<IOptions<FakeOptions>>().Options;
            Assert.NotNull(options);
            Assert.True(options.Configured);
            Assert.Equal(environment, options.Environment);
        }

        [Fact]
        public void StartupClassWithConfigureServicesAndUseServicesAddsBothToServices()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.Add(HostingServices.GetDefaultServices());
            var services = serviceCollection.BuildServiceProvider();
            var manager = services.GetService<IStartupManager>();

            var startup = manager.LoadStartup("Microsoft.AspNet.Hosting.Tests", "UseServices");

            var app = new ApplicationBuilder(services);

            startup.Invoke(app);

            Assert.NotNull(app.ApplicationServices.GetService<FakeService>());
            Assert.NotNull(app.ApplicationServices.GetService<IFakeService>());

            var options = app.ApplicationServices.GetService<IOptions<FakeOptions>>().Options;
            Assert.NotNull(options);
            Assert.Equal("Configured", options.Message);
            Assert.False(options.Configured); // REVIEW: why doesn't the ConfigureServices ConfigureOptions get run?
        }

        [Fact]
        public void StartupWithNoConfigureThrows()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.Add(HostingServices.GetDefaultServices());
            serviceCollection.AddInstance<IFakeStartupCallback>(this);
            var services = serviceCollection.BuildServiceProvider();
            var manager = services.GetService<IStartupManager>();

            var ex = Assert.Throws<Exception>(() => manager.LoadStartup("Microsoft.AspNet.Hosting.Tests", "Boom"));
            Assert.True(ex.Message.Contains("ConfigureBoom or Configure method not found"));
        }

        public void ConfigurationMethodCalled(object instance)
        {
            _configurationMethodCalledList.Add(instance);
        }
    }
}
