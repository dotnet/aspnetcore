// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    public class HubFilterTests : VerifiableLoggedTest
    {
        // Add multiple filters and check order
        // Add multiple filters type + instance
        // Add filter by type and AddSingleton of that type
        // Per Hub filters

        public class VerifyMethodFilter : IHubFilter
        {
            private readonly TcsService _service;
            public VerifyMethodFilter(TcsService tcsService)
            {
                _service = tcsService;
            }

            public async Task OnConnectedAsync(HubCallerContext context, Func<HubCallerContext, Task> next)
            {
                _service.StartedMethod.TrySetResult(null);
                await next(context);
                _service.EndMethod.TrySetResult(null);
            }

            public async ValueTask<object> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object>> next)
            {
                _service.StartedMethod.TrySetResult(null);
                var result = await next(invocationContext);
                _service.EndMethod.TrySetResult(null);

                return result;
            }

            public async Task OnDisconnectedAsync(HubCallerContext context, Func<HubCallerContext, Task> next)
            {
                _service.StartedMethod.TrySetResult(null);
                await next(context);
                _service.EndMethod.TrySetResult(null);
            }
        }

        [Fact]
        public async Task GlobalHubFilterByType_MethodsAreCalled()
        {
            using (StartVerifiableLog())
            {
                var tcsService = new TcsService();
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
                {
                    services.AddSignalR(options =>
                    {
                        options.AddFilter<VerifyMethodFilter>();
                    });

                    services.AddSingleton(tcsService);
                }, LoggerFactory);

                await AssertMethodsCalled(serviceProvider, tcsService);
            }
        }

        [Fact]
        public async Task GlobalHubFilterByInstance_MethodsAreCalled()
        {
            using (StartVerifiableLog())
            {
                var tcsService = new TcsService();
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
                {
                    services.AddSignalR(options =>
                    {
                        options.AddFilter(new VerifyMethodFilter(tcsService));
                    });
                }, LoggerFactory);

                await AssertMethodsCalled(serviceProvider, tcsService);
            }
        }

        [Fact]
        public async Task PerHubFilterByInstance_MethodsAreCalled()
        {
            using (StartVerifiableLog())
            {
                var tcsService = new TcsService();
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
                {
                    services.AddSignalR().AddHubOptions<MethodHub>(options =>
                    {
                        options.AddFilter(new VerifyMethodFilter(tcsService));
                    });
                }, LoggerFactory);

                await AssertMethodsCalled(serviceProvider, tcsService);
            }
        }

        [Fact]
        public async Task PerHubFilterByType_MethodsAreCalled()
        {
            using (StartVerifiableLog())
            {
                var tcsService = new TcsService();
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
                {
                    services.AddSignalR().AddHubOptions<MethodHub>(options =>
                    {
                        options.AddFilter<VerifyMethodFilter>();
                    });

                    services.AddSingleton(tcsService);
                }, LoggerFactory);

                await AssertMethodsCalled(serviceProvider, tcsService);
            }
        }

        private async Task AssertMethodsCalled(IServiceProvider serviceProvider, TcsService tcsService)
        {
            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                await tcsService.StartedMethod.Task.OrTimeout();
                await client.Connected.OrTimeout();
                await tcsService.EndMethod.Task.OrTimeout();

                tcsService.Reset();
                var message = await client.InvokeAsync(nameof(MethodHub.Echo), "Hello world!").OrTimeout();
                await tcsService.EndMethod.Task.OrTimeout();
                tcsService.Reset();

                Assert.Null(message.Error);

                client.Dispose();

                await connectionHandlerTask.OrTimeout();

                await tcsService.EndMethod.Task.OrTimeout();
            }
        }
    }
}
