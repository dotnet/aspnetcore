// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.AspNetCore.Components.Routing
{
    public class RouterTest
    {
        private readonly Router _router;
        private readonly TestNavigationManager _navigationManager;
        private readonly TestRenderer _renderer;

        public RouterTest()
        {
            var services = new ServiceCollection();
            _navigationManager = new TestNavigationManager();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddSingleton<NavigationManager>(_navigationManager);
            services.AddSingleton<INavigationInterception, TestNavigationInterception>();
            var serviceProvider = services.BuildServiceProvider();

            _renderer = new TestRenderer(serviceProvider);
            _renderer.ShouldHandleExceptions = true;
            _router = (Router)_renderer.InstantiateComponent<Router>();
            _router.AppAssembly = Assembly.GetExecutingAssembly();
            _router.Found = routeData => (builder) => builder.AddContent(0, $"Rendering route matching {routeData.PageType}");
            _renderer.AssignRootComponentId(_router);
        }

        [Fact]
        public async Task CanRunOnNavigateAsync()
        {
            // Arrange
            var called = false;
            Action<NavigationContext> OnNavigateAsync = async (NavigationContext args) =>
            {
                await Task.CompletedTask;
                called = true;
            };
            _router.OnNavigateAsync = new EventCallback<NavigationContext>(null, OnNavigateAsync);

            // Act
            await _renderer.Dispatcher.InvokeAsync(() => _router.RunOnNavigateAsync("http://example.com/jan", false));

            // Assert
            Assert.True(called);
        }

        [Fact]
        public async Task CanceledFailedOnNavigateAsyncDoesNothing()
        {
            // Arrange
            var onNavigateInvoked = 0;
            Action<NavigationContext> OnNavigateAsync = async (NavigationContext args) =>
            {
                onNavigateInvoked += 1;
                if (args.Path.EndsWith("jan", StringComparison.Ordinal))
                {
                    await Task.Delay(Timeout.Infinite, args.CancellationToken);
                    throw new Exception("This is an uncaught exception.");
                }
            };
            var refreshCalled = 0;
            _renderer.OnUpdateDisplay = (renderBatch) =>
            {
                refreshCalled += 1;
                return;
            };
            _router.OnNavigateAsync = new EventCallback<NavigationContext>(null, OnNavigateAsync);

            // Act
            var janTask = _renderer.Dispatcher.InvokeAsync(() => _router.RunOnNavigateAsync("http://example.com/jan", false));
            var febTask = _renderer.Dispatcher.InvokeAsync(() => _router.RunOnNavigateAsync("http://example.com/feb", false));

            await janTask;
            await febTask;

            // Assert that we render the second route component and don't throw an exception
            Assert.Empty(_renderer.HandledExceptions);
            Assert.Equal(2, onNavigateInvoked);
            Assert.Equal(2, refreshCalled);
        }

        [Fact]
        public async Task AlreadyCanceledOnNavigateAsyncDoesNothing()
        {
            // Arrange
            var triggerCancel = new TaskCompletionSource();
            Action<NavigationContext> OnNavigateAsync = async (NavigationContext args) =>
            {
                if (args.Path.EndsWith("jan", StringComparison.Ordinal))
                {
                    var tcs = new TaskCompletionSource();
                    await triggerCancel.Task;
                    tcs.TrySetCanceled();
                    await tcs.Task;
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
            _router.OnNavigateAsync = new EventCallback<NavigationContext>(null, OnNavigateAsync);

            // Act (start the operations then await them)
            var jan = _renderer.Dispatcher.InvokeAsync(() => _router.RunOnNavigateAsync("http://example.com/jan", false));
            var feb = _renderer.Dispatcher.InvokeAsync(() => _router.RunOnNavigateAsync("http://example.com/feb", false));
            triggerCancel.TrySetResult();

            await jan;
            await feb;
        }

        [Fact]
        public void CanCancelPreviousOnNavigateAsync()
        {
            // Arrange
            var cancelled = "";
            Action<NavigationContext> OnNavigateAsync = async (NavigationContext args) =>
            {
                await Task.CompletedTask;
                args.CancellationToken.Register(() => cancelled = args.Path);
            };
            _router.OnNavigateAsync = new EventCallback<NavigationContext>(null, OnNavigateAsync);

            // Act
            _ = _router.RunOnNavigateAsync("jan", false);
            _ = _router.RunOnNavigateAsync("feb", false);

            // Assert
            var expected = "jan";
            Assert.Equal(expected, cancelled);
        }

        [Fact]
        public async Task RefreshesOnceOnCancelledOnNavigateAsync()
        {
            // Arrange
            Action<NavigationContext> OnNavigateAsync = async (NavigationContext args) =>
            {
                if (args.Path.EndsWith("jan", StringComparison.Ordinal))
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
            _router.OnNavigateAsync = new EventCallback<NavigationContext>(null, OnNavigateAsync);

            // Act
            var jan = _renderer.Dispatcher.InvokeAsync(() => _router.RunOnNavigateAsync("http://example.com/jan", false));
            var feb = _renderer.Dispatcher.InvokeAsync(() => _router.RunOnNavigateAsync("http://example.com/feb", false));

            await jan;
            await feb;
        }

        [Fact]
        public async Task UsesCurrentRouteMatchingIfSpecified()
        {
            // Arrange
            // Current routing prefers exactly-matched patterns over {*someWildcard}, no matter
            // how many segments are in the exact match
            _navigationManager.NotifyLocationChanged("https://www.example.com/subdir/a/b", false);
            var parameters = new Dictionary<string, object>
            {
                { nameof(Router.AppAssembly), typeof(RouterTest).Assembly },
                { nameof(Router.NotFound), (RenderFragment)(builder => { }) },
            };

            // Act
            await _renderer.Dispatcher.InvokeAsync(() =>
                _router.SetParametersAsync(ParameterView.FromDictionary(parameters)));

            // Assert
            var renderedFrame = _renderer.Batches.First().ReferenceFrames.First();
            Assert.Equal(RenderTreeFrameType.Text, renderedFrame.FrameType);
            Assert.Equal($"Rendering route matching {typeof(MultiSegmentRouteComponent)}", renderedFrame.TextContent);
        }

        internal class TestNavigationManager : NavigationManager
        {
            public TestNavigationManager() =>
                Initialize("https://www.example.com/subdir/", "https://www.example.com/subdir/jan");

            protected override void NavigateToCore(string uri, bool forceLoad) => throw new NotImplementedException();

            public void NotifyLocationChanged(string uri, bool intercepted)
            {
                Uri = uri;
                NotifyLocationChanged(intercepted);
            }
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

        [Route("{*matchAnything}")]
        public class MatchAnythingComponent : ComponentBase { }

        [Route("a/b")]
        public class MultiSegmentRouteComponent : ComponentBase { }
    }
}
