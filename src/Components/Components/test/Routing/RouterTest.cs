// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.Extensions.DependencyModel;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Components.Test.Routing
{
    public class RouterTest
    {
        [Fact]
        public void CanRunOnNavigateAsync()
        {
            // Arrange
            var router = CreateMockRouter();
            var called = false;
            async Task OnNavigateAsync(NavigationContext args)
            {
                await Task.CompletedTask;
                called = true;
            }
            router.Object.OnNavigateAsync = new EventCallbackFactory().Create<NavigationContext>(router, OnNavigateAsync);

            // Act
            router.Object.RunOnNavigateWithRefreshAsync("http://example.com/jan", false);

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
            router.Object.OnNavigateAsync = new EventCallbackFactory().Create<NavigationContext>(router, OnNavigateAsync);

            // Act
            router.Object.RunOnNavigateWithRefreshAsync("jan", false);
            router.Object.RunOnNavigateWithRefreshAsync("feb", false);

            // Assert
            var expected = "jan";
            Assert.Equal(cancelled, expected);
        }

        [Fact]
        public void RefreshesOnceOnCancelledOnNavigateAsync()
        {
            // Arrange
            var router = CreateMockRouter();
            async Task OnNavigateAsync(NavigationContext args)
            {
                if (args.Path.EndsWith("jan"))
                {
                    await Task.Delay(Timeout.Infinite);
                }
            };
            router.Object.OnNavigateAsync = new EventCallbackFactory().Create<NavigationContext>(router, OnNavigateAsync);

            // Act
            router.Object.RunOnNavigateWithRefreshAsync("jan", false);
            router.Object.RunOnNavigateWithRefreshAsync("feb", false);

            // Assert refresh should've only been called once for the second route
            router.Verify(x => x.Refresh(It.IsAny<bool>()), Times.Once());
        }

        private Mock<Router> CreateMockRouter()
        {
            var router = new Mock<Router>() { CallBase = true };
            router.Setup(x => x.Refresh(It.IsAny<bool>())).Verifiable();
            return router;
        }

        [Route("jan")]
        private class JanComponent : ComponentBase { }

        [Route("feb")]
        private class FebComponent : ComponentBase { }
    }
}
