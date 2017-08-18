// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Builder.Internal;
using Microsoft.AspNetCore.Hosting.Fakes;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Hosting.Tests.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Hosting.Tests
{
    public class StartupManagerTests
    {
        [Fact]
        public void StartupClassMayHaveHostingServicesInjected()
        {
            var callbackStartup = new FakeStartupCallback();
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IServiceProviderFactory<IServiceCollection>, DefaultServiceProviderFactory>();
            serviceCollection.AddSingleton<IFakeStartupCallback>(callbackStartup);
            var services = serviceCollection.BuildServiceProvider();

            var type = StartupLoader.FindStartupType("Microsoft.AspNetCore.Hosting.Tests", "WithServices");
            var startup = StartupLoader.LoadMethods(services, type, "WithServices");

            var app = new ApplicationBuilder(services);
            app.ApplicationServices = startup.ConfigureServicesDelegate(serviceCollection);
            startup.ConfigureDelegate(app);

            Assert.Equal(2, callbackStartup.MethodsCalled);
        }

        [Fact]
        public void StartupClassMayHaveScopedServicesInjected()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IServiceProviderFactory<IServiceCollection>>(new DefaultServiceProviderFactory(new ServiceProviderOptions
            {
                ValidateScopes = true
            }));

            serviceCollection.AddScoped<DisposableService>();
            var services = serviceCollection.BuildServiceProvider();

            var type = StartupLoader.FindStartupType("Microsoft.AspNetCore.Hosting.Tests", "WithScopedServices");
            var startup = StartupLoader.LoadMethods(services, type, "WithScopedServices");
            Assert.NotNull(startup.StartupInstance);

            var app = new ApplicationBuilder(services);
            app.ApplicationServices = startup.ConfigureServicesDelegate(serviceCollection);
            startup.ConfigureDelegate(app);

            var instance = (StartupWithScopedServices)startup.StartupInstance;
            Assert.NotNull(instance.DisposableService);
            Assert.True(instance.DisposableService.Disposed);
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
            var services = new ServiceCollection()
                .AddSingleton<IServiceProviderFactory<IServiceCollection>, DefaultServiceProviderFactory>()
                .BuildServiceProvider();
            var type = StartupLoader.FindStartupType("Microsoft.AspNetCore.Hosting.Tests", environment);
            var startup = StartupLoader.LoadMethods(services, type, environment);

            var app = new ApplicationBuilder(services);
            app.ApplicationServices = startup.ConfigureServicesDelegate(new ServiceCollection());
            startup.ConfigureDelegate(app);

            var options = app.ApplicationServices.GetRequiredService<IOptions<FakeOptions>>().Value;
            Assert.NotNull(options);
            Assert.True(options.Configured);
            Assert.Equal(environment, options.Environment);
        }

        [Fact]
        public void StartupWithNoConfigureThrows()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IServiceProviderFactory<IServiceCollection>, DefaultServiceProviderFactory>();
            serviceCollection.AddSingleton<IFakeStartupCallback>(new FakeStartupCallback());
            var services = serviceCollection.BuildServiceProvider();
            var type = StartupLoader.FindStartupType("Microsoft.AspNetCore.Hosting.Tests", "Boom");
            var ex = Assert.Throws<InvalidOperationException>(() => StartupLoader.LoadMethods(services, type, "Boom"));
            Assert.Equal("A public method named 'ConfigureBoom' or 'Configure' could not be found in the 'Microsoft.AspNetCore.Hosting.Fakes.StartupBoom' type.", ex.Message);
        }

        [Theory]
        [InlineData("caseinsensitive")]
        [InlineData("CaseInsensitive")]
        [InlineData("CASEINSENSITIVE")]
        [InlineData("CaSEiNSENsitiVE")]
        public void FindsStartupClassCaseInsensitive(string environment)
        {
            var type = StartupLoader.FindStartupType("Microsoft.AspNetCore.Hosting.Tests", environment);

            Assert.Equal("StartupCaseInsensitive", type.Name);
        }

        [Theory]
        [InlineData("caseinsensitive")]
        [InlineData("CaseInsensitive")]
        [InlineData("CASEINSENSITIVE")]
        [InlineData("CaSEiNSENsitiVE")]
        public void StartupClassAddsConfigureServicesToApplicationServicesCaseInsensitive(string environment)
        {
            var services = new ServiceCollection()
                .AddSingleton<IServiceProviderFactory<IServiceCollection>, DefaultServiceProviderFactory>()
                .BuildServiceProvider();
            var type = StartupLoader.FindStartupType("Microsoft.AspNetCore.Hosting.Tests", environment);
            var startup = StartupLoader.LoadMethods(services, type, environment);

            var app = new ApplicationBuilder(services);
            app.ApplicationServices = startup.ConfigureServicesDelegate(new ServiceCollection());
            startup.ConfigureDelegate(app); // By this not throwing, it found "ConfigureCaseInsensitive"

            var options = app.ApplicationServices.GetRequiredService<IOptions<FakeOptions>>().Value;
            Assert.NotNull(options);
            Assert.True(options.Configured);
            Assert.Equal("ConfigureCaseInsensitiveServices", options.Environment);
        }

        [Fact]
        public void StartupWithTwoConfiguresThrows()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IServiceProviderFactory<IServiceCollection>, DefaultServiceProviderFactory>();
            serviceCollection.AddSingleton<IFakeStartupCallback>(new FakeStartupCallback());
            var services = serviceCollection.BuildServiceProvider();

            var type = StartupLoader.FindStartupType("Microsoft.AspNetCore.Hosting.Tests", "TwoConfigures");

            var ex = Assert.Throws<InvalidOperationException>(() => StartupLoader.LoadMethods(services, type, "TwoConfigures"));
            Assert.Equal("Having multiple overloads of method 'Configure' is not supported.", ex.Message);
        }

        [Fact]
        public void StartupWithPrivateConfiguresThrows()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IServiceProviderFactory<IServiceCollection>, DefaultServiceProviderFactory>();
            serviceCollection.AddSingleton<IFakeStartupCallback>(new FakeStartupCallback());
            var services = serviceCollection.BuildServiceProvider();

            var diagnosticMessages = new List<string>();
            var type = StartupLoader.FindStartupType("Microsoft.AspNetCore.Hosting.Tests", "PrivateConfigure");

            var ex = Assert.Throws<InvalidOperationException>(() => StartupLoader.LoadMethods(services, type, "PrivateConfigure"));
            Assert.Equal("A public method named 'ConfigurePrivateConfigure' or 'Configure' could not be found in the 'Microsoft.AspNetCore.Hosting.Fakes.StartupPrivateConfigure' type.", ex.Message);
        }

        [Fact]
        public void StartupWithTwoConfigureServicesThrows()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IServiceProviderFactory<IServiceCollection>, DefaultServiceProviderFactory>();
            serviceCollection.AddSingleton<IFakeStartupCallback>(new FakeStartupCallback());
            var services = serviceCollection.BuildServiceProvider();

            var type = StartupLoader.FindStartupType("Microsoft.AspNetCore.Hosting.Tests", "TwoConfigureServices");

            var ex = Assert.Throws<InvalidOperationException>(() => StartupLoader.LoadMethods(services, type, "TwoConfigureServices"));
            Assert.Equal("Having multiple overloads of method 'ConfigureServices' is not supported.", ex.Message);
        }

        [Fact]
        public void StartupClassCanHandleConfigureServicesThatReturnsNull()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IServiceProviderFactory<IServiceCollection>, DefaultServiceProviderFactory>();
            var services = serviceCollection.BuildServiceProvider();

            var type = StartupLoader.FindStartupType("Microsoft.AspNetCore.Hosting.Tests", "WithNullConfigureServices");
            var startup = StartupLoader.LoadMethods(services, type, "WithNullConfigureServices");

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
            serviceCollection.AddSingleton<IServiceProviderFactory<IServiceCollection>, DefaultServiceProviderFactory>();
            var services = serviceCollection.BuildServiceProvider();

            var type = StartupLoader.FindStartupType("Microsoft.AspNetCore.Hosting.Tests", "WithConfigureServices");
            var startup = StartupLoader.LoadMethods(services, type, "WithConfigureServices");

            var app = new ApplicationBuilder(services);
            app.ApplicationServices = startup.ConfigureServicesDelegate(serviceCollection);
            startup.ConfigureDelegate(app);

            var foo = app.ApplicationServices.GetRequiredService<StartupWithConfigureServices.IFoo>();
            Assert.True(foo.Invoked);
        }

        [Fact]
        public void StartupLoaderCanLoadByType()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IServiceProviderFactory<IServiceCollection>, DefaultServiceProviderFactory>();
            var services = serviceCollection.BuildServiceProvider();

            var hostingEnv = new HostingEnvironment();
            var startup = StartupLoader.LoadMethods(services, typeof(TestStartup), hostingEnv.EnvironmentName);

            var app = new ApplicationBuilder(services);
            app.ApplicationServices = startup.ConfigureServicesDelegate(serviceCollection);
            startup.ConfigureDelegate(app);

            var foo = app.ApplicationServices.GetRequiredService<SimpleService>();
            Assert.Equal("Configure", foo.Message);
        }

        [Fact]
        public void StartupLoaderCanLoadByTypeWithEnvironment()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IServiceProviderFactory<IServiceCollection>, DefaultServiceProviderFactory>();
            var services = serviceCollection.BuildServiceProvider();

            var startup = StartupLoader.LoadMethods(services, typeof(TestStartup), "No");

            var app = new ApplicationBuilder(services);
            app.ApplicationServices = startup.ConfigureServicesDelegate(serviceCollection);

            var ex = Assert.Throws<TargetInvocationException>(() => startup.ConfigureDelegate(app));
            Assert.IsAssignableFrom(typeof(InvalidOperationException), ex.InnerException);
        }

        [Fact]
        public void CustomProviderFactoryCallsConfigureContainer()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IServiceProviderFactory<MyContainer>, MyContainerFactory>();
            var services = serviceCollection.BuildServiceProvider();

            var startup = StartupLoader.LoadMethods(services, typeof(MyContainerStartup), EnvironmentName.Development);

            var app = new ApplicationBuilder(services);
            app.ApplicationServices = startup.ConfigureServicesDelegate(serviceCollection);

            Assert.IsType(typeof(MyContainer), app.ApplicationServices);
            Assert.True(((MyContainer)app.ApplicationServices).FancyMethodCalled);
        }

        [Fact]
        public void CustomServiceProviderFactoryStartupBaseClassCallsConfigureContainer()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IServiceProviderFactory<MyContainer>, MyContainerFactory>();
            var services = serviceCollection.BuildServiceProvider();

            var startup = StartupLoader.LoadMethods(services, typeof(MyContainerStartupBaseClass), EnvironmentName.Development);

            var app = new ApplicationBuilder(services);
            app.ApplicationServices = startup.ConfigureServicesDelegate(serviceCollection);

            Assert.IsType(typeof(MyContainer), app.ApplicationServices);
            Assert.True(((MyContainer)app.ApplicationServices).FancyMethodCalled);
        }

        [Fact]
        public void CustomServiceProviderFactoryEnvironmentBasedConfigureContainer()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IServiceProviderFactory<MyContainer>, MyContainerFactory>();
            var services = serviceCollection.BuildServiceProvider();

            var startup = StartupLoader.LoadMethods(services, typeof(MyContainerStartupEnvironmentBased), EnvironmentName.Production);

            var app = new ApplicationBuilder(services);
            app.ApplicationServices = startup.ConfigureServicesDelegate(serviceCollection);

            Assert.IsType(typeof(MyContainer), app.ApplicationServices);
            Assert.Equal(((MyContainer)app.ApplicationServices).Environment, EnvironmentName.Production);
        }

        [Fact]
        public void CustomServiceProviderFactoryThrowsIfNotRegisteredWithConfigureContainerMethod()
        {
            var serviceCollection = new ServiceCollection();
            var services = serviceCollection.BuildServiceProvider();

            var startup = StartupLoader.LoadMethods(services, typeof(MyContainerStartup), EnvironmentName.Development);

            Assert.Throws<InvalidOperationException>(() => startup.ConfigureServicesDelegate(serviceCollection));
        }

        [Fact]
        public void CustomServiceProviderFactoryThrowsIfNotRegisteredWithConfigureContainerMethodStartupBase()
        {
            var serviceCollection = new ServiceCollection();
            var services = serviceCollection.BuildServiceProvider();

            Assert.Throws<InvalidOperationException>(() => StartupLoader.LoadMethods(services, typeof(MyContainerStartupBaseClass), EnvironmentName.Development));
        }

        [Fact]
        public void CustomServiceProviderFactoryFailsWithOverloadsInStartup()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IServiceProviderFactory<MyContainer>, MyContainerFactory>();
            var services = serviceCollection.BuildServiceProvider();

            Assert.Throws<InvalidOperationException>(() => StartupLoader.LoadMethods(services, typeof(MyContainerStartupWithOverloads), EnvironmentName.Development));
        }

        [Fact]
        public void BadServiceProviderFactoryFailsThatReturnsNullServiceProviderOverriddenByDefault()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IServiceProviderFactory<MyContainer>, MyBadContainerFactory>();
            var services = serviceCollection.BuildServiceProvider();

            var startup = StartupLoader.LoadMethods(services, typeof(MyContainerStartup), EnvironmentName.Development);

            var app = new ApplicationBuilder(services);
            app.ApplicationServices = startup.ConfigureServicesDelegate(serviceCollection);

            Assert.NotNull(app.ApplicationServices);
            Assert.IsNotType(typeof(MyContainer), app.ApplicationServices);
        }

        public class MyContainerStartupWithOverloads
        {
            public void ConfigureServices(IServiceCollection services)
            {

            }

            public void ConfigureContainer(MyContainer container)
            {
                container.MyFancyContainerMethod();
            }

            public void ConfigureContainer(IServiceCollection services)
            {

            }

            public void Configure(IApplicationBuilder app)
            {

            }
        }

        public class MyContainerStartupEnvironmentBased
        {
            public void ConfigureServices(IServiceCollection services)
            {

            }

            public void ConfigureDevelopmentContainer(MyContainer container)
            {
                container.Environment = EnvironmentName.Development;
            }

            public void ConfigureProductionContainer(MyContainer container)
            {
                container.Environment = EnvironmentName.Production;
            }

            public void Configure(IApplicationBuilder app)
            {

            }
        }

        public class MyContainerStartup
        {
            public void ConfigureServices(IServiceCollection services)
            {

            }

            public void ConfigureContainer(MyContainer container)
            {
                container.MyFancyContainerMethod();
            }

            public void Configure(IApplicationBuilder app)
            {

            }
        }

        public class MyContainerStartupBaseClass : StartupBase<MyContainer>
        {
            public MyContainerStartupBaseClass(IServiceProviderFactory<MyContainer> factory) : base(factory)
            {
            }

            public override void Configure(IApplicationBuilder app)
            {

            }

            public override void ConfigureContainer(MyContainer containerBuilder)
            {
                containerBuilder.MyFancyContainerMethod();
            }
        }

        public class SimpleService
        {
            public SimpleService()
            {
            }

            public string Message { get; set; }
        }

        public class TestStartup
        {
            public void ConfigureServices(IServiceCollection services)
            {
                services.AddSingleton<SimpleService>();
            }

            public void ConfigureNoServices(IServiceCollection services)
            {
            }

            public void Configure(IApplicationBuilder app)
            {
                var service = app.ApplicationServices.GetRequiredService<SimpleService>();
                service.Message = "Configure";
            }

            public void ConfigureNo(IApplicationBuilder app)
            {
                var service = app.ApplicationServices.GetRequiredService<SimpleService>();
            }
        }

        public class FakeStartupCallback : IFakeStartupCallback
        {
            private readonly IList<object> _configurationMethodCalledList = new List<object>();

            public int MethodsCalled => _configurationMethodCalledList.Count;

            public void ConfigurationMethodCalled(object instance)
            {
                _configurationMethodCalledList.Add(instance);
            }
        }

        public class DisposableService : IDisposable
        {
            public bool Disposed { get; set; }

            public void Dispose()
            {
                Disposed = true;
            }
        }
    }
}
