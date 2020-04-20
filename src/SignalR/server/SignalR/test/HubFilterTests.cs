// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    // Tests:
    // Not running OnConnected, Invoke, and OnDisconnected
    //

    public class HubFilterTests : VerifiableLoggedTest
    {
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

        [Fact]
        public async Task MutlipleFilters_MethodsAreCalled()
        {
            using (StartVerifiableLog())
            {
                var tcsService1 = new TcsService();
                var tcsService2 = new TcsService();
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
                {
                    services.AddSignalR(options =>
                    {
                        options.AddFilter(new VerifyMethodFilter(tcsService1));
                        options.AddFilter(new VerifyMethodFilter(tcsService2));
                    });
                }, LoggerFactory);

                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

                using (var client = new TestClient())
                {
                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                    await tcsService1.StartedMethod.Task.OrTimeout();
                    await tcsService2.StartedMethod.Task.OrTimeout();
                    await client.Connected.OrTimeout();
                    await tcsService1.EndMethod.Task.OrTimeout();
                    await tcsService2.EndMethod.Task.OrTimeout();

                    tcsService1.Reset();
                    tcsService2.Reset();
                    var message = await client.InvokeAsync(nameof(MethodHub.Echo), "Hello world!").OrTimeout();
                    await tcsService1.EndMethod.Task.OrTimeout();
                    await tcsService2.EndMethod.Task.OrTimeout();
                    tcsService1.Reset();
                    tcsService2.Reset();

                    Assert.Null(message.Error);

                    client.Dispose();

                    await connectionHandlerTask.OrTimeout();

                    await tcsService1.EndMethod.Task.OrTimeout();
                    await tcsService2.EndMethod.Task.OrTimeout();
                }
            }
        }

        [Fact]
        public async Task MixingTypeAndInstanceGlobalFilters_MethodsAreCalled()
        {
            using (StartVerifiableLog())
            {
                var tcsService1 = new TcsService();
                var tcsService2 = new TcsService();
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
                {
                    services.AddSignalR(options =>
                    {
                        options.AddFilter(new VerifyMethodFilter(tcsService1));
                        options.AddFilter<VerifyMethodFilter>();
                    });

                    services.AddSingleton(tcsService2);
                }, LoggerFactory);

                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

                using (var client = new TestClient())
                {
                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                    await tcsService1.StartedMethod.Task.OrTimeout();
                    await tcsService2.StartedMethod.Task.OrTimeout();
                    await client.Connected.OrTimeout();
                    await tcsService1.EndMethod.Task.OrTimeout();
                    await tcsService2.EndMethod.Task.OrTimeout();

                    tcsService1.Reset();
                    tcsService2.Reset();
                    var message = await client.InvokeAsync(nameof(MethodHub.Echo), "Hello world!").OrTimeout();
                    await tcsService1.EndMethod.Task.OrTimeout();
                    await tcsService2.EndMethod.Task.OrTimeout();
                    tcsService1.Reset();
                    tcsService2.Reset();

                    Assert.Null(message.Error);

                    client.Dispose();

                    await connectionHandlerTask.OrTimeout();

                    await tcsService1.EndMethod.Task.OrTimeout();
                    await tcsService2.EndMethod.Task.OrTimeout();
                }
            }
        }

        [Fact]
        public async Task MixingTypeAndInstanceHubSpecificFilters_MethodsAreCalled()
        {
            using (StartVerifiableLog())
            {
                var tcsService1 = new TcsService();
                var tcsService2 = new TcsService();
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
                {
                    services.AddSignalR()
                    .AddHubOptions<MethodHub>(options =>
                    {
                        options.AddFilter(new VerifyMethodFilter(tcsService1));
                        options.AddFilter<VerifyMethodFilter>();
                    });

                    services.AddSingleton(tcsService2);
                }, LoggerFactory);

                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

                using (var client = new TestClient())
                {
                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                    await tcsService1.StartedMethod.Task.OrTimeout();
                    await tcsService2.StartedMethod.Task.OrTimeout();
                    await client.Connected.OrTimeout();
                    await tcsService1.EndMethod.Task.OrTimeout();
                    await tcsService2.EndMethod.Task.OrTimeout();

                    tcsService1.Reset();
                    tcsService2.Reset();
                    var message = await client.InvokeAsync(nameof(MethodHub.Echo), "Hello world!").OrTimeout();
                    await tcsService1.EndMethod.Task.OrTimeout();
                    await tcsService2.EndMethod.Task.OrTimeout();
                    tcsService1.Reset();
                    tcsService2.Reset();

                    Assert.Null(message.Error);

                    client.Dispose();

                    await connectionHandlerTask.OrTimeout();

                    await tcsService1.EndMethod.Task.OrTimeout();
                    await tcsService2.EndMethod.Task.OrTimeout();
                }
            }
        }

        public class SyncPointFilter : IHubFilter
        {
            private readonly SyncPoint[] _syncPoint;
            public SyncPointFilter(SyncPoint[] syncPoints)
            {
                Debug.Assert(syncPoints.Length == 3);
                _syncPoint = syncPoints;
            }

            public async Task OnConnectedAsync(HubCallerContext context, Func<HubCallerContext, Task> next)
            {
                await _syncPoint[0].WaitToContinue();
                await next(context);
            }

            public async ValueTask<object> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object>> next)
            {
                await _syncPoint[1].WaitToContinue();
                var result = await next(invocationContext);

                return result;
            }

            public async Task OnDisconnectedAsync(HubCallerContext context, Func<HubCallerContext, Task> next)
            {
                await _syncPoint[2].WaitToContinue();
                await next(context);
            }
        }

        [Fact]
        public async Task GlobalFiltersRunInOrder()
        {
            using (StartVerifiableLog())
            {
                var syncPoint1 = SyncPoint.Create(3, out var syncPoints1);
                var syncPoint2 = SyncPoint.Create(3, out var syncPoints2);
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
                {
                    services.AddSignalR(options =>
                    {
                        options.AddFilter(new SyncPointFilter(syncPoints1));
                        options.AddFilter(new SyncPointFilter(syncPoints2));
                    });
                }, LoggerFactory);

                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

                using (var client = new TestClient())
                {
                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                    await syncPoints1[0].WaitForSyncPoint().OrTimeout();
                    // Second filter wont run yet because first filter is waiting on SyncPoint
                    Assert.False(syncPoints2[0].WaitForSyncPoint().IsCompleted);
                    syncPoints1[0].Continue();

                    await syncPoints2[0].WaitForSyncPoint().OrTimeout();
                    syncPoints2[0].Continue();
                    await client.Connected.OrTimeout();

                    var invokeTask = client.InvokeAsync(nameof(MethodHub.Echo), "Hello world!");

                    await syncPoints1[1].WaitForSyncPoint().OrTimeout();
                    // Second filter wont run yet because first filter is waiting on SyncPoint
                    Assert.False(syncPoints2[1].WaitForSyncPoint().IsCompleted);
                    syncPoints1[1].Continue();

                    await syncPoints2[1].WaitForSyncPoint().OrTimeout();
                    syncPoints2[1].Continue();
                    var message = await invokeTask.OrTimeout();

                    Assert.Null(message.Error);

                    client.Dispose();

                    await syncPoints1[2].WaitForSyncPoint().OrTimeout();
                    // Second filter wont run yet because first filter is waiting on SyncPoint
                    Assert.False(syncPoints2[2].WaitForSyncPoint().IsCompleted);
                    syncPoints1[2].Continue();

                    await syncPoints2[2].WaitForSyncPoint().OrTimeout();
                    syncPoints2[2].Continue();

                    await connectionHandlerTask.OrTimeout();
                }
            }
        }

        [Fact]
        public async Task HubSpecificFiltersRunInOrder()
        {
            using (StartVerifiableLog())
            {
                var syncPoint1 = SyncPoint.Create(3, out var syncPoints1);
                var syncPoint2 = SyncPoint.Create(3, out var syncPoints2);
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
                {
                    services.AddSignalR()
                    .AddHubOptions<MethodHub>(options =>
                    {
                        options.AddFilter(new SyncPointFilter(syncPoints1));
                        options.AddFilter(new SyncPointFilter(syncPoints2));
                    });
                }, LoggerFactory);

                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

                using (var client = new TestClient())
                {
                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                    await syncPoints1[0].WaitForSyncPoint().OrTimeout();
                    // Second filter wont run yet because first filter is waiting on SyncPoint
                    Assert.False(syncPoints2[0].WaitForSyncPoint().IsCompleted);
                    syncPoints1[0].Continue();

                    await syncPoints2[0].WaitForSyncPoint().OrTimeout();
                    syncPoints2[0].Continue();
                    await client.Connected.OrTimeout();

                    var invokeTask = client.InvokeAsync(nameof(MethodHub.Echo), "Hello world!");

                    await syncPoints1[1].WaitForSyncPoint().OrTimeout();
                    // Second filter wont run yet because first filter is waiting on SyncPoint
                    Assert.False(syncPoints2[1].WaitForSyncPoint().IsCompleted);
                    syncPoints1[1].Continue();

                    await syncPoints2[1].WaitForSyncPoint().OrTimeout();
                    syncPoints2[1].Continue();
                    var message = await invokeTask.OrTimeout();

                    Assert.Null(message.Error);

                    client.Dispose();

                    await syncPoints1[2].WaitForSyncPoint().OrTimeout();
                    // Second filter wont run yet because first filter is waiting on SyncPoint
                    Assert.False(syncPoints2[2].WaitForSyncPoint().IsCompleted);
                    syncPoints1[2].Continue();

                    await syncPoints2[2].WaitForSyncPoint().OrTimeout();
                    syncPoints2[2].Continue();

                    await connectionHandlerTask.OrTimeout();
                }
            }
        }

        [Fact]
        public async Task GlobalFiltersRunBeforeHubSpecificFilters()
        {
            using (StartVerifiableLog())
            {
                var syncPoint1 = SyncPoint.Create(3, out var syncPoints1);
                var syncPoint2 = SyncPoint.Create(3, out var syncPoints2);
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
                {
                    services.AddSignalR(options =>
                    {
                        options.AddFilter(new SyncPointFilter(syncPoints1));
                    })
                    .AddHubOptions<MethodHub>(options =>
                    {
                        options.AddFilter(new SyncPointFilter(syncPoints2));
                    });
                }, LoggerFactory);

                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

                using (var client = new TestClient())
                {
                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                    await syncPoints1[0].WaitForSyncPoint().OrTimeout();
                    // Second filter wont run yet because first filter is waiting on SyncPoint
                    Assert.False(syncPoints2[0].WaitForSyncPoint().IsCompleted);
                    syncPoints1[0].Continue();

                    await syncPoints2[0].WaitForSyncPoint().OrTimeout();
                    syncPoints2[0].Continue();
                    await client.Connected.OrTimeout();

                    var invokeTask = client.InvokeAsync(nameof(MethodHub.Echo), "Hello world!");

                    await syncPoints1[1].WaitForSyncPoint().OrTimeout();
                    // Second filter wont run yet because first filter is waiting on SyncPoint
                    Assert.False(syncPoints2[1].WaitForSyncPoint().IsCompleted);
                    syncPoints1[1].Continue();

                    await syncPoints2[1].WaitForSyncPoint().OrTimeout();
                    syncPoints2[1].Continue();
                    var message = await invokeTask.OrTimeout();

                    Assert.Null(message.Error);

                    client.Dispose();

                    await syncPoints1[2].WaitForSyncPoint().OrTimeout();
                    // Second filter wont run yet because first filter is waiting on SyncPoint
                    Assert.False(syncPoints2[2].WaitForSyncPoint().IsCompleted);
                    syncPoints1[2].Continue();

                    await syncPoints2[2].WaitForSyncPoint().OrTimeout();
                    syncPoints2[2].Continue();

                    await connectionHandlerTask.OrTimeout();
                }
            }
        }

        [Fact]
        public async Task FilterCanBeResolvedFromDI()
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

                    // If this instance wasn't resolved, then the tcsService.StartedMethod waits would never trigger and fail the test
                    services.AddSingleton(new VerifyMethodFilter(tcsService));
                }, LoggerFactory);

                await AssertMethodsCalled(serviceProvider, tcsService);
            }
        }

        public class FilterCounter
        {
            public int OnConnectedAsyncCount;
            public int InvokeMethodAsyncCount;
            public int OnDisconnectedAsyncCount;
        }

        public class CounterFilter : IHubFilter
        {
            private readonly FilterCounter _counter;
            public CounterFilter(FilterCounter counter)
            {
                _counter = counter;
                _counter.OnConnectedAsyncCount= 0;
                _counter.InvokeMethodAsyncCount = 0;
                _counter.OnDisconnectedAsyncCount = 0;
            }

            public Task OnConnectedAsync(HubCallerContext context, Func<HubCallerContext, Task> next)
            {
                _counter.OnConnectedAsyncCount++;
                return next(context);
            }

            public Task OnDisconnectedAsync(HubCallerContext context, Func<HubCallerContext, Task> next)
            {
                _counter.OnDisconnectedAsyncCount++;
                return next(context);
            }

            public ValueTask<object> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object>> next)
            {
                _counter.InvokeMethodAsyncCount++;
                return next(invocationContext);
            }
        }

        [Fact]
        public async Task FiltersHaveTransientScopeByDefault()
        {
            using (StartVerifiableLog())
            {
                var counter = new FilterCounter();
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
                {
                    services.AddSignalR(options =>
                    {
                        options.AddFilter<CounterFilter>();
                    });

                    services.AddSingleton(counter);
                }, LoggerFactory);

                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

                using (var client = new TestClient())
                {
                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                    await client.Connected.OrTimeout();
                    // Filter is transient, so these counts are reset every time the filter is created
                    Assert.Equal(1, counter.OnConnectedAsyncCount);
                    Assert.Equal(0, counter.InvokeMethodAsyncCount);
                    Assert.Equal(0, counter.OnDisconnectedAsyncCount);

                    var message = await client.InvokeAsync(nameof(MethodHub.Echo), "Hello world!").OrTimeout();
                    // Filter is transient, so these counts are reset every time the filter is created
                    Assert.Equal(0, counter.OnConnectedAsyncCount);
                    Assert.Equal(1, counter.InvokeMethodAsyncCount);
                    Assert.Equal(0, counter.OnDisconnectedAsyncCount);

                    Assert.Null(message.Error);

                    client.Dispose();

                    await connectionHandlerTask.OrTimeout();

                    // Filter is transient, so these counts are reset every time the filter is created
                    Assert.Equal(0, counter.OnConnectedAsyncCount);
                    Assert.Equal(0, counter.InvokeMethodAsyncCount);
                    Assert.Equal(1, counter.OnDisconnectedAsyncCount);
                }
            }
        }

        [Fact]
        public async Task FiltersCanBeSingletonIfAddedToDI()
        {
            using (StartVerifiableLog())
            {
                var counter = new FilterCounter();
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
                {
                    services.AddSignalR(options =>
                    {
                        options.AddFilter<CounterFilter>();
                    });

                    services.AddSingleton<CounterFilter>();
                    services.AddSingleton(counter);
                }, LoggerFactory);

                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

                using (var client = new TestClient())
                {
                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                    await client.Connected.OrTimeout();
                    Assert.Equal(1, counter.OnConnectedAsyncCount);
                    Assert.Equal(0, counter.InvokeMethodAsyncCount);
                    Assert.Equal(0, counter.OnDisconnectedAsyncCount);

                    var message = await client.InvokeAsync(nameof(MethodHub.Echo), "Hello world!").OrTimeout();
                    Assert.Equal(1, counter.OnConnectedAsyncCount);
                    Assert.Equal(1, counter.InvokeMethodAsyncCount);
                    Assert.Equal(0, counter.OnDisconnectedAsyncCount);

                    Assert.Null(message.Error);

                    client.Dispose();

                    await connectionHandlerTask.OrTimeout();

                    Assert.Equal(1, counter.OnConnectedAsyncCount);
                    Assert.Equal(1, counter.InvokeMethodAsyncCount);
                    Assert.Equal(1, counter.OnDisconnectedAsyncCount);
                }
            }
        }

        public class NoExceptionFilter : IHubFilter
        {
            public async Task OnConnectedAsync(HubCallerContext context, Func<HubCallerContext, Task> next)
            {
                try
                {
                    await next(context);
                }
                catch { }
            }

            public async Task OnDisconnectedAsync(HubCallerContext context, Func<HubCallerContext, Task> next)
            {
                try
                {
                    await next(context);
                }
                catch { }
            }

            public async ValueTask<object> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object>> next)
            {
                try
                {
                    return await next(invocationContext);
                }
                catch { }

                return null;
            }
        }

        [Fact]
        public async Task ConnectionDisconnectedIfOnConnectedAsyncThrowsAndFilterDoesNot()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
                {
                    services.AddSignalR(options =>
                    {
                        options.AddFilter<NoExceptionFilter>();
                    });
                }, LoggerFactory);

                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<OnConnectedThrowsHub>>();

                using (var client = new TestClient())
                {
                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                    var closeMessage = Assert.IsType<CloseMessage>(await client.ReadAsync().OrTimeout());

                    Assert.False(closeMessage.AllowReconnect);
                    Assert.Null(closeMessage.Error);

                    await connectionHandlerTask.OrTimeout();
                }
            }
        }

        public class SkipNextFilter : IHubFilter
        {
            private readonly bool _skipOnConnected;
            private readonly bool _skipInvoke;
            private readonly bool _skipOnDisconnected;

            public SkipNextFilter(bool skipOnConnected = false, bool skipInvoke = false, bool skipOnDisconnected = false)
            {
                _skipOnConnected = skipOnConnected;
                _skipInvoke = skipInvoke;
                _skipOnDisconnected = skipOnDisconnected;
            }

            public Task OnConnectedAsync(HubCallerContext context, Func<HubCallerContext, Task> next)
            {
                if (_skipOnConnected)
                {
                    return Task.CompletedTask;
                }

                return next(context);
            }

            public Task OnDisconnectedAsync(HubCallerContext context, Func<HubCallerContext, Task> next)
            {
                if (_skipOnDisconnected)
                {
                    return Task.CompletedTask;
                }

                return next(context);
            }

            public ValueTask<object> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object>> next)
            {
                if (_skipInvoke)
                {
                    return new ValueTask<object>(Task.FromResult<object>(null));
                }

                return next(invocationContext);
            }
        }

        [Fact]
        public async Task ConnectionDisconnectedIfOnConnectedAsyncNotCalledByFilter()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
                {
                    services.AddSignalR(options =>
                    {
                        options.AddFilter(new SkipNextFilter(skipOnConnected: true));
                    });
                }, LoggerFactory);

                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

                using (var client = new TestClient())
                {
                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                    var closeMessage = Assert.IsType<CloseMessage>(await client.ReadAsync().OrTimeout());

                    Assert.False(closeMessage.AllowReconnect);
                    Assert.Null(closeMessage.Error);

                    await connectionHandlerTask.OrTimeout();
                }
            }
        }

        [Fact]
        public async Task Invoke()
        {
            using (StartVerifiableLog())
            {
                var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
                {
                    services.AddSignalR(options =>
                    {
                        options.AddFilter(new SkipNextFilter(skipInvoke: true));
                    });
                }, LoggerFactory);

                var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

                using (var client = new TestClient())
                {
                    var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                    await client.Connected.OrTimeout();

                    var message = await client.InvokeAsync(nameof(MethodHub.Echo), "Hello world!").OrTimeout();

                    Assert.Equal("Method not called", message.Error);

                    client.Dispose();

                    await connectionHandlerTask.OrTimeout();
                }
            }
        }
    }
}
