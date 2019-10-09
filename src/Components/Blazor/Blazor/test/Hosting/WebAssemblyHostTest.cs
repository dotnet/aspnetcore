// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Builder;
using Microsoft.AspNetCore.Components.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Mono.WebAssembly.Interop;
using Xunit;

namespace Microsoft.AspNetCore.Blazor.Hosting.Test
{
    public class WebAssemblyHostTest
    {
        [Fact]
        public async Task BrowserHost_StartAsync_ThrowsWithoutStartup()
        {
            // Arrange
            var builder = new WebAssemblyHostBuilder();
            var host = builder.Build();

            // Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await host.StartAsync());

            // Assert
            Assert.Equal(
                "Could not find a registered Blazor Startup class. " +
                "Using IWebAssemblyHost requires a call to IWebAssemblyHostBuilder.UseBlazorStartup.",
                ex.Message);
        }

        [Fact]
        public async Task BrowserHost_StartAsync_RunsConfigureMethod()
        {
            // Arrange
            var builder = new WebAssemblyHostBuilder();

            var startup = new MockStartup();
            builder.ConfigureServices((c, s) => { s.AddSingleton<IBlazorStartup>(startup); });

            var host = builder.Build();

            // Act
            await host.StartAsync();

            // Assert
            Assert.True(startup.ConfigureCalled);
        }

        private class MockStartup : IBlazorStartup
        {
            public bool ConfigureCalled { get; set; }

            public void Configure(IComponentsApplicationBuilder app, IServiceProvider services)
            {
                ConfigureCalled = true;
            }

            public void ConfigureServices(IServiceCollection services)
            {
            }
        }
    }
}
