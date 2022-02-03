// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Hosting;

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

            Assert.All(providerFactory.Providers, provider =>
            {
                Assert.False(provider.DisposeCalled);
                Assert.False(provider.DisposeAsyncCalled);
            });

            host.Dispose();

            Assert.All(providerFactory.Providers, provider =>
            {
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
