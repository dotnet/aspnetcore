// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.Extensions.DependencyModel;
using Xunit;

namespace Microsoft.AspNetCore.Components.Test.Routing
{
    public class RouterTest
    {
        [Fact]
        public void CanRunOnNavigateViaLocationChangeAsync()
        {
            // Arrange
            var router = CreateMockRouter();
            var called = false;
            async Task OnNavigateAsync(NavigationContext args)
            {
                await Task.CompletedTask;
                called = true;
            }
            router.OnNavigateAsync = new EventCallbackFactory().Create<NavigationContext>(router, OnNavigateAsync);

            // Act
            router.OnLocationChanged(null, new LocationChangedEventArgs("http://example.com/jan", false));

            // Assert
            Assert.True(called);
        }

        [Fact]
        public void CanCancelPreviousOnNavigateAsync()
        {
            // Arrange
            var router = CreateMockRouter();
            var cancelled = "";
            async Task OnNavigateAsync(NavigationContext args)
            {
                await Task.CompletedTask;
                args.CancellationToken.Register(() => cancelled = args.Path);
            };
            router.OnNavigateAsync = new EventCallbackFactory().Create<NavigationContext>(router, OnNavigateAsync);

            // Act
            router.OnLocationChanged(null, new LocationChangedEventArgs("http://example.com/jan", false));
            router.OnLocationChanged(null, new LocationChangedEventArgs("http://example.com/feb", false));

            // Assert
            var expected = "jan";
            Assert.Equal(cancelled, expected);
        }

        [Fact]
        public async Task RefreshesOnceOnCancelledOnNavigateAsync()
        {
            // Arrange
            var router = CreateMockRouter();
            async Task OnNavigateAsync(NavigationContext args)
            {
                if (args.Path.EndsWith("jan"))
                {
                    await Task.Delay(5000);
                }
                if (args.Path.EndsWith("feb"))
                {
                    await Task.CompletedTask;
                }
            };
            router.OnNavigateAsync = new EventCallbackFactory().Create<NavigationContext>(router, OnNavigateAsync);

            // Act
            var janTask = router.RunOnNavigateAsync("jan");
            var febTask = router.RunOnNavigateAsync("feb");

            await janTask;
            await febTask;

            // Assert
            Assert.False(janTask.Result);
            Assert.True(febTask.Result);
        }

        private Router CreateMockRouter()
        {
            var router = new Router();
            var renderer = new TestRenderer();
            router._renderHandle = new RenderHandle(renderer, 0);
            router.Routes = RouteTableFactory.Create(new[] { typeof(JanComponent), typeof(FebComponent) });
            router.NavigationManager = new TestNavigationManager();
            return router;
        }

        [Route("jan")]
        private class JanComponent : ComponentBase { }

        [Route("feb")]
        private class FebComponent : ComponentBase { }

        private class TestNavigationManager : NavigationManager
        {
            public TestNavigationManager()
            {
                Initialize("http://example.com/", "http://example.com/months");
            }

            public new void Initialize(string baseUri, string uri)
            {
                base.Initialize(baseUri, uri);
            }

            protected override void NavigateToCore(string uri, bool forceLoad)
            {
                throw new System.NotImplementedException();
            }
        }
    }
}
