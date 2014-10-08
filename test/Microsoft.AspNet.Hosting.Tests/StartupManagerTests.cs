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

            var options = app.ApplicationServices.GetService<IOptionsAccessor<FakeOptions>>().Options;
            Assert.NotNull(options);
            Assert.True(options.Configured);
            Assert.Equal(environment, options.Environment);
        }

        [Fact]
        public void StartupClassDoesNotRegisterOptionsWithNoConfigureServices()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.Add(HostingServices.GetDefaultServices());
            serviceCollection.AddInstance<IFakeStartupCallback>(this);
            var services = serviceCollection.BuildServiceProvider();
            var manager = services.GetService<IStartupManager>();

            var startup = manager.LoadStartup("Microsoft.AspNet.Hosting.Tests", "NoServices");

            var app = new ApplicationBuilder(services);

            startup.Invoke(app);

            var ex = Assert.Throws<Exception>(() => app.ApplicationServices.GetService<IOptionsAccessor<FakeOptions>>());
            Assert.True(ex.Message.Contains("No service for type 'Microsoft.Framework.OptionsModel.IOptionsAccessor"));
        }

        public void ConfigurationMethodCalled(object instance)
        {
            _configurationMethodCalledList.Add(instance);
        }
    }
}
