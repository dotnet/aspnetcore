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
            var serviceCollection = new ServiceCollection().AddHosting();
            serviceCollection.AddInstance<IFakeStartupCallback>(this);
            var services = serviceCollection.BuildServiceProvider();

            var loader = services.GetRequiredService<IStartupLoader>();

            var diagnosticMessages = new List<string>();
            var startup = loader.LoadStartup("Microsoft.AspNet.Hosting.Tests", "WithServices", diagnosticMessages);

            startup.Invoke(new ApplicationBuilder(services));

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
            var services = HostingServices.Create().BuildServiceProvider();
            var loader = services.GetRequiredService<IStartupLoader>();
            var diagnosticMesssages = new List<string>();

            var startup = loader.LoadStartup("Microsoft.AspNet.Hosting.Tests", environment ?? "", diagnosticMesssages);

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
            var loader = services.GetRequiredService<IStartupLoader>();
            var diagnosticMessages = new List<string>();

            var startup = loader.LoadStartup("Microsoft.AspNet.Hosting.Tests", env, diagnosticMessages);

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
            var loader = services.GetRequiredService<IStartupLoader>();
            var diagnosticMessages = new List<string>();

            var startup = loader.LoadStartup("Microsoft.AspNet.Hosting.Tests", "UseServices", diagnosticMessages);

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
            var loader = services.GetRequiredService<IStartupLoader>();
            var diagnosticMessages = new List<string>();

            var ex = Assert.Throws<Exception>(() => loader.LoadStartup("Microsoft.AspNet.Hosting.Tests", "Boom", diagnosticMessages));
            Assert.Equal("A method named 'ConfigureBoom' or 'Configure' in the type 'Microsoft.AspNet.Hosting.Fakes.StartupBoom' could not be found.", ex.Message);
        }

        [Fact]
        public void StartupWithConfigureServicesNotResolvedThrows()
        {
            var serviceCollection = HostingServices.Create();
            var services = serviceCollection.BuildServiceProvider();
            var loader = services.GetRequiredService<IStartupLoader>();
            var diagnosticMessages = new List<string>();

            var startup = loader.LoadStartup("Microsoft.AspNet.Hosting.Tests", "WithConfigureServicesNotResolved", diagnosticMessages);


            var app = new ApplicationBuilder(services);

            var ex = Assert.Throws<Exception>(() => startup.Invoke(app));

            Assert.Equal("Could not resolve a service of type 'System.Int32' for the parameter 'notAService' of method 'Configure' on type 'Microsoft.AspNet.Hosting.Fakes.StartupWithConfigureServicesNotResolved'.", ex.Message);
        }

        [Fact]
        public void StartupClassWithConfigureServicesShouldMakeServiceAvailableInConfigure()
        {
            var serviceCollection = HostingServices.Create();
            var services = serviceCollection.BuildServiceProvider();
            var loader = services.GetRequiredService<IStartupLoader>();

            var app = new ApplicationBuilder(services);

            var diagnosticMessages = new List<string>();
            var startup = loader.LoadStartup("Microsoft.AspNet.Hosting.Tests", "WithConfigureServices", diagnosticMessages);

            startup.Invoke(app);

            var foo = app.ApplicationServices.GetRequiredService<StartupWithConfigureServices.IFoo>();
            Assert.True(foo.Invoked);
        }

        public void ConfigurationMethodCalled(object instance)
        {
            _configurationMethodCalledList.Add(instance);
        }
    }
}
