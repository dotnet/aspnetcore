// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Fakes;
using Microsoft.AspNetCore.Hosting.Tests.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Hosting.Tests;

public class StartupManagerTests
{
    [Fact]
    public void ConventionalStartupClass_StartupServiceFilters_WrapsConfigureServicesMethod()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IServiceProviderFactory<IServiceCollection>, DefaultServiceProviderFactory>();
#pragma warning disable CS0612 // Type or member is obsolete
        serviceCollection.AddSingleton<IStartupConfigureServicesFilter>(new TestStartupServicesFilter(1, overrideAfterService: true));
        serviceCollection.AddSingleton<IStartupConfigureServicesFilter>(new TestStartupServicesFilter(2, overrideAfterService: true));
#pragma warning restore CS0612 // Type or member is obsolete
        var services = serviceCollection.BuildServiceProvider();

        var type = typeof(VoidReturningStartupServicesFiltersStartup);
        var startup = StartupLoader.LoadMethods(services, type, "");

        var applicationServices = startup.ConfigureServicesDelegate(serviceCollection);
        var before = applicationServices.GetRequiredService<ServiceBefore>();
        var after = applicationServices.GetRequiredService<ServiceAfter>();

        Assert.Equal("StartupServicesFilter Before 1", before.Message);
        Assert.Equal("StartupServicesFilter After 1", after.Message);
    }

    [Fact]
    public void ConventionalStartupClass_StartupServiceFilters_MultipleStartupServiceFiltersRun()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IServiceProviderFactory<IServiceCollection>, DefaultServiceProviderFactory>();
#pragma warning disable CS0612 // Type or member is obsolete
        serviceCollection.AddSingleton<IStartupConfigureServicesFilter>(new TestStartupServicesFilter(1, overrideAfterService: false));
        serviceCollection.AddSingleton<IStartupConfigureServicesFilter>(new TestStartupServicesFilter(2, overrideAfterService: true));
#pragma warning restore CS0612 // Type or member is obsolete
        var services = serviceCollection.BuildServiceProvider();

        var type = typeof(VoidReturningStartupServicesFiltersStartup);
        var startup = StartupLoader.LoadMethods(services, type, "");

        var applicationServices = startup.ConfigureServicesDelegate(serviceCollection);
        var before = applicationServices.GetRequiredService<ServiceBefore>();
        var after = applicationServices.GetRequiredService<ServiceAfter>();

        Assert.Equal("StartupServicesFilter Before 1", before.Message);
        Assert.Equal("StartupServicesFilter After 2", after.Message);
    }

    [Fact]
    public void ConventionalStartupClass_StartupServicesFilters_ThrowsIfStartupBuildsTheContainerAsync()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IServiceProviderFactory<IServiceCollection>, DefaultServiceProviderFactory>();
#pragma warning disable CS0612 // Type or member is obsolete
        serviceCollection.AddSingleton<IStartupConfigureServicesFilter>(new TestStartupServicesFilter(1, overrideAfterService: false));
#pragma warning restore CS0612 // Type or member is obsolete
        var services = serviceCollection.BuildServiceProvider();

        var type = typeof(IServiceProviderReturningStartupServicesFiltersStartup);
        var startup = StartupLoader.LoadMethods(services, type, "");

#pragma warning disable CS0612 // Type or member is obsolete
        var expectedMessage = $"A ConfigureServices method that returns an {nameof(IServiceProvider)} is " +
            $"not compatible with the use of one or more {nameof(IStartupConfigureServicesFilter)}. " +
            $"Use a void returning ConfigureServices method instead or a ConfigureContainer method.";
#pragma warning restore CS0612 // Type or member is obsolete

        var exception = Assert.Throws<InvalidOperationException>(() => startup.ConfigureServicesDelegate(serviceCollection));

        Assert.Equal(expectedMessage, exception.Message);
    }

    [Fact]
    public void ConventionalStartupClass_ConfigureContainerFilters_WrapInRegistrationOrder()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IServiceProviderFactory<MyContainer>, MyContainerFactory>();
#pragma warning disable CS0612 // Type or member is obsolete
        serviceCollection.AddSingleton<IStartupConfigureContainerFilter<MyContainer>>(new TestConfigureContainerFilter(1, overrideAfterService: true));
        serviceCollection.AddSingleton<IStartupConfigureContainerFilter<MyContainer>>(new TestConfigureContainerFilter(2, overrideAfterService: true));
#pragma warning restore CS0612 // Type or member is obsolete
        var services = serviceCollection.BuildServiceProvider();

        var type = typeof(ConfigureContainerStartupServicesFiltersStartup);
        var startup = StartupLoader.LoadMethods(services, type, "");

        var applicationServices = startup.ConfigureServicesDelegate(serviceCollection);
        var before = applicationServices.GetRequiredService<ServiceBefore>();
        var after = applicationServices.GetRequiredService<ServiceAfter>();

        Assert.Equal("ConfigureContainerFilter Before 1", before.Message);
        Assert.Equal("ConfigureContainerFilter After 1", after.Message);
    }

    [Fact]
    public void ConventionalStartupClass_ConfigureContainerFilters_RunsAllFilters()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IServiceProviderFactory<MyContainer>, MyContainerFactory>();
#pragma warning disable CS0612 // Type or member is obsolete
        serviceCollection.AddSingleton<IStartupConfigureContainerFilter<MyContainer>>(new TestConfigureContainerFilter(1, overrideAfterService: false));
        serviceCollection.AddSingleton<IStartupConfigureContainerFilter<MyContainer>>(new TestConfigureContainerFilter(2, overrideAfterService: true));
#pragma warning restore CS0612 // Type or member is obsolete
        var services = serviceCollection.BuildServiceProvider();

        var type = typeof(ConfigureContainerStartupServicesFiltersStartup);
        var startup = StartupLoader.LoadMethods(services, type, "");

        var applicationServices = startup.ConfigureServicesDelegate(serviceCollection);
        var before = applicationServices.GetRequiredService<ServiceBefore>();
        var after = applicationServices.GetRequiredService<ServiceAfter>();

        Assert.Equal("ConfigureContainerFilter Before 1", before.Message);
        Assert.Equal("ConfigureContainerFilter After 2", after.Message);
    }

    [Fact]
    public void ConventionalStartupClass_ConfigureContainerFilters_RunAfterConfigureServicesFilters()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IServiceProviderFactory<MyContainer>, MyContainerFactory>();
#pragma warning disable CS0612 // Type or member is obsolete
        serviceCollection.AddSingleton<IStartupConfigureServicesFilter>(new TestStartupServicesFilter(1, overrideAfterService: false));
        serviceCollection.AddSingleton<IStartupConfigureContainerFilter<MyContainer>>(new TestConfigureContainerFilter(2, overrideAfterService: true));
#pragma warning restore CS0612 // Type or member is obsolete
        var services = serviceCollection.BuildServiceProvider();

        var type = typeof(ConfigureServicesAndConfigureContainerStartup);
        var startup = StartupLoader.LoadMethods(services, type, "");

        var applicationServices = startup.ConfigureServicesDelegate(serviceCollection);
        var before = applicationServices.GetRequiredService<ServiceBefore>();
        var after = applicationServices.GetRequiredService<ServiceAfter>();

        Assert.Equal("StartupServicesFilter Before 1", before.Message);
        Assert.Equal("ConfigureContainerFilter After 2", after.Message);
    }

    public class ConfigureContainerStartupServicesFiltersStartup
    {
        public void ConfigureContainer(MyContainer services)
        {
            services.Services.TryAddSingleton(new ServiceBefore { Message = "Configure container" });
            services.Services.TryAddSingleton(new ServiceAfter { Message = "Configure container" });
        }

        public void Configure(IApplicationBuilder builder)
        {
        }
    }

    public class ConfigureServicesAndConfigureContainerStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.TryAddSingleton(new ServiceBefore { Message = "Configure services" });
            services.TryAddSingleton(new ServiceAfter { Message = "Configure services" });
        }

        public void ConfigureContainer(MyContainer services)
        {
            services.Services.TryAddSingleton(new ServiceBefore { Message = "Configure container" });
            services.Services.TryAddSingleton(new ServiceAfter { Message = "Configure container" });
        }

        public void Configure(IApplicationBuilder builder)
        {
        }
    }

#pragma warning disable CS0612 // Type or member is obsolete
    public class TestConfigureContainerFilter : IStartupConfigureContainerFilter<MyContainer>
#pragma warning restore CS0612 // Type or member is obsolete
    {
        public TestConfigureContainerFilter(object additionalData, bool overrideAfterService)
        {
            AdditionalData = additionalData;
            OverrideAfterService = overrideAfterService;
        }

        public object AdditionalData { get; }
        public bool OverrideAfterService { get; }

        public Action<MyContainer> ConfigureContainer(Action<MyContainer> next)
        {
            return services =>
            {
                services.Services.TryAddSingleton(new ServiceBefore { Message = $"ConfigureContainerFilter Before {AdditionalData}" });

                next(services);

                // Ensures we can always override.
                if (OverrideAfterService)
                {
                    services.Services.AddSingleton(new ServiceAfter { Message = $"ConfigureContainerFilter After {AdditionalData}" });
                }
                else
                {
                    services.Services.TryAddSingleton(new ServiceAfter { Message = $"ConfigureContainerFilter After {AdditionalData}" });
                }
            };
        }
    }

    public class IServiceProviderReturningStartupServicesFiltersStartup
    {
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.TryAddSingleton(new ServiceBefore { Message = "Configure services" });
            services.TryAddSingleton(new ServiceAfter { Message = "Configure services" });

            return services.BuildServiceProvider();
        }

        public void Configure(IApplicationBuilder builder)
        {
        }
    }

#pragma warning disable CS0612 // Type or member is obsolete
    public class TestStartupServicesFilter : IStartupConfigureServicesFilter
#pragma warning restore CS0612 // Type or member is obsolete
    {
        public TestStartupServicesFilter(object additionalData, bool overrideAfterService)
        {
            AdditionalData = additionalData;
            OverrideAfterService = overrideAfterService;
        }

        public object AdditionalData { get; }
        public bool OverrideAfterService { get; }

        public Action<IServiceCollection> ConfigureServices(Action<IServiceCollection> next)
        {
            return services =>
            {
                services.TryAddSingleton(new ServiceBefore { Message = $"StartupServicesFilter Before {AdditionalData}" });

                next(services);

                // Ensures we can always override.
                if (OverrideAfterService)
                {
                    services.AddSingleton(new ServiceAfter { Message = $"StartupServicesFilter After {AdditionalData}" });
                }
                else
                {
                    services.TryAddSingleton(new ServiceAfter { Message = $"StartupServicesFilter After {AdditionalData}" });
                }
            };
        }
    }

    public class VoidReturningStartupServicesFiltersStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.TryAddSingleton(new ServiceBefore { Message = "Configure services" });
            services.TryAddSingleton(new ServiceAfter { Message = "Configure services" });
        }

        public void Configure(IApplicationBuilder builder)
        {
        }
    }

    public class ServiceBefore
    {
        public string Message { get; set; }
    }

    public class ServiceAfter
    {
        public string Message { get; set; }
    }

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

        Assert.Throws<InvalidOperationException>(() => startup.ConfigureDelegate(app));
    }

    [Fact]
    public void ConfigureServicesThrowingDoesNotThrowTargetInvocationException()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IServiceProviderFactory<IServiceCollection>, DefaultServiceProviderFactory>();
        var services = serviceCollection.BuildServiceProvider();

        var startup = StartupLoader.LoadMethods(services, typeof(StartupConfigureServicesThrows), environmentName: null);

        var app = new ApplicationBuilder(services);

        Assert.Throws<Exception>(() => startup.ConfigureServicesDelegate(serviceCollection));
    }

    [Fact]
    public void ConfigureThrowingDoesNotThrowTargetInvocationException()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IServiceProviderFactory<IServiceCollection>, DefaultServiceProviderFactory>();
        var services = serviceCollection.BuildServiceProvider();

        var startup = StartupLoader.LoadMethods(services, typeof(StartupConfigureThrows), environmentName: null);

        var app = new ApplicationBuilder(services);
        app.ApplicationServices = startup.ConfigureServicesDelegate(serviceCollection);

        Assert.Throws<Exception>(() => startup.ConfigureDelegate(app));
    }

    [Fact]
    public void CustomProviderFactoryCallsConfigureContainer()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IServiceProviderFactory<MyContainer>, MyContainerFactory>();
        var services = serviceCollection.BuildServiceProvider();

        var startup = StartupLoader.LoadMethods(services, typeof(MyContainerStartup), Environments.Development);

        var app = new ApplicationBuilder(services);
        app.ApplicationServices = startup.ConfigureServicesDelegate(serviceCollection);

        Assert.IsType<MyContainer>(app.ApplicationServices);
        Assert.True(((MyContainer)app.ApplicationServices).FancyMethodCalled);
    }

    [Fact]
    public void CustomServiceProviderFactoryStartupBaseClassCallsConfigureContainer()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IServiceProviderFactory<MyContainer>, MyContainerFactory>();
        var services = serviceCollection.BuildServiceProvider();

        var startup = StartupLoader.LoadMethods(services, typeof(MyContainerStartupBaseClass), Environments.Development);

        var app = new ApplicationBuilder(services);
        app.ApplicationServices = startup.ConfigureServicesDelegate(serviceCollection);

        Assert.IsType<MyContainer>(app.ApplicationServices);
        Assert.True(((MyContainer)app.ApplicationServices).FancyMethodCalled);
    }

    [Fact]
    public void CustomServiceProviderFactoryEnvironmentBasedConfigureContainer()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IServiceProviderFactory<MyContainer>, MyContainerFactory>();
        var services = serviceCollection.BuildServiceProvider();

        var startup = StartupLoader.LoadMethods(services, typeof(MyContainerStartupEnvironmentBased), Environments.Production);

        var app = new ApplicationBuilder(services);
        app.ApplicationServices = startup.ConfigureServicesDelegate(serviceCollection);

        Assert.IsType<MyContainer>(app.ApplicationServices);
        Assert.Equal(((MyContainer)app.ApplicationServices).Environment, Environments.Production);
    }

    [Fact]
    public void CustomServiceProviderFactoryThrowsIfNotRegisteredWithConfigureContainerMethod()
    {
        var serviceCollection = new ServiceCollection();
        var services = serviceCollection.BuildServiceProvider();

        var startup = StartupLoader.LoadMethods(services, typeof(MyContainerStartup), Environments.Development);

        Assert.Throws<InvalidOperationException>(() => startup.ConfigureServicesDelegate(serviceCollection));
    }

    [Fact]
    public void CustomServiceProviderFactoryThrowsIfNotRegisteredWithConfigureContainerMethodStartupBase()
    {
        var serviceCollection = new ServiceCollection();
        var services = serviceCollection.BuildServiceProvider();

        Assert.Throws<InvalidOperationException>(() => StartupLoader.LoadMethods(services, typeof(MyContainerStartupBaseClass), Environments.Development));
    }

    [Fact]
    public void CustomServiceProviderFactoryFailsWithOverloadsInStartup()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IServiceProviderFactory<MyContainer>, MyContainerFactory>();
        var services = serviceCollection.BuildServiceProvider();

        Assert.Throws<InvalidOperationException>(() => StartupLoader.LoadMethods(services, typeof(MyContainerStartupWithOverloads), Environments.Development));
    }

    [Fact]
    public void BadServiceProviderFactoryFailsThatReturnsNullServiceProviderOverriddenByDefault()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IServiceProviderFactory<MyContainer>, MyBadContainerFactory>();
        var services = serviceCollection.BuildServiceProvider();

        var startup = StartupLoader.LoadMethods(services, typeof(MyContainerStartup), Environments.Development);

        var app = new ApplicationBuilder(services);
        app.ApplicationServices = startup.ConfigureServicesDelegate(serviceCollection);

        Assert.NotNull(app.ApplicationServices);
        Assert.IsNotType<MyContainer>(app.ApplicationServices);
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
            container.Environment = Environments.Development;
        }

        public void ConfigureProductionContainer(MyContainer container)
        {
            container.Environment = Environments.Production;
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
