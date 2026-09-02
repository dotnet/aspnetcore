// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Hosting;

public class IHostInitializerTest
{
    [Fact]
    public async Task DefaultsAndCancellationTokenArePassedThrough()
    {
        var initializer = new TestHostInitializer();
        var services = new TestServiceProvider();
        using var cancellationTokenSource = new CancellationTokenSource();

        await initializer.InitializeAsync(services, cancellationTokenSource.Token);

        Assert.Equal(0, ((IHostInitializer)initializer).Order);
        Assert.False(((IHostInitializer)initializer).RequiresJSInterop);
        Assert.Same(services, initializer.Services);
        Assert.Equal(cancellationTokenSource.Token, initializer.CancellationToken);
    }

    private sealed class TestHostInitializer : IHostInitializer
    {
        public IServiceProvider Services { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

        public Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
        {
            Services = services;
            CancellationToken = cancellationToken;
            return Task.CompletedTask;
        }

    }

    private sealed class TestServiceProvider : IServiceProvider
    {
        public object GetService(Type serviceType)
            => null;
    }
}
