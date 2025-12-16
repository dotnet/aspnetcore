// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Moq;

namespace Microsoft.AspNetCore.Components.WebAssembly.Hosting;

public class WebAssemblyHostBuilderTest
{
    [Fact]
    public void Build_AllowsConfiguringConfiguration()
    {
        // Arrange
        var builder = new WebAssemblyHostBuilder(new TestInternalJSImportMethods());

        builder.Configuration.AddInMemoryCollection(new[]
        {
            new KeyValuePair<string, string>("key", "value"),
        });

        // Act
        var host = builder.Build();

        // Assert
        Assert.Equal("value", host.Configuration["key"]);
    }

    [Fact]
    public void Build_AllowsConfiguringServices()
    {
        // Arrange
        var builder = new WebAssemblyHostBuilder(new TestInternalJSImportMethods());

        // This test also verifies that we create a scope.
        builder.Services.AddScoped<StringBuilder>();

        // Act
        var host = builder.Build();

        // Assert
        Assert.NotNull(host.Services.GetRequiredService<StringBuilder>());
    }

    [Fact]
    public void Build_AllowsConfiguringContainer()
    {
        // Arrange
        var builder = new WebAssemblyHostBuilder(new TestInternalJSImportMethods());

        builder.Services.AddScoped<StringBuilder>();
        var factory = new MyFakeServiceProviderFactory();
        builder.ConfigureContainer(factory);

        // Act
        var host = builder.Build();

        // Assert
        Assert.True(factory.CreateServiceProviderCalled);
        Assert.NotNull(host.Services.GetRequiredService<StringBuilder>());
    }

    [Fact]
    public void Build_AllowsConfiguringContainer_WithDelegate()
    {
        // Arrange
        var builder = new WebAssemblyHostBuilder(new TestInternalJSImportMethods());

        builder.Services.AddScoped<StringBuilder>();

        var factory = new MyFakeServiceProviderFactory();
        builder.ConfigureContainer(factory, builder =>
        {
            builder.ServiceCollection.AddScoped<List<string>>();
        });

        // Act
        var host = builder.Build();

        // Assert
        Assert.True(factory.CreateServiceProviderCalled);
        Assert.NotNull(host.Services.GetRequiredService<StringBuilder>());
        Assert.NotNull(host.Services.GetRequiredService<List<string>>());
    }

    [Fact]
    public void Build_InDevelopment_ConfiguresWithServiceProviderWithScopeValidation()
    {
        // Arrange
        var builder = new WebAssemblyHostBuilder(new TestInternalJSImportMethods(environment: "Development"));

        builder.Services.AddScoped<StringBuilder>();
        builder.Services.AddSingleton<TestServiceThatTakesStringBuilder>();

        // Act & Assert
        var exception = Assert.Throws<AggregateException>(() => builder.Build());
        Assert.Contains("Cannot consume scoped service", exception.Message);
    }

    [Fact]
    public void Build_InProduction_ConfiguresWithServiceProviderWithScopeValidation()
    {
        // Arrange
        var builder = new WebAssemblyHostBuilder(new TestInternalJSImportMethods());

        builder.Services.AddScoped<StringBuilder>();
        builder.Services.AddSingleton<TestServiceThatTakesStringBuilder>();

        // Act
        var host = builder.Build();

        // Assert
        Assert.NotNull(host.Services.GetRequiredService<StringBuilder>());
        Assert.NotNull(host.Services.GetRequiredService<TestServiceThatTakesStringBuilder>());
    }

    [Fact]
    public void Builder_InDevelopment_SetsHostEnvironmentProperty()
    {
        // Arrange
        var builder = new WebAssemblyHostBuilder(new TestInternalJSImportMethods(environment: "Development"));

        // Assert
        Assert.NotNull(builder.HostEnvironment);
        Assert.True(WebAssemblyHostEnvironmentExtensions.IsDevelopment(builder.HostEnvironment));
    }

    [Fact]
    public void Builder_CreatesNavigationManager()
    {
        // Arrange
        var builder = new WebAssemblyHostBuilder(new TestInternalJSImportMethods(environment: "Development"));

        // Act
        var host = builder.Build();

        // Assert
        var navigationManager = host.Services.GetRequiredService<NavigationManager>();
        Assert.NotNull(navigationManager);
        Assert.Equal("https://www.example.com/", navigationManager.BaseUri);
        Assert.Equal("https://www.example.com/awesome-part-that-will-be-truncated-in-tests/cool", navigationManager.Uri);
    }

    private class TestServiceThatTakesStringBuilder
    {
        public TestServiceThatTakesStringBuilder(StringBuilder builder) { }
    }

    private class MyFakeDIBuilderThing
    {
        public MyFakeDIBuilderThing(IServiceCollection serviceCollection)
        {
            ServiceCollection = serviceCollection;
        }

        public IServiceCollection ServiceCollection { get; }
    }

    private class MyFakeServiceProviderFactory : IServiceProviderFactory<MyFakeDIBuilderThing>
    {
        public bool CreateServiceProviderCalled { get; set; }

        public MyFakeDIBuilderThing CreateBuilder(IServiceCollection services)
        {
            return new MyFakeDIBuilderThing(services);
        }

        public IServiceProvider CreateServiceProvider(MyFakeDIBuilderThing containerBuilder)
        {
            // This is the best way to test the factory was actually used. The Host doesn't
            // expose the *root* service provider, only a scoped instance. So we can return
            // a different type here, but we have no way to inspect it.
            CreateServiceProviderCalled = true;
            return containerBuilder.ServiceCollection.BuildServiceProvider();
        }
    }

    [Fact]
    public void Build_AddsConfigurationToServices()
    {
        // Arrange
        var builder = new WebAssemblyHostBuilder(new TestInternalJSImportMethods());

        builder.Configuration.AddInMemoryCollection(new[]
        {
                new KeyValuePair<string, string>("key", "value"),
            });

        // Act
        var host = builder.Build();

        // Assert
        var configuration = host.Services.GetRequiredService<IConfiguration>();
        Assert.Equal("value", configuration["key"]);
    }

    private static IReadOnlyList<Type> DefaultServiceTypes
    {
        get
        {
            return new Type[]
            {
                    typeof(IJSRuntime),
                    typeof(NavigationManager),
                    typeof(INavigationInterception),
                    typeof(ILoggerFactory),
                    typeof(ILogger<>),
                    typeof(IWebAssemblyHostEnvironment),
            };
        }
    }

    [Fact]
    public void Constructor_AddsDefaultServices()
    {
        // Arrange & Act
        var builder = new WebAssemblyHostBuilder(new TestInternalJSImportMethods());

        foreach (var type in DefaultServiceTypes)
        {
            Assert.Single(builder.Services, d => d.ServiceType == type);
        }
    }

    [Fact]
    public void Builder_SupportsConfiguringLogging()
    {
        // Arrange
        var builder = new WebAssemblyHostBuilder(new TestInternalJSImportMethods());
        var provider = new Mock<ILoggerProvider>();

        // Act
        builder.Logging.AddProvider(provider.Object);
        var host = builder.Build();

        // Assert
        var loggerProvider = host.Services.GetRequiredService<ILoggerProvider>();
        Assert.NotNull(loggerProvider);
        Assert.Equal<ILoggerProvider>(provider.Object, loggerProvider);

    }

    [Fact]
    public void UseDefaultServiceProvider_DetectsCircularDependencies()
    {
        // Arrange
        var builder = new WebAssemblyHostBuilder(new TestInternalJSImportMethods());

        // Add a circular dependency
        builder.Services.AddScoped<CircularServiceA>();
        builder.Services.AddScoped<CircularServiceB>();

        // Act
        builder.UseDefaultServiceProvider(options =>
        {
            options.ValidateOnBuild = true;
        });

        // Assert
        var exception = Assert.Throws<AggregateException>(() => builder.Build());
        Assert.Contains("circular dependency", exception.Message.ToLowerInvariant());
    }

    [Fact]
    public void UseDefaultServiceProvider_EnvironmentOverload_WorksCorrectly()
    {
        // Arrange
        var builder = new WebAssemblyHostBuilder(new TestInternalJSImportMethods(environment: "Development"));

        // Act
        builder.UseDefaultServiceProvider((env, options) =>
        {
            options.ValidateOnBuild = env.IsDevelopment();
        });

        var host = builder.Build();

        // Assert
        Assert.NotNull(host);
    }

    [Fact]
    public void DefaultServiceProviderOptions_InDevelopment_ValidatesOnBuild()
    {
        // Arrange
        var builder = new WebAssemblyHostBuilder(new TestInternalJSImportMethods(environment: "Development"));

        // Add a circular dependency - should throw due to default ValidateOnBuild=true in development
        builder.Services.AddScoped<CircularServiceA>();
        builder.Services.AddScoped<CircularServiceB>();

        // Act & Assert
        var exception = Assert.Throws<AggregateException>(() => builder.Build());
        Assert.Contains("circular dependency", exception.Message.ToLowerInvariant());
    }

    // Helper classes for testing circular dependencies
    private class CircularServiceA
    {
        public CircularServiceA(CircularServiceB serviceB) { }
    }

    private class CircularServiceB
    {
        public CircularServiceB(CircularServiceA serviceA) { }
    }

    [Fact]
    public void Configuration_IncludesEnvironmentVariables_WhenAddedExplicitly()
    {
        // Arrange
        var testEnvVarKey = $"TEST_WASM_CONFIG_{Guid.NewGuid():N}";
        var testEnvVarValue = "test-value-12345";

        try
        {
            // Set an environment variable before creating the builder
            Environment.SetEnvironmentVariable(testEnvVarKey, testEnvVarValue);

            var builder = new WebAssemblyHostBuilder(new TestInternalJSImportMethods());

            // This mimics what CreateDefault now does
            builder.Configuration.AddEnvironmentVariables();

            // Act
            var host = builder.Build();

            // Assert
            var configuration = host.Services.GetRequiredService<IConfiguration>();
            Assert.Equal(testEnvVarValue, configuration[testEnvVarKey]);
        }
        finally
        {
            // Clean up the environment variable
            Environment.SetEnvironmentVariable(testEnvVarKey, null);
        }
    }
}
