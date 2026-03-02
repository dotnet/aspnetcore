// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Components.WebAssembly.Hosting;

public class HostedServiceExecutorTest
{
    [Fact]
    public async Task StopAsync_WithFailingServices_ThrowsAggregateExceptionWithDescriptiveMessage()
    {
        // Arrange
        var services = new[]
        {
            new FaultyHostedService("Service 1 error"),
            new FaultyHostedService("Service 2 error"),
        };
        var executor = new HostedServiceExecutor(services, NullLogger<HostedServiceExecutor>.Instance);

        // Act
        var ex = await Assert.ThrowsAsync<AggregateException>(() => executor.StopAsync(CancellationToken.None));

        // Assert
        Assert.StartsWith("One or more hosted services failed to stop.", ex.Message);
        Assert.Equal(2, ex.InnerExceptions.Count);
    }

    [Fact]
    public async Task StopAsync_WithNoFailingServices_DoesNotThrow()
    {
        // Arrange
        var services = new[]
        {
            new SuccessfulHostedService(),
            new SuccessfulHostedService(),
        };
        var executor = new HostedServiceExecutor(services, NullLogger<HostedServiceExecutor>.Instance);

        // Act (should not throw)
        await executor.StopAsync(CancellationToken.None);
    }

    private class FaultyHostedService(string errorMessage) : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken) =>
            Task.FromException(new InvalidOperationException(errorMessage));
    }

    private class SuccessfulHostedService : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
