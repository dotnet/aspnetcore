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
        // Add single filter by type
        // Add single filter by instance
        // Add multiple filters and check order
        // Add multiple filters type + instance
        // Add filter by type and AddSingleton of that type
        // Per Hub filters

        public class F : IHubFilter
        {
            public Task OnConnectedAsync(HubCallerContext context, Func<HubCallerContext, Task> next)
            {
                return next(context);
            }

            public ValueTask<object> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object>> next)
            {
                throw new NotImplementedException();
            }

            public Task OnDisconnectedAsync(HubCallerContext context, Func<HubCallerContext, Task> next)
            {
                return next(context);
            }
        }

        [Fact]
        public async Task GlobalHubFilterIsCalled()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
                {
                    services.AddSignalR(options =>
                    {
                        options.AddFilter<F>();
                    });
                }, LoggerFactory);

                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

                using (var client = new TestClient())
                {
                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                    await client.Connected.OrTimeout();

                    var message = await client.InvokeAsync(nameof(MethodHub.Echo), "Hello world!").OrTimeout();

                    Assert.Null(message.Error);

                    client.Dispose();

                    await connectionHandlerTask.OrTimeout();
                }
            }
        }
    }
}
