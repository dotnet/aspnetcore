// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using Microsoft.AspNetCore.Components;

namespace Microsoft.AspNetCore.Components.Test.Routing
{
    public class RouterTest
    {
        private readonly Router _router;
        private readonly TestRenderer _renderer;

        public RouterTest()
        {
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddSingleton<NavigationManager, TestNavigationManager>();
            services.AddSingleton<INavigationInterception, TestNavigationInterception>();
            var serviceProvider = services.BuildServiceProvider();

            _renderer = new TestRenderer(serviceProvider);
            _renderer.ShouldHandleExceptions = true;
            _router = (Router)_renderer.InstantiateComponent<Router>();
            _router.AppAssembly = Assembly.GetExecutingAssembly();
            _router.Found = routeData => (builder) => builder.AddContent(0, "Rendering route...");
            _renderer.AssignRootComponentId(_router);
        }

        [Fact]
        public async Task CanRunOnNavigateAsync()
        {
            // Arrange
            var called = false;
            async Task OnNavigateAsync(NavigationContext args)
            {
                await Task.CompletedTask;
                called = true;
            }
            _router.OnNavigateAsync = OnNavigateAsync;

            // Act
            await _renderer.Dispatcher.InvokeAsync(() => _router.RunOnNavigateWithRefreshAsync("http://example.com/jan", false));

            // Assert
            Assert.True(called);
        }

        [Fact]
        public async Task CanHandleSingleFailedOnNavigateAsync()
        {
            // Arrange
            var called = false;
            async Task OnNavigateAsync(NavigationContext args)
            {
                called = true;
                await Task.CompletedTask;
                throw new Exception("This is an uncaught exception.");
            }
            _router.OnNavigateAsync = OnNavigateAsync;

            // Act
            await _renderer.Dispatcher.InvokeAsync(() => _router.RunOnNavigateWithRefreshAsync("http://example.com/jan", false));

            // Assert
            Assert.True(called);
            Assert.Single(_renderer.HandledExceptions);
            var unhandledException = _renderer.HandledExceptions[0];
            Assert.Equal("This is an uncaught exception.", unhandledException.Message);
        }

        [Fact]
        public async Task CanceledFailedOnNavigateAsyncDoesNothing()
        {
            // Arrange
            var onNavigateInvoked = 0;
            async Task OnNavigateAsync(NavigationContext args)
            {
                onNavigateInvoked += 1;
                if (args.Path.EndsWith("jan"))
                {
                    await Task.Delay(Timeout.Infinite, args.CancellationToken);
                    throw new Exception("This is an uncaught exception.");
                }
            }
            var refreshCalled = false;
            _renderer.OnUpdateDisplay = (renderBatch) =>
            {
                if (!refreshCalled)
                {
                    refreshCalled = true;
                    return;
                }
                Assert.True(false, "OnUpdateDisplay called more than once.");
            };
            _router.OnNavigateAsync = OnNavigateAsync;

            // Act
            var janTask = _renderer.Dispatcher.InvokeAsync(() => _router.RunOnNavigateWithRefreshAsync("http://example.com/jan", false));
            var febTask = _renderer.Dispatcher.InvokeAsync(() => _router.RunOnNavigateWithRefreshAsync("http://example.com/feb", false));

            await janTask;
            await febTask;

            // Assert that we render the second route component and don't throw an exception
            Assert.Empty(_renderer.HandledExceptions);
            Assert.Equal(2, onNavigateInvoked);
        }

        [Fact]
        public async Task CanHandleSingleCancelledOnNavigateAsync()
        {
            // Arrange
            async Task OnNavigateAsync(NavigationContext args)
            {
                var tcs = new TaskCompletionSource<int>();
                tcs.TrySetCanceled();
                await tcs.Task;
            }
            _renderer.OnUpdateDisplay = (renderBatch) => Assert.True(false, "OnUpdateDisplay called more than once.");
            _router.OnNavigateAsync = OnNavigateAsync;

            // Act
            await _renderer.Dispatcher.InvokeAsync(() => _router.RunOnNavigateWithRefreshAsync("http://example.com/jan", false));

            // Assert
            Assert.Single(_renderer.HandledExceptions);
            var unhandledException = _renderer.HandledExceptions[0];
            Assert.Equal("OnNavigateAsync can only be cancelled via NavigateContext.CancellationToken.", unhandledException.Message);
        }

        [Fact]
        public async Task AlreadyCanceledOnNavigateAsyncDoesNothing()
        {
            // Arrange
            var triggerCancel = new TaskCompletionSource();
            async Task OnNavigateAsync(NavigationContext args)
            {
                if (args.Path.EndsWith("jan"))
                {
                    var tcs = new TaskCompletionSource();
                    await triggerCancel.Task;
                    tcs.TrySetCanceled();
                    await tcs.Task;
                }
            }
            var refreshCalled = false;
            _renderer.OnUpdateDisplay = (renderBatch) =>
            {
                if (!refreshCalled)
                {
                    Assert.True(true);
                    return;
                }
                Assert.True(false, "OnUpdateDisplay called more than once.");
            };
            _router.OnNavigateAsync = OnNavigateAsync;

            // Act (start the operations then await them)
            var jan = _renderer.Dispatcher.InvokeAsync(() => _router.RunOnNavigateWithRefreshAsync("http://example.com/jan", false));
            var feb = _renderer.Dispatcher.InvokeAsync(() => _router.RunOnNavigateWithRefreshAsync("http://example.com/feb", false));
            triggerCancel.TrySetResult();

            await jan;
            await feb;
        }

        [Fact]
        public void CanCancelPreviousOnNavigateAsync()
        {
            // Arrange
            var cancelled = "";
            async Task OnNavigateAsync(NavigationContext args)
            {
                await Task.CompletedTask;
                args.CancellationToken.Register(() => cancelled = args.Path);
            };
            _router.OnNavigateAsync = OnNavigateAsync;

            // Act
            _ = _router.RunOnNavigateWithRefreshAsync("jan", false);
            _ = _router.RunOnNavigateWithRefreshAsync("feb", false);

            // Assert
            var expected = "jan";
            Assert.Equal(expected, cancelled);
        }

        [Fact]
        public async Task RefreshesOnceOnCancelledOnNavigateAsync()
        {
            // Arrange
            async Task OnNavigateAsync(NavigationContext args)
            {
                if (args.Path.EndsWith("jan"))
                {
                    await Task.Delay(Timeout.Infinite, args.CancellationToken);
                }
            };
            var refreshCalled = false;
            _renderer.OnUpdateDisplay = (renderBatch) =>
            {
                if (!refreshCalled)
                {
                    Assert.True(true);
                    return;
                }
                Assert.True(false, "OnUpdateDisplay called more than once.");
            };
            _router.OnNavigateAsync = OnNavigateAsync;

            // Act
            var jan = _renderer.Dispatcher.InvokeAsync(() => _router.RunOnNavigateWithRefreshAsync("http://example.com/jan", false));
            var feb = _renderer.Dispatcher.InvokeAsync(() => _router.RunOnNavigateWithRefreshAsync("http://example.com/feb", false));

            await jan;
            await feb;
        }

        internal class TestNavigationManager : NavigationManager
        {
            public TestNavigationManager() =>
                Initialize("https://www.example.com/subdir/", "https://www.example.com/subdir/jan");

            protected override void NavigateToCore(string uri, bool forceLoad) => throw new NotImplementedException();
        }

        internal sealed class TestNavigationInterception : INavigationInterception
        {
            public static readonly TestNavigationInterception Instance = new TestNavigationInterception();

            public Task EnableNavigationInterceptionAsync()
            {
                return Task.CompletedTask;
            }
        }

        [Route("feb")]
        public class FebComponent : ComponentBase { }

        [Route("jan")]
        public class JanComponent : ComponentBase { }
    }
}
