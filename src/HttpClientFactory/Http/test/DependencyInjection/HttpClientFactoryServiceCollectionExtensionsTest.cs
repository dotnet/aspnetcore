// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Http.Logging;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection
{
    // These are mostly integration tests that verify the configuration experience.
    public class HttpClientFactoryServiceCollectionExtensionsTest
    {
        [Fact] // Verifies that AddHttpClient is enough to get the factory and make clients.
        public void AddHttpClient_IsSelfContained_CanCreateClient()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();

            // Act1
            serviceCollection.AddHttpClient(); 

            var services = serviceCollection.BuildServiceProvider();
            var options = services.GetRequiredService<IOptionsMonitor<HttpClientFactoryOptions>>();

            var factory = services.GetRequiredService<IHttpClientFactory>();

            // Act2
            var client = factory.CreateClient();

            // Assert
            Assert.NotNull(client);
        }

        [Fact] // Verifies that AddHttpClient is enough to get the factory and make handlers.
        public void AddHttpClient_IsSelfContained_CanCreateHandler()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();

            // Act1
            serviceCollection.AddHttpClient();

            var services = serviceCollection.BuildServiceProvider();

            var factory = services.GetRequiredService<IHttpMessageHandlerFactory>();

            // Act2
            var handler = factory.CreateHandler();

            // Assert
            Assert.NotNull(handler);
        }

        [Fact]
        public void AddHttpClient_WithDefaultName_ConfiguresDefaultClient()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();

            // Act1
            serviceCollection.AddHttpClient(Options.Options.DefaultName, c => c.BaseAddress = new Uri("http://example.com/"));

            var services = serviceCollection.BuildServiceProvider();

            var factory = services.GetRequiredService<IHttpClientFactory>();

            // Act2
            var client = factory.CreateClient();

            // Assert
            Assert.NotNull(client);
            Assert.Equal("http://example.com/", client.BaseAddress.AbsoluteUri);
        }

        [Fact]
        public void AddHttpClient_WithName_ConfiguresNamedClient()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();

            // Act1
            serviceCollection.AddHttpClient("example.com", c => c.BaseAddress = new Uri("http://example.com/"));

            var services = serviceCollection.BuildServiceProvider();
            var factory = services.GetRequiredService<IHttpClientFactory>();

            // Act2
            var client = factory.CreateClient("example.com");

            // Assert
            Assert.NotNull(client);
            Assert.Equal("http://example.com/", client.BaseAddress.AbsoluteUri);
        }

        [Fact]
        public void AddHttpClient_WithTypedClient_ConfiguresNamedClient()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();

            serviceCollection.Configure<HttpClientFactoryOptions>(nameof(TestTypedClient), options =>
            {
                options.HttpClientActions.Add((c) => c.BaseAddress = new Uri("http://example.com"));
            });

            // Act
            serviceCollection.AddHttpClient<TestTypedClient>();

            var services = serviceCollection.BuildServiceProvider();

            // Act2
            var client = services.GetRequiredService<TestTypedClient>();

            // Assert
            Assert.Equal("http://example.com/", client.HttpClient.BaseAddress.AbsoluteUri);
        }

        [Fact]
        public void AddHttpClient_WithGenericTypedClient_ConfiguresNamedClient()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();

            serviceCollection.Configure<HttpClientFactoryOptions>("TestGenericTypedClient<string>", options =>
            {
                options.HttpClientActions.Add((c) => c.BaseAddress = new Uri("http://example.com"));
            });

            // Act
            serviceCollection.AddHttpClient<TestGenericTypedClient<string>>();

            var services = serviceCollection.BuildServiceProvider();

            // Act2
            var client = services.GetRequiredService<TestGenericTypedClient<string>>();

            // Assert
            Assert.Equal("http://example.com/", client.HttpClient.BaseAddress.AbsoluteUri);
        }

        [Fact]
        public void AddHttpClient_WithTypedClientAndImplementation_ConfiguresNamedClient()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();

            serviceCollection.Configure<HttpClientFactoryOptions>(nameof(ITestTypedClient), options =>
            {
                options.HttpClientActions.Add((c) => c.BaseAddress = new Uri("http://example.com"));
            });

            // Act
            serviceCollection.AddHttpClient<ITestTypedClient, TestTypedClient>();

            var services = serviceCollection.BuildServiceProvider();

            // Act2
            var client = services.GetRequiredService<ITestTypedClient>();

            // Assert
            Assert.Equal("http://example.com/", client.HttpClient.BaseAddress.AbsoluteUri);
        }

        [Fact]
        public void AddHttpClient_WithTypedClient_AndName_ConfiguresNamedClient()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();

            serviceCollection.Configure<HttpClientFactoryOptions>("test", options =>
            {
                options.HttpClientActions.Add((c) => c.BaseAddress = new Uri("http://example.com"));
            });

            // Act
            serviceCollection.AddHttpClient<TestTypedClient>("test");

            var services = serviceCollection.BuildServiceProvider();

            // Act2
            var client = services.GetRequiredService<TestTypedClient>();

            // Assert
            Assert.Equal("http://example.com/", client.HttpClient.BaseAddress.AbsoluteUri);
        }

        [Fact]
        public void AddHttpClient_WithTypedClientAndImplementation_AndName_ConfiguresNamedClient()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();

            serviceCollection.Configure<HttpClientFactoryOptions>("test", options =>
            {
                options.HttpClientActions.Add((c) => c.BaseAddress = new Uri("http://example.com"));
            });

            // Act
            serviceCollection.AddHttpClient<ITestTypedClient, TestTypedClient>("test");

            var services = serviceCollection.BuildServiceProvider();

            // Act2
            var client = services.GetRequiredService<ITestTypedClient>();

            // Assert
            Assert.Equal("http://example.com/", client.HttpClient.BaseAddress.AbsoluteUri);
        }

        [Fact]
        public void AddHttpClient_WithTypedClient_AndDelegate_ConfiguresNamedClient()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();

            serviceCollection.Configure<HttpClientFactoryOptions>(nameof(TestTypedClient), options =>
            {
                options.HttpClientActions.Add((c) => c.BaseAddress = new Uri("http://example.com"));
            });

            // Act
            serviceCollection.AddHttpClient<TestTypedClient>((c) =>
            {
                Assert.Equal("http://example.com/", c.BaseAddress.AbsoluteUri);
                c.BaseAddress = new Uri("http://example2.com");
            });

            var services = serviceCollection.BuildServiceProvider();

            // Act2
            var client = services.GetRequiredService<TestTypedClient>();

            // Assert
            Assert.Equal("http://example2.com/", client.HttpClient.BaseAddress.AbsoluteUri);
        }

        [Fact]
        public void AddHttpClient_WithTypedClientAndImplementation_AndDelegate_ConfiguresNamedClient()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();

            serviceCollection.Configure<HttpClientFactoryOptions>(nameof(ITestTypedClient), options =>
            {
                options.HttpClientActions.Add((c) => c.BaseAddress = new Uri("http://example.com"));
            });

            // Act
            serviceCollection.AddHttpClient<ITestTypedClient, TestTypedClient>((c) =>
            {
                Assert.Equal("http://example.com/", c.BaseAddress.AbsoluteUri);
                c.BaseAddress = new Uri("http://example2.com");
            });

            var services = serviceCollection.BuildServiceProvider();

            // Act2
            var client = services.GetRequiredService<ITestTypedClient>();

            // Assert
            Assert.Equal("http://example2.com/", client.HttpClient.BaseAddress.AbsoluteUri);
        }

        [Fact]
        public void AddHttpClient_AddTypedClient_ConfiguresNamedClient()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();

            serviceCollection.Configure<HttpClientFactoryOptions>("test", options =>
            {
                options.HttpClientActions.Add((c) => c.BaseAddress = new Uri("http://example.com"));
            });

            // Act
            serviceCollection.AddHttpClient("test").AddTypedClient<TestTypedClient>();

            var services = serviceCollection.BuildServiceProvider();

            // Act2
            var client = services.GetRequiredService<TestTypedClient>();

            // Assert
            Assert.Equal("http://example.com/", client.HttpClient.BaseAddress.AbsoluteUri);
        }

        [Fact]
        public void AddHttpClient_AddTypedClientAndImplementation_ConfiguresNamedClient()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();

            serviceCollection.Configure<HttpClientFactoryOptions>("test", options =>
            {
                options.HttpClientActions.Add((c) => c.BaseAddress = new Uri("http://example.com"));
            });

            // Act
            serviceCollection.AddHttpClient("test").AddTypedClient<ITestTypedClient, TestTypedClient>();

            var services = serviceCollection.BuildServiceProvider();

            // Act2
            var client = services.GetRequiredService<ITestTypedClient>();

            // Assert
            Assert.Equal("http://example.com/", client.HttpClient.BaseAddress.AbsoluteUri);
        }

        [Fact]
        public void AddHttpClient_AddTypedClient_WithDelegate_ConfiguresNamedClient()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();

            serviceCollection.Configure<HttpClientFactoryOptions>("test", options =>
            {
                options.HttpClientActions.Add((c) => c.BaseAddress = new Uri("http://example.com"));
            });

            // Act
            serviceCollection.AddHttpClient("test").AddTypedClient<TestTypedClient>((c) =>
            {
                Assert.Equal("http://example.com/", c.BaseAddress.AbsoluteUri);
                c.BaseAddress = new Uri("http://example2.com");
                return new TestTypedClient(c);
            });

            var services = serviceCollection.BuildServiceProvider();

            // Act2
            var client = services.GetRequiredService<TestTypedClient>();

            // Assert
            Assert.Equal("http://example2.com/", client.HttpClient.BaseAddress.AbsoluteUri);
        }

        [Fact]
        public void AddHttpClient_AddTypedClient_WithServiceDelegate_ConfiguresNamedClient()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();

            serviceCollection.Configure<HttpClientFactoryOptions>("test", options =>
            {
                options.HttpClientActions.Add((c) => c.BaseAddress = new Uri("http://example.com"));
            });

            // Act
            serviceCollection.AddHttpClient("test").AddTypedClient<TestTypedClient>((c,s) =>
            {
                Assert.Equal("http://example.com/", c.BaseAddress.AbsoluteUri);
                c.BaseAddress = new Uri("http://example2.com");
                return new TestTypedClient(c);
            });

            var services = serviceCollection.BuildServiceProvider();

            // Act2
            var client = services.GetRequiredService<TestTypedClient>();

            // Assert
            Assert.Equal("http://example2.com/", client.HttpClient.BaseAddress.AbsoluteUri);
        }

        [Fact]
        public void AddHttpClient_WithTypedClient_AndName_AndDelegate_ConfiguresNamedClient()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();

            serviceCollection.Configure<HttpClientFactoryOptions>("test", options =>
            {
                options.HttpClientActions.Add((c) => c.BaseAddress = new Uri("http://example.com"));
            });

            // Act
            serviceCollection.AddHttpClient<TestTypedClient>("test", (c) =>
            {
                Assert.Equal("http://example.com/", c.BaseAddress.AbsoluteUri);
                c.BaseAddress = new Uri("http://example2.com");
            });

            var services = serviceCollection.BuildServiceProvider();

            // Act2
            var client = services.GetRequiredService<TestTypedClient>();

            // Assert
            Assert.Equal("http://example2.com/", client.HttpClient.BaseAddress.AbsoluteUri);
        }

        [Fact]
        public void AddHttpMessageHandler_WithName_NewHandlerIsSurroundedByLogging_ForHttpClient()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();

            HttpMessageHandlerBuilder builder = null;

            // Act1
            serviceCollection.AddHttpClient("example.com").ConfigureHttpMessageHandlerBuilder(b =>
            {
                builder = b;

                b.AdditionalHandlers.Add(Mock.Of<DelegatingHandler>());
            });

            var services = serviceCollection.BuildServiceProvider();
            var options = services.GetRequiredService<IOptionsMonitor<HttpClientFactoryOptions>>();

            var factory = services.GetRequiredService<IHttpClientFactory>();

            // Act2
            var client = factory.CreateClient("example.com");

            // Assert
            Assert.NotNull(client);

            Assert.Collection(
                builder.AdditionalHandlers,
                h => Assert.IsType<LoggingScopeHttpMessageHandler>(h),
                h => Assert.NotNull(h),
                h => Assert.IsType<LoggingHttpMessageHandler>(h));
        }

        [Fact]
        public void AddHttpClient_WithTypedClient_AndServiceDelegate_ConfiguresClient()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();

            serviceCollection.Configure<OtherTestOptions>(options =>
            {
                options.BaseAddress = "http://example.com/";
            });

            // Act1
            serviceCollection.AddHttpClient<TestTypedClient>((s,c) =>
            {
                var options = s.GetRequiredService<IOptions<OtherTestOptions>>();
                c.BaseAddress = new Uri(options.Value.BaseAddress);
            });

            var services = serviceCollection.BuildServiceProvider();

            // Act2
            var client = services.GetRequiredService<TestTypedClient>();
        
            // Assert
            Assert.Equal("http://example.com/", client.HttpClient.BaseAddress.AbsoluteUri);
        }

        [Fact]
        public void AddHttpClient_WithTypedClientAndImplementation_AndServiceDelegate_ConfiguresClient()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();

            serviceCollection.Configure<OtherTestOptions>(options =>
            {
                options.BaseAddress = "http://example.com/";
            });

            // Act1
            serviceCollection.AddHttpClient<ITestTypedClient, TestTypedClient>((s,c) =>
            {
                var options = s.GetRequiredService<IOptions<OtherTestOptions>>();
                c.BaseAddress = new Uri(options.Value.BaseAddress);
            });

            var services = serviceCollection.BuildServiceProvider();

            // Act2
            var client = services.GetRequiredService<ITestTypedClient>();
        
            // Assert
            Assert.Equal("http://example.com/", client.HttpClient.BaseAddress.AbsoluteUri);
        }

        [Fact]
        public void AddHttpClient_WithTypedClient_AndServiceDelegate_ConfiguresNamedClient()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();

            serviceCollection.Configure<OtherTestOptions>(options =>
            {
                options.BaseAddress = "http://example.com/";
            });

            // Act1
            serviceCollection.AddHttpClient<TestTypedClient>("test", (s,c) =>
            {
                var options = s.GetRequiredService<IOptions<OtherTestOptions>>();
                c.BaseAddress = new Uri(options.Value.BaseAddress);
            });

            var services = serviceCollection.BuildServiceProvider();

            // Act2
            var client = services.GetRequiredService<TestTypedClient>();
        
            // Assert
            Assert.Equal("http://example.com/", client.HttpClient.BaseAddress.AbsoluteUri);
        }

        [Fact]
        public void AddHttpClient_WithTypedClientAndImplementation_AndServiceDelegate_ConfiguresNamedClient()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();

            serviceCollection.Configure<OtherTestOptions>(options =>
            {
                options.BaseAddress = "http://example.com/";
            });

            // Act1
            serviceCollection.AddHttpClient<ITestTypedClient, TestTypedClient>("test", (s,c) =>
            {
                var options = s.GetRequiredService<IOptions<OtherTestOptions>>();
                c.BaseAddress = new Uri(options.Value.BaseAddress);
            });

            var services = serviceCollection.BuildServiceProvider();

            // Act2
            var client = services.GetRequiredService<ITestTypedClient>();
        
            // Assert
            Assert.Equal("http://example.com/", client.HttpClient.BaseAddress.AbsoluteUri);
        }

        [Fact]
        public void AddHttpMessageHandler_WithName_NewHandlerIsSurroundedByLogging_ForHttpMessageHandler()
        {
            var serviceCollection = new ServiceCollection();

            HttpMessageHandlerBuilder builder = null;

            // Act1
            serviceCollection.AddHttpClient("example.com").ConfigureHttpMessageHandlerBuilder(b =>
            {
                builder = b;

                b.AdditionalHandlers.Add(Mock.Of<DelegatingHandler>());
            });

            var services = serviceCollection.BuildServiceProvider();

            var factory = services.GetRequiredService<IHttpMessageHandlerFactory>();

            // Act2
            var handler = factory.CreateHandler("example.com");

            // Assert
            Assert.NotNull(handler);
            Assert.IsNotType<LoggingScopeHttpMessageHandler>(handler);

            Assert.Collection(
                builder.AdditionalHandlers,
                h => Assert.IsType<LoggingScopeHttpMessageHandler>(h),
                h => Assert.NotNull(h),
                h => Assert.IsType<LoggingHttpMessageHandler>(h));
        }

        [Fact] // Verifies IHttpClientFactory and IHttpMessageHandlerFactory are backed by the same internal handlers
        public void AddHttpClient_ProvidesSameImplementationForClientsAndHandlers()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();

            // Act
            serviceCollection.AddHttpClient();

            var services = serviceCollection.BuildServiceProvider();

            var clientFactory = services.GetRequiredService<IHttpClientFactory>();
            var handlerFactory = services.GetRequiredService<IHttpMessageHandlerFactory>();

            // Assert
            Assert.Same(clientFactory, handlerFactory);
        }

        [Fact]
        public async Task AddHttpClient_MessageHandler_SingletonDependency()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<SingletonService>();
            serviceCollection.AddTransient<HandlerWithSingletonService>();
            serviceCollection
                .AddHttpClient<TypedClientWithSingletonService>("test")
                .AddHttpMessageHandler<HandlerWithSingletonService>();

            var services = serviceCollection.BuildServiceProvider(validateScopes: true);

            // Act
            var client = services.GetRequiredService<TypedClientWithSingletonService>();

            // Assert
            var request = new HttpRequestMessage(HttpMethod.Get, "http://example.com/");
            var response = await client.HttpClient.SendAsync(request);

            Assert.Same(
                services.GetRequiredService<SingletonService>(),
                request.Properties[nameof(SingletonService)]);

            Assert.Same(
                client.Service,
                request.Properties[nameof(SingletonService)]);
        }

        [Fact]
        public async Task AddHttpClient_MessageHandler_Scope_SingletonDependency()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<SingletonService>();
            serviceCollection.AddTransient<HandlerWithSingletonService>();
            serviceCollection
                .AddHttpClient<TypedClientWithSingletonService>("test")
                .AddHttpMessageHandler<HandlerWithSingletonService>();

            var services = serviceCollection.BuildServiceProvider(validateScopes: true);

            using (var scope = services.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                // Act
                var client = scope.ServiceProvider.GetRequiredService<TypedClientWithSingletonService>();

                // Assert
                var request = new HttpRequestMessage(HttpMethod.Get, "http://example.com/");
                var response = await client.HttpClient.SendAsync(request);

                Assert.Same(
                    services.GetRequiredService<SingletonService>(),
                    request.Properties[nameof(SingletonService)]);

                Assert.Same(
                    scope.ServiceProvider.GetRequiredService<SingletonService>(),
                    request.Properties[nameof(SingletonService)]);

                Assert.Same(
                    client.Service,
                    request.Properties[nameof(SingletonService)]);
            }
        }

        [Fact]
        public void AddHttpClient_MessageHandler_ScopedDependency()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddScoped<ScopedService>();
            serviceCollection.AddTransient<HandlerWithScopedService>();
            serviceCollection
                .AddHttpClient<TypedClientWithScopedService>("test")
                .AddHttpMessageHandler<HandlerWithScopedService>();

            var services = serviceCollection.BuildServiceProvider(validateScopes: true);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
            {
                services.GetRequiredService<TypedClientWithScopedService>();
            });
        }

        [Fact]
        public async Task AddHttpClient_MessageHandler_Scope_ScopedDependency()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddScoped<ScopedService>();
            serviceCollection.AddScoped<HandlerWithScopedService>();
            serviceCollection
                .AddHttpClient<TypedClientWithScopedService>("test")
                .AddHttpMessageHandler<HandlerWithScopedService>();

            var services = serviceCollection.BuildServiceProvider(validateScopes: true);

            using (var scope = services.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                // Act
                var client = scope.ServiceProvider.GetRequiredService<TypedClientWithScopedService>();

                // Assert
                var request = new HttpRequestMessage(HttpMethod.Get, "http://example.com/");
                var response = await client.HttpClient.SendAsync(request);

                Assert.NotSame(
                    scope.ServiceProvider.GetRequiredService<ScopedService>(),
                    request.Properties[nameof(ScopedService)]);

                Assert.Same(
                    scope.ServiceProvider.GetRequiredService<ScopedService>(),
                    client.Service);

                Assert.NotSame(
                    client.Service,
                    request.Properties[nameof(ScopedService)]);
            }
        }

        [Fact]
        public async Task AddHttpClient_MessageHandler_TransientDependency()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddTransient<TransientService>();
            serviceCollection.AddTransient<HandlerWithTransientService>();
            serviceCollection
                .AddHttpClient<TypedClientWithTransientService>("test")
                .AddHttpMessageHandler<HandlerWithTransientService>();

            var services = serviceCollection.BuildServiceProvider(validateScopes: true);

            // Act
            var client = services.GetRequiredService<TypedClientWithTransientService>();

            // Assert
            var request = new HttpRequestMessage(HttpMethod.Get, "http://example.com/");
            var response = await client.HttpClient.SendAsync(request);

            Assert.NotSame(
                services.GetRequiredService<TransientService>(),
                request.Properties[nameof(TransientService)]);

            Assert.NotSame(
                client.Service,
                request.Properties[nameof(TransientService)]);
        }

        [Fact]
        public async Task AddHttpClient_MessageHandler_Scope_TransientDependency()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddTransient<TransientService>();
            serviceCollection.AddTransient<HandlerWithTransientService>();
            serviceCollection
                .AddHttpClient<TypedClientWithTransientService>("test")
                .AddHttpMessageHandler<HandlerWithTransientService>();

            var services = serviceCollection.BuildServiceProvider(validateScopes: true);

            using (var scope = services.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                // Act
                var client = scope.ServiceProvider.GetRequiredService<TypedClientWithTransientService>();

                // Assert
                var request = new HttpRequestMessage(HttpMethod.Get, "http://example.com/");
                var response = await client.HttpClient.SendAsync(request);

                Assert.NotSame(
                    services.GetRequiredService<TransientService>(),
                    request.Properties[nameof(TransientService)]);

                Assert.NotSame(
                    client.Service,
                    request.Properties[nameof(TransientService)]);
            }
        }

        [Fact]
        public void AddHttpClient_GetAwaiterAndResult_InSingleThreadedSynchronizationContext_ShouldNotHangs()
        {
            // Arrange
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
            {
                var token = cts.Token;
                token.Register(() => throw new OperationCanceledException(token));
                var serviceCollection = new ServiceCollection();
                serviceCollection.AddHttpClient("example.com")
                    .ConfigurePrimaryHttpMessageHandler(() =>
                    {
                        var mockHandler = new Mock<HttpMessageHandler>();
                        mockHandler
                        .Protected()
                        .Setup<Task<HttpResponseMessage>>(
                            "SendAsync",
                            ItExpr.IsAny<HttpRequestMessage>(),
                            ItExpr.IsAny<CancellationToken>()
                        )
                        .Returns(async () =>
                        {
                            await Task.Delay(1).ConfigureAwait(false);
                            return new HttpResponseMessage(HttpStatusCode.OK);
                        });
                        return mockHandler.Object;
                    });

                var services = serviceCollection.BuildServiceProvider();
                var factory = services.GetRequiredService<IHttpClientFactory>();
                var client = factory.CreateClient("example.com");
                var hangs = true;
                SingleThreadedSynchronizationContext.Run(() =>
                {
                    // Act
                    client.GetAsync("http://example.com", token).GetAwaiter().GetResult();
                    hangs = false;
                });

                // Assert
                Assert.False(hangs);
            }
        }

        [Fact]
        public void SuppressScope_False_CreatesNewScope()
        {
            // Arrange
            IServiceProvider capturedServices = null;

            var serviceCollection = new ServiceCollection();
            serviceCollection
                .AddHttpClient<TestTypedClient>("test")
                .AddHttpMessageHandler(s =>
                {
                    capturedServices = s;
                    return Mock.Of<DelegatingHandler>();
                });

            serviceCollection.Configure<HttpClientFactoryOptions>("test", o =>
            {
                o.SuppressHandlerScope = false;
            });

            var services = serviceCollection.BuildServiceProvider(validateScopes: true);

            // Act
            services.GetRequiredService<TestTypedClient>();

            Assert.NotSame(services, capturedServices);
        }

        [Fact]
        public void SuppressScope_False_InScope_CreatesNewScope()
        {
            // Arrange
            IServiceProvider capturedServices = null;

            var serviceCollection = new ServiceCollection();
            serviceCollection
                .AddHttpClient<TestTypedClient>("test")
                .AddHttpMessageHandler(s =>
                {
                    capturedServices = s;
                    return Mock.Of<DelegatingHandler>();
                });

            serviceCollection.Configure<HttpClientFactoryOptions>("test", o =>
            {
                o.SuppressHandlerScope = false;
            });

            var services = serviceCollection.BuildServiceProvider(validateScopes: true);

            using (var scope = services.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                // Act
                scope.ServiceProvider.GetRequiredService<TestTypedClient>();

                Assert.NotSame(services, capturedServices);
                Assert.NotSame(scope.ServiceProvider, capturedServices);
            }
        }

        [Fact]
        public void SuppressScope_True_DoesNotCreateScope()
        {
            // Arrange
            IServiceProvider capturedServices = null;

            var serviceCollection = new ServiceCollection();
            serviceCollection
                .AddHttpClient<TestTypedClient>("test")
                .AddHttpMessageHandler(s =>
                {
                    capturedServices = s;
                    return Mock.Of<DelegatingHandler>();
                });

            serviceCollection.Configure<HttpClientFactoryOptions>("test", o =>
            {
                o.SuppressHandlerScope = true;
            });

            var services = serviceCollection.BuildServiceProvider(validateScopes: true);

            // Act
            services.GetRequiredService<TestTypedClient>();

            Assert.NotSame(services, capturedServices);
        }

        [Fact]
        public void SuppressScope_True_InScope_DoesNotCreateScope()
        {
            // Arrange
            IServiceProvider capturedServices = null;

            var serviceCollection = new ServiceCollection();
            serviceCollection
                .AddHttpClient<TestTypedClient>("test")
                .AddHttpMessageHandler(s =>
                {
                    capturedServices = s;
                    return Mock.Of<DelegatingHandler>();
                });

            serviceCollection.Configure<HttpClientFactoryOptions>("test", o =>
            {
                o.SuppressHandlerScope = true;
            });

            var services = serviceCollection.BuildServiceProvider(validateScopes: true);

            using (var scope = services.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                // Act
                scope.ServiceProvider.GetRequiredService<TestTypedClient>();

                Assert.NotSame(scope.ServiceProvider, capturedServices);
            }
        }

        private class TestGenericTypedClient<T> : TestTypedClient
        {
            public TestGenericTypedClient(HttpClient httpClient)
                : base(httpClient)
            {
            }
        }

        private class SingletonService
        {
        }

        private class ScopedService
        {
        }

        private class TransientService
        {
        }

        private class HandlerWithSingletonService : DelegatingHandler
        {
            public HandlerWithSingletonService(SingletonService service)
            {
                Service = service;
            }

            public SingletonService Service { get; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                request.Properties[nameof(SingletonService)] = Service;
                return Task.FromResult(new HttpResponseMessage());
            }
        }

        private class HandlerWithScopedService : DelegatingHandler
        {
            public HandlerWithScopedService(ScopedService service)
            {
                Service = service;
            }

            public ScopedService Service { get; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                request.Properties[nameof(ScopedService)] = Service;
                return Task.FromResult(new HttpResponseMessage());
            }
        }

        private class HandlerWithTransientService : DelegatingHandler
        {
            public HandlerWithTransientService(TransientService service)
            {
                Service = service;
            }

            public TransientService Service { get; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                request.Properties[nameof(TransientService)] = Service;
                return Task.FromResult(new HttpResponseMessage());
            }
        }

        private class TypedClientWithSingletonService
        {
            public TypedClientWithSingletonService(HttpClient httpClient, SingletonService service)
            {
                HttpClient = httpClient;
                Service = service;
            }

            public HttpClient HttpClient { get; }

            public SingletonService Service { get; }
        }

        private class TypedClientWithScopedService
        {
            public TypedClientWithScopedService(HttpClient httpClient, ScopedService service)
            {
                HttpClient = httpClient;
                Service = service;
            }

            public HttpClient HttpClient { get; }

            public ScopedService Service { get; }
        }

        private class TypedClientWithTransientService
        {
            public TypedClientWithTransientService(HttpClient httpClient, TransientService service)
            {
                HttpClient = httpClient;
                Service = service;
            }

            public HttpClient HttpClient { get; }

            public TransientService Service { get; }
        }
    }
}
