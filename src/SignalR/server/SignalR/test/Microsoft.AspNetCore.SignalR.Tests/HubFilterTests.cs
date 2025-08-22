// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Tests;

public class HubFilterTests : VerifiableLoggedTest
{
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
    public async Task PerHubFilterByCompileTimeType_MethodsAreCalled()
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

    [Fact]
    public async Task PerHubFilterByRuntimeType_MethodsAreCalled()
    {
        using (StartVerifiableLog())
        {
            var tcsService = new TcsService();
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
            {
                services.AddSignalR().AddHubOptions<MethodHub>(options =>
                {
                    options.AddFilter(typeof(VerifyMethodFilter));
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

            await tcsService.StartedMethod.Task.DefaultTimeout();
            await client.Connected.DefaultTimeout();
            await tcsService.EndMethod.Task.DefaultTimeout();

            tcsService.Reset();
            var message = await client.InvokeAsync(nameof(MethodHub.Echo), "Hello world!").DefaultTimeout();
            await tcsService.EndMethod.Task.DefaultTimeout();
            tcsService.Reset();

            Assert.Null(message.Error);

            client.Dispose();

            await connectionHandlerTask.DefaultTimeout();

            await tcsService.EndMethod.Task.DefaultTimeout();
        }
    }

    [Fact]
    public async Task HubFilterDoesNotNeedToImplementMethods()
    {
        using (StartVerifiableLog())
        {
            var tcsService = new TcsService();
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
            {
                services.AddSignalR().AddHubOptions<DynamicTestHub>(options =>
                {
                    options.AddFilter(typeof(EmptyFilter));
                });
            }, LoggerFactory);

            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<DynamicTestHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                await client.Connected.DefaultTimeout();

                var completion = await client.InvokeAsync(nameof(DynamicTestHub.Echo), "hello");
                Assert.Null(completion.Error);
                Assert.Equal("hello", completion.Result);

                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();
            }
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

                await tcsService1.StartedMethod.Task.DefaultTimeout();
                await tcsService2.StartedMethod.Task.DefaultTimeout();
                await client.Connected.DefaultTimeout();
                await tcsService1.EndMethod.Task.DefaultTimeout();
                await tcsService2.EndMethod.Task.DefaultTimeout();

                tcsService1.Reset();
                tcsService2.Reset();
                var message = await client.InvokeAsync(nameof(MethodHub.Echo), "Hello world!").DefaultTimeout();
                await tcsService1.EndMethod.Task.DefaultTimeout();
                await tcsService2.EndMethod.Task.DefaultTimeout();
                tcsService1.Reset();
                tcsService2.Reset();

                Assert.Null(message.Error);

                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();

                await tcsService1.EndMethod.Task.DefaultTimeout();
                await tcsService2.EndMethod.Task.DefaultTimeout();
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

                await tcsService1.StartedMethod.Task.DefaultTimeout();
                await tcsService2.StartedMethod.Task.DefaultTimeout();
                await client.Connected.DefaultTimeout();
                await tcsService1.EndMethod.Task.DefaultTimeout();
                await tcsService2.EndMethod.Task.DefaultTimeout();

                tcsService1.Reset();
                tcsService2.Reset();
                var message = await client.InvokeAsync(nameof(MethodHub.Echo), "Hello world!").DefaultTimeout();
                await tcsService1.EndMethod.Task.DefaultTimeout();
                await tcsService2.EndMethod.Task.DefaultTimeout();
                tcsService1.Reset();
                tcsService2.Reset();

                Assert.Null(message.Error);

                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();

                await tcsService1.EndMethod.Task.DefaultTimeout();
                await tcsService2.EndMethod.Task.DefaultTimeout();
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

                await tcsService1.StartedMethod.Task.DefaultTimeout();
                await tcsService2.StartedMethod.Task.DefaultTimeout();
                await client.Connected.DefaultTimeout();
                await tcsService1.EndMethod.Task.DefaultTimeout();
                await tcsService2.EndMethod.Task.DefaultTimeout();

                tcsService1.Reset();
                tcsService2.Reset();
                var message = await client.InvokeAsync(nameof(MethodHub.Echo), "Hello world!").DefaultTimeout();
                await tcsService1.EndMethod.Task.DefaultTimeout();
                await tcsService2.EndMethod.Task.DefaultTimeout();
                tcsService1.Reset();
                tcsService2.Reset();

                Assert.Null(message.Error);

                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();

                await tcsService1.EndMethod.Task.DefaultTimeout();
                await tcsService2.EndMethod.Task.DefaultTimeout();
            }
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

                await syncPoints1[0].WaitForSyncPoint().DefaultTimeout();
                // Second filter wont run yet because first filter is waiting on SyncPoint
                Assert.False(syncPoints2[0].WaitForSyncPoint().IsCompleted);
                syncPoints1[0].Continue();

                await syncPoints2[0].WaitForSyncPoint().DefaultTimeout();
                syncPoints2[0].Continue();
                await client.Connected.DefaultTimeout();

                var invokeTask = client.InvokeAsync(nameof(MethodHub.Echo), "Hello world!");

                await syncPoints1[1].WaitForSyncPoint().DefaultTimeout();
                // Second filter wont run yet because first filter is waiting on SyncPoint
                Assert.False(syncPoints2[1].WaitForSyncPoint().IsCompleted);
                syncPoints1[1].Continue();

                await syncPoints2[1].WaitForSyncPoint().DefaultTimeout();
                syncPoints2[1].Continue();
                var message = await invokeTask.DefaultTimeout();

                Assert.Null(message.Error);

                client.Dispose();

                await syncPoints1[2].WaitForSyncPoint().DefaultTimeout();
                // Second filter wont run yet because first filter is waiting on SyncPoint
                Assert.False(syncPoints2[2].WaitForSyncPoint().IsCompleted);
                syncPoints1[2].Continue();

                await syncPoints2[2].WaitForSyncPoint().DefaultTimeout();
                syncPoints2[2].Continue();

                await connectionHandlerTask.DefaultTimeout();
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

                await syncPoints1[0].WaitForSyncPoint().DefaultTimeout();
                // Second filter wont run yet because first filter is waiting on SyncPoint
                Assert.False(syncPoints2[0].WaitForSyncPoint().IsCompleted);
                syncPoints1[0].Continue();

                await syncPoints2[0].WaitForSyncPoint().DefaultTimeout();
                syncPoints2[0].Continue();
                await client.Connected.DefaultTimeout();

                var invokeTask = client.InvokeAsync(nameof(MethodHub.Echo), "Hello world!");

                await syncPoints1[1].WaitForSyncPoint().DefaultTimeout();
                // Second filter wont run yet because first filter is waiting on SyncPoint
                Assert.False(syncPoints2[1].WaitForSyncPoint().IsCompleted);
                syncPoints1[1].Continue();

                await syncPoints2[1].WaitForSyncPoint().DefaultTimeout();
                syncPoints2[1].Continue();
                var message = await invokeTask.DefaultTimeout();

                Assert.Null(message.Error);

                client.Dispose();

                await syncPoints1[2].WaitForSyncPoint().DefaultTimeout();
                // Second filter wont run yet because first filter is waiting on SyncPoint
                Assert.False(syncPoints2[2].WaitForSyncPoint().IsCompleted);
                syncPoints1[2].Continue();

                await syncPoints2[2].WaitForSyncPoint().DefaultTimeout();
                syncPoints2[2].Continue();

                await connectionHandlerTask.DefaultTimeout();
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

                await syncPoints1[0].WaitForSyncPoint().DefaultTimeout();
                // Second filter wont run yet because first filter is waiting on SyncPoint
                Assert.False(syncPoints2[0].WaitForSyncPoint().IsCompleted);
                syncPoints1[0].Continue();

                await syncPoints2[0].WaitForSyncPoint().DefaultTimeout();
                syncPoints2[0].Continue();
                await client.Connected.DefaultTimeout();

                var invokeTask = client.InvokeAsync(nameof(MethodHub.Echo), "Hello world!");

                await syncPoints1[1].WaitForSyncPoint().DefaultTimeout();
                // Second filter wont run yet because first filter is waiting on SyncPoint
                Assert.False(syncPoints2[1].WaitForSyncPoint().IsCompleted);
                syncPoints1[1].Continue();

                await syncPoints2[1].WaitForSyncPoint().DefaultTimeout();
                syncPoints2[1].Continue();
                var message = await invokeTask.DefaultTimeout();

                Assert.Null(message.Error);

                client.Dispose();

                await syncPoints1[2].WaitForSyncPoint().DefaultTimeout();
                // Second filter wont run yet because first filter is waiting on SyncPoint
                Assert.False(syncPoints2[2].WaitForSyncPoint().IsCompleted);
                syncPoints1[2].Continue();

                await syncPoints2[2].WaitForSyncPoint().DefaultTimeout();
                syncPoints2[2].Continue();

                await connectionHandlerTask.DefaultTimeout();
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

                await client.Connected.DefaultTimeout();
                // Filter is transient, so these counts are reset every time the filter is created
                Assert.Equal(1, counter.OnConnectedAsyncCount);
                Assert.Equal(0, counter.InvokeMethodAsyncCount);
                Assert.Equal(0, counter.OnDisconnectedAsyncCount);

                var message = await client.InvokeAsync(nameof(MethodHub.Echo), "Hello world!").DefaultTimeout();
                // Filter is transient, so these counts are reset every time the filter is created
                Assert.Equal(0, counter.OnConnectedAsyncCount);
                Assert.Equal(1, counter.InvokeMethodAsyncCount);
                Assert.Equal(0, counter.OnDisconnectedAsyncCount);

                Assert.Null(message.Error);

                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();

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

                await client.Connected.DefaultTimeout();
                Assert.Equal(1, counter.OnConnectedAsyncCount);
                Assert.Equal(0, counter.InvokeMethodAsyncCount);
                Assert.Equal(0, counter.OnDisconnectedAsyncCount);

                var message = await client.InvokeAsync(nameof(MethodHub.Echo), "Hello world!").DefaultTimeout();
                Assert.Equal(1, counter.OnConnectedAsyncCount);
                Assert.Equal(1, counter.InvokeMethodAsyncCount);
                Assert.Equal(0, counter.OnDisconnectedAsyncCount);

                Assert.Null(message.Error);

                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();

                Assert.Equal(1, counter.OnConnectedAsyncCount);
                Assert.Equal(1, counter.InvokeMethodAsyncCount);
                Assert.Equal(1, counter.OnDisconnectedAsyncCount);
            }
        }
    }

    [Fact]
    public async Task ConnectionContinuesIfOnConnectedAsyncThrowsAndFilterDoesNot()
    {
        using (StartVerifiableLog())
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
            {
                services.AddSignalR(options =>
                {
                    options.EnableDetailedErrors = true;
                    options.AddFilter<NoExceptionFilter>();
                });
            }, LoggerFactory);

            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<OnConnectedThrowsHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                // Verify connection still connected, can't invoke a method if the connection is disconnected
                var message = await client.InvokeAsync("Method");
                Assert.Equal("Failed to invoke 'Method' due to an error on the server. HubException: Method does not exist.", message.Error);

                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();
            }
        }
    }

    [Fact]
    public async Task ConnectionContinuesIfOnConnectedAsyncNotCalledByFilter()
    {
        using (StartVerifiableLog())
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
            {
                services.AddSignalR(options =>
                {
                    options.EnableDetailedErrors = true;
                    options.AddFilter(new SkipNextFilter(skipOnConnected: true));
                });
            }, LoggerFactory);

            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                // Verify connection still connected, can't invoke a method if the connection is disconnected
                var message = await client.InvokeAsync("Method");
                Assert.Equal("Failed to invoke 'Method' due to an error on the server. HubException: Method does not exist.", message.Error);

                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();
            }
        }
    }

    [Fact]
    public async Task FilterCanSkipCallingHubMethod()
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

                await client.Connected.DefaultTimeout();

                var message = await client.InvokeAsync(nameof(MethodHub.Echo), "Hello world!").DefaultTimeout();

                Assert.Null(message.Error);
                Assert.Null(message.Result);

                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();
            }
        }
    }

    [Fact]
    public async Task FiltersWithIDisposableAreDisposed()
    {
        using (StartVerifiableLog())
        {
            var tcsService = new TcsService();
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
            {
                services.AddSignalR(options =>
                {
                    options.EnableDetailedErrors = true;
                    options.AddFilter<DisposableFilter>();
                });

                services.AddSingleton(tcsService);
            }, LoggerFactory);

            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                // OnConnectedAsync creates and destroys the filter
                await tcsService.StartedMethod.Task.DefaultTimeout();
                tcsService.Reset();

                var message = await client.InvokeAsync("Echo", "Hello");
                Assert.Equal("Hello", message.Result);
                await tcsService.StartedMethod.Task.DefaultTimeout();
                tcsService.Reset();

                client.Dispose();

                // OnDisconnectedAsync creates and destroys the filter
                await tcsService.StartedMethod.Task.DefaultTimeout();
                await connectionHandlerTask.DefaultTimeout();
            }
        }
    }

    [Fact]
    public async Task InstanceFiltersWithIDisposableAreNotDisposed()
    {
        using (StartVerifiableLog())
        {
            var tcsService = new TcsService();
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
            {
                services.AddSignalR(options =>
                {
                    options.EnableDetailedErrors = true;
                    options.AddFilter(new DisposableFilter(tcsService));
                });

                services.AddSingleton(tcsService);
            }, LoggerFactory);

            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                var message = await client.InvokeAsync("Echo", "Hello");
                Assert.Equal("Hello", message.Result);

                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();

                Assert.False(tcsService.StartedMethod.Task.IsCompleted);
            }
        }
    }

    [Fact]
    public async Task FiltersWithIAsyncDisposableAreDisposed()
    {
        using (StartVerifiableLog())
        {
            var tcsService = new TcsService();
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
            {
                services.AddSignalR(options =>
                {
                    options.EnableDetailedErrors = true;
                    options.AddFilter<AsyncDisposableFilter>();
                });

                services.AddSingleton(tcsService);
            }, LoggerFactory);

            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                // OnConnectedAsync creates and destroys the filter
                await tcsService.StartedMethod.Task.DefaultTimeout();
                tcsService.Reset();

                var message = await client.InvokeAsync("Echo", "Hello");
                Assert.Equal("Hello", message.Result);
                await tcsService.StartedMethod.Task.DefaultTimeout();
                tcsService.Reset();

                client.Dispose();

                // OnDisconnectedAsync creates and destroys the filter
                await tcsService.StartedMethod.Task.DefaultTimeout();
                await connectionHandlerTask.DefaultTimeout();
            }
        }
    }

    [Fact]
    public async Task InstanceFiltersWithIAsyncDisposableAreNotDisposed()
    {
        using (StartVerifiableLog())
        {
            var tcsService = new TcsService();
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
            {
                services.AddSignalR(options =>
                {
                    options.EnableDetailedErrors = true;
                    options.AddFilter(new AsyncDisposableFilter(tcsService));
                });

                services.AddSingleton(tcsService);
            }, LoggerFactory);

            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                var message = await client.InvokeAsync("Echo", "Hello");
                Assert.Equal("Hello", message.Result);

                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();

                Assert.False(tcsService.StartedMethod.Task.IsCompleted);
            }
        }
    }

    [Fact]
    public async Task InvokeFailsWhenFilterCallsNonExistantMethod()
    {
        bool ExpectedErrors(WriteContext writeContext)
        {
            return writeContext.LoggerName == "Microsoft.AspNetCore.SignalR.Internal.DefaultHubDispatcher" &&
                   writeContext.EventId.Name == "FailedInvokingHubMethod";
        }

        using (StartVerifiableLog(expectedErrorsFilter: ExpectedErrors))
        {
            var serviceProvider = HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
            {
                services.AddSignalR(options =>
                {
                    options.EnableDetailedErrors = true;
                    options.AddFilter<ChangeMethodFilter>();
                });
            }, LoggerFactory);

            var connectionHandler = serviceProvider.GetService<HubConnectionHandler<MethodHub>>();

            using (var client = new TestClient())
            {
                var connectionHandlerTask = await client.ConnectAsync(connectionHandler);

                var message = await client.InvokeAsync("Echo", "Hello");
                Assert.Equal("An unexpected error occurred invoking 'Echo' on the server. HubException: Unknown hub method 'BaseMethod'", message.Error);

                client.Dispose();

                await connectionHandlerTask.DefaultTimeout();
            }
        }
    }
}
