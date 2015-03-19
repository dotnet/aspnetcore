// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting.Fakes;
using Microsoft.AspNet.Hosting.Startup;
using Microsoft.Framework.DependencyInjection;
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
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddInstance<IFakeStartupCallback>(this);
            var services = serviceCollection.BuildServiceProvider();

            var diagnosticMessages = new List<string>();
            var startup = ApplicationStartup.LoadStartupMethods(services, "Microsoft.AspNet.Hosting.Tests", "WithServices", diagnosticMessages);

            var app = new ApplicationBuilder(services);
            app.ApplicationServices = startup.ConfigureServicesDelegate(serviceCollection);
            startup.ConfigureDelegate(app);

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
        [InlineData("BaseClass")]
        public void StartupClassAddsConfigureServicesToApplicationServices(string environment)
        {
            var services = new ServiceCollection().BuildServiceProvider();
            var diagnosticMesssages = new List<string>();

            var startup = ApplicationStartup.LoadStartupMethods(services, "Microsoft.AspNet.Hosting.Tests", environment ?? "", diagnosticMesssages);

            var app = new ApplicationBuilder(services);
            app.ApplicationServices = startup.ConfigureServicesDelegate(new ServiceCollection());
            startup.ConfigureDelegate(app);

            var options = app.ApplicationServices.GetRequiredService<IOptions<FakeOptions>>().Options;
            Assert.NotNull(options);
            Assert.True(options.Configured);
            Assert.Equal(environment, options.Environment);
        }

        [Fact]
        public void StartupWithNoConfigureThrows()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddInstance<IFakeStartupCallback>(this);
            var services = serviceCollection.BuildServiceProvider();
            var diagnosticMessages = new List<string>();

            var ex = Assert.Throws<InvalidOperationException>(() => ApplicationStartup.LoadStartupMethods(services, "Microsoft.AspNet.Hosting.Tests", "Boom", diagnosticMessages));
            Assert.Equal("A method named 'ConfigureBoom' or 'Configure' in the type 'Microsoft.AspNet.Hosting.Fakes.StartupBoom' could not be found.", ex.Message);
        }

        [Fact]
        public void StartupClassCanHandleConfigureServicesThatReturnsNull()
        {
            var serviceCollection = new ServiceCollection();
            var services = serviceCollection.BuildServiceProvider();

            var diagnosticMessages = new List<string>();
            var startup = ApplicationStartup.LoadStartupMethods(services, "Microsoft.AspNet.Hosting.Tests", "WithNullConfigureServices", diagnosticMessages);

            var app = new ApplicationBuilder(services);
            app.ApplicationServices = startup.ConfigureServicesDelegate(new ServiceCollection());
            Assert.NotNull(app.ApplicationServices);
            startup.ConfigureDelegate(app);
            Assert.NotNull(app.ApplicationServices);
        }

        [Fact]
        public void StartupClassWithConfigureServicesShouldMakeServiceAvailableInConfigure()
        {
            var serviceCollection = new ServiceCollection();
            var services = serviceCollection.BuildServiceProvider();

            var diagnosticMessages = new List<string>();
            var startup = ApplicationStartup.LoadStartupMethods(services, "Microsoft.AspNet.Hosting.Tests", "WithConfigureServices", diagnosticMessages);

            var app = new ApplicationBuilder(services);
            app.ApplicationServices = startup.ConfigureServicesDelegate(serviceCollection);
            startup.ConfigureDelegate(app);

            var foo = app.ApplicationServices.GetRequiredService<StartupWithConfigureServices.IFoo>();
            Assert.True(foo.Invoked);
        }

        public void ConfigurationMethodCalled(object instance)
        {
            _configurationMethodCalledList.Add(instance);
        }
    }
}
