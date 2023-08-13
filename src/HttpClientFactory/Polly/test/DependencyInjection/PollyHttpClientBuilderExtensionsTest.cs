// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Http.Logging;
using Polly;
using Polly.Registry;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection;

// These are integration tests that verify basic end-to-ends.
public class PollyHttpClientBuilderExtensionsTest
{
    public PollyHttpClientBuilderExtensionsTest()
    {
        PrimaryHandler = new FaultyMessageHandler();

        NoOpPolicy = Policy.NoOpAsync<HttpResponseMessage>();
        RetryPolicy = Policy.Handle<OverflowException>().OrResult<HttpResponseMessage>(r => false).RetryAsync();
    }

    private FaultyMessageHandler PrimaryHandler { get; }

    // Allows the exception from our handler to propegate
    private IAsyncPolicy<HttpResponseMessage> NoOpPolicy { get; }

    // Matches what our client handler does
    private IAsyncPolicy<HttpResponseMessage> RetryPolicy { get; }

    [Fact]
    public async Task AddPolicyHandler_Policy_AddsPolicyHandler()
    {
        var serviceCollection = new ServiceCollection();

        IList<DelegatingHandler> additionalHandlers = null;

        // Act1
        serviceCollection.AddHttpClient("example.com", c => c.BaseAddress = new Uri("http://example.com"))
            .AddPolicyHandler(RetryPolicy)
            .ConfigurePrimaryHttpMessageHandler(() => PrimaryHandler)
            .ConfigureAdditionalHttpMessageHandlers((handlers, _) => additionalHandlers = handlers);

        var services = serviceCollection.BuildServiceProvider();
        var factory = services.GetRequiredService<IHttpClientFactory>();

        // Act2
        var client = factory.CreateClient("example.com");

        // Assert
        Assert.NotNull(client);

        Assert.Collection(
            additionalHandlers,
            h => Assert.IsType<LoggingScopeHttpMessageHandler>(h),
            h => Assert.IsType<PolicyHttpMessageHandler>(h),
            h => Assert.IsType<LoggingHttpMessageHandler>(h));

        // Act 3
        var response = await client.SendAsync(new HttpRequestMessage());

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task AddPolicyHandler_PolicySelector_AddsPolicyHandler()
    {
        var serviceCollection = new ServiceCollection();

        IList<DelegatingHandler> additionalHandlers = null;

        // Act1
        serviceCollection.AddHttpClient("example.com", c => c.BaseAddress = new Uri("http://example.com"))
            .AddPolicyHandler((req) => req.RequestUri.AbsolutePath == "/" ? RetryPolicy : NoOpPolicy)
            .ConfigurePrimaryHttpMessageHandler(() => PrimaryHandler)
            .ConfigureAdditionalHttpMessageHandlers((handlers, _) => additionalHandlers = handlers);

        var services = serviceCollection.BuildServiceProvider();
        var factory = services.GetRequiredService<IHttpClientFactory>();

        // Act2
        var client = factory.CreateClient("example.com");

        // Assert
        Assert.NotNull(client);

        Assert.Collection(
            additionalHandlers,
            h => Assert.IsType<LoggingScopeHttpMessageHandler>(h),
            h => Assert.IsType<PolicyHttpMessageHandler>(h),
            h => Assert.IsType<LoggingHttpMessageHandler>(h));

        // Act 3
        var response = await client.SendAsync(new HttpRequestMessage());

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // Act 4
        await Assert.ThrowsAsync<OverflowException>(() => client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/throw")));
    }

    [Fact]
    public async Task AddPolicyHandler_PolicySelectorWithServices_AddsPolicyHandler()
    {
        var serviceCollection = new ServiceCollection();

        IList<DelegatingHandler> additionalHandlers = null;

        // Act1
        serviceCollection.AddHttpClient("example.com", c => c.BaseAddress = new Uri("http://example.com"))
            .AddPolicyHandler((req) => req.RequestUri.AbsolutePath == "/" ? RetryPolicy : NoOpPolicy)
            .ConfigurePrimaryHttpMessageHandler(() => PrimaryHandler)
            .ConfigureAdditionalHttpMessageHandlers((handlers, _) => additionalHandlers = handlers);

        var services = serviceCollection.BuildServiceProvider();
        var factory = services.GetRequiredService<IHttpClientFactory>();

        // Act2
        var client = factory.CreateClient("example.com");

        // Assert
        Assert.NotNull(client);

        Assert.Collection(
            additionalHandlers,
            h => Assert.IsType<LoggingScopeHttpMessageHandler>(h),
            h => Assert.IsType<PolicyHttpMessageHandler>(h),
            h => Assert.IsType<LoggingHttpMessageHandler>(h));

        // Act 3
        var response = await client.SendAsync(new HttpRequestMessage());

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // Act 4
        await Assert.ThrowsAsync<OverflowException>(() => client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/throw")));
    }

    [Fact]
    public async Task AddPolicyHandlerFromRegistry_Name_AddsPolicyHandler()
    {
        var serviceCollection = new ServiceCollection();

        var registry = serviceCollection.AddPolicyRegistry();
        registry.Add<IAsyncPolicy<HttpResponseMessage>>("retry", RetryPolicy);

        IList<DelegatingHandler> additionalHandlers = null;

        // Act1
        serviceCollection.AddHttpClient("example.com", c => c.BaseAddress = new Uri("http://example.com"))
            .AddPolicyHandlerFromRegistry("retry")
            .ConfigurePrimaryHttpMessageHandler(() => PrimaryHandler)
            .ConfigureAdditionalHttpMessageHandlers((handlers, _) => additionalHandlers = handlers);

        var services = serviceCollection.BuildServiceProvider();
        var factory = services.GetRequiredService<IHttpClientFactory>();

        // Act2
        var client = factory.CreateClient("example.com");

        // Assert
        Assert.NotNull(client);

        Assert.Collection(
            additionalHandlers,
            h => Assert.IsType<LoggingScopeHttpMessageHandler>(h),
            h => Assert.IsType<PolicyHttpMessageHandler>(h),
            h => Assert.IsType<LoggingHttpMessageHandler>(h));

        // Act 3
        var response = await client.SendAsync(new HttpRequestMessage());

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task AddPolicyHandlerFromRegistry_Dynamic_AddsPolicyHandler()
    {
        var serviceCollection = new ServiceCollection();

        var registry = serviceCollection.AddPolicyRegistry();
        registry.Add<IAsyncPolicy<HttpResponseMessage>>("noop", NoOpPolicy);
        registry.Add<IAsyncPolicy<HttpResponseMessage>>("retry", RetryPolicy);

        IList<DelegatingHandler> additionalHandlers = null;

        // Act1
        serviceCollection.AddHttpClient("example.com", c => c.BaseAddress = new Uri("http://example.com"))
            .AddPolicyHandlerFromRegistry((reg, req) =>
            {
                return req.RequestUri.AbsolutePath == "/" ?
                    reg.Get<IAsyncPolicy<HttpResponseMessage>>("retry") :
                    reg.Get<IAsyncPolicy<HttpResponseMessage>>("noop");
            })
            .ConfigurePrimaryHttpMessageHandler(() => PrimaryHandler)
            .ConfigureAdditionalHttpMessageHandlers((handlers, _) => additionalHandlers = handlers);

        var services = serviceCollection.BuildServiceProvider();
        var factory = services.GetRequiredService<IHttpClientFactory>();

        // Act2
        var client = factory.CreateClient("example.com");

        // Assert
        Assert.NotNull(client);

        Assert.Collection(
            additionalHandlers,
            h => Assert.IsType<LoggingScopeHttpMessageHandler>(h),
            h => Assert.IsType<PolicyHttpMessageHandler>(h),
            h => Assert.IsType<LoggingHttpMessageHandler>(h));

        // Act 3
        var response = await client.SendAsync(new HttpRequestMessage());

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // Act 4
        await Assert.ThrowsAsync<OverflowException>(() => client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/throw")));
    }

    [Theory]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData((HttpStatusCode)500)]
    [InlineData((HttpStatusCode)501)]
    [InlineData((HttpStatusCode)502)]
    [InlineData((HttpStatusCode)503)]
    public async Task AddTransientHttpErrorPolicy_AddsPolicyHandler_HandlesStatusCode(HttpStatusCode statusCode)
    {
        // Arrange
        using var handler = new SequenceMessageHandler()
        {
            Responses =
                {
                    (req) => new HttpResponseMessage(statusCode),
                    (req) => new HttpResponseMessage(HttpStatusCode.OK),
                },
        };

        var serviceCollection = new ServiceCollection();

        IList<DelegatingHandler> additionalHandlers = null;

        // Act1
        serviceCollection.AddHttpClient("example.com", c => c.BaseAddress = new Uri("http://example.com"))
            .AddTransientHttpErrorPolicy(b => b.RetryAsync(5))
            .ConfigurePrimaryHttpMessageHandler(() => handler)
            .ConfigureAdditionalHttpMessageHandlers((handlers, _) => additionalHandlers = handlers);

        var services = serviceCollection.BuildServiceProvider();
        var factory = services.GetRequiredService<IHttpClientFactory>();

        // Act2
        var client = factory.CreateClient("example.com");

        // Assert
        Assert.NotNull(client);

        Assert.Collection(
            additionalHandlers,
            h => Assert.IsType<LoggingScopeHttpMessageHandler>(h),
            h => Assert.IsType<PolicyHttpMessageHandler>(h),
            h => Assert.IsType<LoggingHttpMessageHandler>(h));

        // Act 3
        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/"));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddTransientHttpErrorPolicy_AddsPolicyHandler_HandlesHttpRequestException()
    {
        // Arrange
        using var handler = new SequenceMessageHandler()
        {
            Responses =
                {
                    (req) => { throw new HttpRequestException("testing..."); },
                    (req) => new HttpResponseMessage(HttpStatusCode.OK),
                },
        };

        var serviceCollection = new ServiceCollection();

        IList<DelegatingHandler> additionalHandlers = null;

        // Act1
        serviceCollection.AddHttpClient("example.com", c => c.BaseAddress = new Uri("http://example.com"))
            .AddTransientHttpErrorPolicy(b => b.RetryAsync(5))
            .ConfigurePrimaryHttpMessageHandler(() => handler)
            .ConfigureAdditionalHttpMessageHandlers((handlers, _) => additionalHandlers = handlers);

        var services = serviceCollection.BuildServiceProvider();
        var factory = services.GetRequiredService<IHttpClientFactory>();

        // Act2
        var client = factory.CreateClient("example.com");

        // Assert
        Assert.NotNull(client);

        Assert.Collection(
            additionalHandlers,
            h => Assert.IsType<LoggingScopeHttpMessageHandler>(h),
            h => Assert.IsType<PolicyHttpMessageHandler>(h),
            h => Assert.IsType<LoggingHttpMessageHandler>(h));

        // Act 3
        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/"));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddPolicyHandlerFromRegistry_PolicySelectorWithKey_AddsPolicyHandler()
    {
        var serviceCollection = new ServiceCollection();
        var registry = serviceCollection.AddPolicyRegistry();
        IList<DelegatingHandler> additionalHandlers = null;

        // Act1
        serviceCollection.AddHttpClient("Service")
            .AddPolicyHandler(
            (sp, req, key) =>
            {
                return RetryPolicy;
            },
            (r) =>
            {
                return r.RequestUri.Host;
            }
            )
            .ConfigurePrimaryHttpMessageHandler(() => PrimaryHandler)
            .ConfigureAdditionalHttpMessageHandlers((handlers, _) => additionalHandlers = handlers);

        var services = serviceCollection.BuildServiceProvider();
        var factory = services.GetRequiredService<IHttpClientFactory>();

        // Act2
        var client = factory.CreateClient("Service");
        // Assert
        Assert.NotNull(client);

        Assert.Collection(
            additionalHandlers,
            h => Assert.IsType<LoggingScopeHttpMessageHandler>(h),
            h => Assert.IsType<PolicyHttpMessageHandler>(h),
            h => Assert.IsType<LoggingHttpMessageHandler>(h));

        // Act 3
        var request = new HttpRequestMessage(HttpMethod.Get, "http://host1/Service1/");
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.True(registry.ContainsKey("host1"));
        Assert.Equal(1, registry.Count);

        // Act 4
        request = new HttpRequestMessage(HttpMethod.Get, "http://host1/Service1/");
        response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.True(registry.ContainsKey("host1"));
        Assert.Equal(1, registry.Count);

        // Act 4
        request = new HttpRequestMessage(HttpMethod.Get, "http://host2/Service1/");
        response = await client.SendAsync(request);

        // Assert policy count
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(2, registry.Count);
        Assert.True(registry.ContainsKey("host1"));
        Assert.True(registry.ContainsKey("host2"));
    }

    [Fact]
    public async Task AddPolicyHandlerFromRegistry_WithConfigureDelegate_AddsPolicyHandler()
    {
        var options = new PollyPolicyOptions()
        {
            PolicyName = "retrypolicy"
        };

        var serviceCollection = new ServiceCollection();

        serviceCollection.AddSingleton(options);

        serviceCollection.AddPolicyRegistry((serviceProvider, registry) =>
        {
            string policyName = serviceProvider.GetRequiredService<PollyPolicyOptions>().PolicyName;

            registry.Add<IAsyncPolicy<HttpResponseMessage>>(policyName, RetryPolicy);
        });

        IList<DelegatingHandler> additionalHandlers = null;

        // Act1
        serviceCollection.AddHttpClient("example.com", c => c.BaseAddress = new Uri("http://example.com"))
            .AddPolicyHandlerFromRegistry(options.PolicyName)
            .ConfigurePrimaryHttpMessageHandler(() => PrimaryHandler)
            .ConfigureAdditionalHttpMessageHandlers((handlers, _) => additionalHandlers = handlers);

        var services = serviceCollection.BuildServiceProvider();
        var factory = services.GetRequiredService<IHttpClientFactory>();

        // Act2
        var client = factory.CreateClient("example.com");

        // Assert
        Assert.NotNull(client);

        Assert.Collection(
            additionalHandlers,
            h => Assert.IsType<LoggingScopeHttpMessageHandler>(h),
            h => Assert.IsType<PolicyHttpMessageHandler>(h),
            h => Assert.IsType<LoggingHttpMessageHandler>(h));

        // Act 3
        var response = await client.SendAsync(new HttpRequestMessage());

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public void AddPolicyHandlerFromRegistry_WithoutConfigureDelegate_AddsPolicyRegistries()
    {
        var serviceCollection = new ServiceCollection();

        // Act
        serviceCollection.AddPolicyRegistry();

        var services = serviceCollection.BuildServiceProvider();
        var registry = services.GetService<IPolicyRegistry<string>>();

        // Assert
        Assert.NotNull(registry);
        Assert.Same(registry, services.GetService<IConcurrentPolicyRegistry<string>>());
        Assert.Same(registry, services.GetService<IReadOnlyPolicyRegistry<string>>());
    }

    [Fact]
    public void AddPolicyHandlerFromRegistry_WithRegistry_AddsPolicyRegistries()
    {
        var serviceCollection = new ServiceCollection();
        var registry = new PolicyRegistry();

        // Act
        serviceCollection.AddPolicyRegistry(registry);

        var services = serviceCollection.BuildServiceProvider();

        // Assert
        Assert.Same(registry, services.GetService<IConcurrentPolicyRegistry<string>>());
        Assert.Same(registry, services.GetService<IPolicyRegistry<string>>());
        Assert.Same(registry, services.GetService<IReadOnlyPolicyRegistry<string>>());
    }

    [Fact]
    public void AddPolicyHandlerFromRegistry_WithConfigureDelegate_AddsPolicyRegistries()
    {
        var serviceCollection = new ServiceCollection();

        // Act
        serviceCollection.AddPolicyRegistry((serviceProvider, registry) =>
        {
            // No-op
        });

        var services = serviceCollection.BuildServiceProvider();
        var registry = services.GetService<IPolicyRegistry<string>>();

        // Assert
        Assert.NotNull(registry);
        Assert.Same(registry, services.GetService<IConcurrentPolicyRegistry<string>>());
        Assert.Same(registry, services.GetService<IReadOnlyPolicyRegistry<string>>());
    }

    [Fact]
    public void AddPolicyRegistry_DoesNotOverrideOrAddExtraRegistrations()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();

        // Act 1
        var existingRegistry = serviceCollection.AddPolicyRegistry();

        // Act 2
        var registry = serviceCollection.AddPolicyRegistry();
        var services = serviceCollection.BuildServiceProvider();

        // Assert
        Assert.NotNull(existingRegistry);
        Assert.Same(existingRegistry, registry);

        Assert.Same(existingRegistry, services.GetService<IPolicyRegistry<string>>());
        Assert.Same(existingRegistry, services.GetService<IConcurrentPolicyRegistry<string>>());
        Assert.Same(existingRegistry, services.GetService<IReadOnlyPolicyRegistry<string>>());

        Assert.Single(serviceCollection, sd => sd.ServiceType == typeof(IPolicyRegistry<string>));
        Assert.Single(serviceCollection, sd => sd.ServiceType == typeof(IReadOnlyPolicyRegistry<string>));
        Assert.Single(serviceCollection, sd => sd.ServiceType == typeof(IConcurrentPolicyRegistry<string>));
    }

    [Theory]
    [InlineData(typeof(IPolicyRegistry<string>))]
    [InlineData(typeof(IReadOnlyPolicyRegistry<string>))]
    [InlineData(typeof(IConcurrentPolicyRegistry<string>))]
    public void AddPolicyRegistry_AddsOnlyMissingRegistrations(Type missingType)
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        var registry = new PolicyRegistry();
        var policyTypes = new List<Type>
        {
            typeof(IPolicyRegistry<string>),
            typeof(IReadOnlyPolicyRegistry<string>),
            typeof(IConcurrentPolicyRegistry<string>)
        };

        // Act 1
        foreach (var policyType in policyTypes.Where(x => x != missingType))
        {
            serviceCollection.AddSingleton(policyType, registry);
        }

        // Act 2
        serviceCollection.AddPolicyRegistry();

        // Assert
        Assert.Single(serviceCollection, sd => sd.ServiceType == typeof(IPolicyRegistry<string>));
        Assert.Single(serviceCollection, sd => sd.ServiceType == typeof(IReadOnlyPolicyRegistry<string>));
        Assert.Single(serviceCollection, sd => sd.ServiceType == typeof(IConcurrentPolicyRegistry<string>));
    }

    // Throws an exception or fails on even numbered requests, otherwise succeeds.
    private class FaultyMessageHandler : DelegatingHandler
    {
        public int CallCount { get; private set; }

        public Func<Exception> CreateException { get; set; } = () => new OverflowException();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (CallCount++ % 2 == 0)
            {
                throw CreateException();
            }
            else
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created));
            }
        }
    }

    private class SequenceMessageHandler : DelegatingHandler
    {
        public int CallCount { get; private set; }

        public List<Func<HttpRequestMessage, HttpResponseMessage>> Responses { get; } = new List<Func<HttpRequestMessage, HttpResponseMessage>>();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var func = Responses[CallCount++ % Responses.Count];
            return Task.FromResult(func(request));
        }
    }

    private class PollyPolicyOptions
    {
        public string PolicyName { get; set; }
    }
}
