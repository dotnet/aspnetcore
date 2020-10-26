// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Hosting
{
    public partial class WebHostTests
    {
        [Fact]
        public async Task DisposingHostCallsDisposeAsyncOnProvider()
        {
            var providerFactory = new AsyncServiceProviderFactory();
            using (var host = CreateBuilder()
                .UseFakeServer()
                .ConfigureServices((context, services) =>
                    services.Add(ServiceDescriptor.Singleton<IServiceProviderFactory<IServiceCollection>>(providerFactory)
                    ))
                .UseStartup("Microsoft.AspNetCore.Hosting.Tests")
                .Build())
            {
                await host.StartAsync();

                Assert.Equal(2, providerFactory.Providers.Count);

                await host.StopAsync();

                Assert.All(providerFactory.Providers, provider => {
                    Assert.False(provider.DisposeCalled);
                    Assert.False(provider.DisposeAsyncCalled);
                });

                host.Dispose();

                Assert.All(providerFactory.Providers, provider => {
                    Assert.False(provider.DisposeCalled);
                    Assert.True(provider.DisposeAsyncCalled);
                });
            }
        }

        private class AsyncServiceProviderFactory : IServiceProviderFactory<IServiceCollection>
        {
            public List<AsyncDisposableServiceProvider> Providers { get; } = new List<AsyncDisposableServiceProvider>();

            public IServiceCollection CreateBuilder(IServiceCollection services)
            {
                return services;
            }

            public IServiceProvider CreateServiceProvider(IServiceCollection containerBuilder)
            {
                var provider = new AsyncDisposableServiceProvider(containerBuilder.BuildServiceProvider());
                Providers.Add(provider);
                return provider;
            }
        }

        private class AsyncDisposableServiceProvider : IServiceProvider, IDisposable, IAsyncDisposable
        {
            private readonly ServiceProvider _serviceProvider;

            public AsyncDisposableServiceProvider(ServiceProvider serviceProvider)
            {
                _serviceProvider = serviceProvider;
            }

            public bool DisposeCalled { get; set; }

            public bool DisposeAsyncCalled { get; set; }

            public object GetService(Type serviceType) => _serviceProvider.GetService(serviceType);

            public void Dispose()
            {
                DisposeCalled = true;
                _serviceProvider.Dispose();
            }

            public ValueTask DisposeAsync()
            {
                DisposeAsyncCalled = true;
                _serviceProvider.Dispose();
                return default;
            }
        }
    }

}
