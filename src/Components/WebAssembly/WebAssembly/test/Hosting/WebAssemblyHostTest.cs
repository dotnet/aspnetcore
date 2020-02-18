// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Components.WebAssembly.Hosting
{
    public class WebAssemblyHostTest
    {
        // This won't happen in the product code, but we need to be able to safely call RunAsync
        // to be able to test a few of the other details.
        [Fact]
        public async Task RunAsync_CanExitBasedOnCancellationToken()
        {
            // Arrange
            var builder = WebAssemblyHostBuilder.CreateDefault();
            var host = builder.Build();

            var cts = new CancellationTokenSource();

            // Act
            var task = host.RunAsyncCore(cts.Token);

            cts.Cancel();
            await task.TimeoutAfter(TimeSpan.FromSeconds(3));

            // Assert (does not throw)
        }

        [Fact]
        public async Task RunAsync_CallingTwiceCausesException()
        {
            // Arrange
            var builder = WebAssemblyHostBuilder.CreateDefault();
            var host = builder.Build();

            var cts = new CancellationTokenSource();
            var task = host.RunAsyncCore(cts.Token);

            // Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => host.RunAsyncCore(cts.Token));

            cts.Cancel();
            await task.TimeoutAfter(TimeSpan.FromSeconds(3));

            // Assert
            Assert.Equal("The host has already started.", ex.Message);
        }

        [Fact]
        public async Task DisposeAsync_CanDisposeAfterCallingRunAsync()
        {
            // Arrange
            var builder = WebAssemblyHostBuilder.CreateDefault();
            builder.Services.AddSingleton<DisposableService>();
            var host = builder.Build();

            var disposable = host.Services.GetRequiredService<DisposableService>();

            var cts = new CancellationTokenSource();

            // Act
            await using (host)
            {
                var task = host.RunAsyncCore(cts.Token);

                cts.Cancel();
                await task.TimeoutAfter(TimeSpan.FromSeconds(3));
            }

            // Assert
            Assert.Equal(1, disposable.DisposeCount);
        }

        private class DisposableService : IAsyncDisposable
        {
            public int DisposeCount { get; private set; }

            public ValueTask DisposeAsync()
            {
                DisposeCount++;
                return new ValueTask(Task.CompletedTask);
            }
        }
    }
}
