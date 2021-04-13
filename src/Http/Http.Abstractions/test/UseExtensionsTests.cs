// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Microsoft.AspNetCore.Builder.Extensions
{
    public class UseExtensionsTests
    {
        [Fact]
        public async Task UseCallsNextMiddleware()
        {
            // Arrange
            var builder = new ApplicationBuilder(serviceProvider: null!);
            var context = new DefaultHttpContext();
            var firstCalled = false;
            var secondCalled = false;
            var lastCalled = false;

            builder.Use((context, next) =>
            {
                firstCalled = true;
                return next();
            });
            builder.Use((context, next) =>
            {
                Assert.True(firstCalled);
                secondCalled = true;
                return next(context);
            });
            builder.Run(context =>
            {
                Assert.True(secondCalled);
                lastCalled = true;
                return Task.CompletedTask;
            });

            // Act
            await builder.Build().Invoke(context);

            // Assert
            Assert.True(firstCalled);
            Assert.True(secondCalled);
            Assert.True(lastCalled);
        }

        [Fact]
        public async Task ThrowFromMiddlewareFlowsBackToInvoke()
        {
            // Arrange
            var builder = new ApplicationBuilder(serviceProvider: null!);
            var context = new DefaultHttpContext();
            var shouldThrow = true;

            builder.Use(async (context, next) =>
            {
                throw await Assert.ThrowsAsync<Exception>(() => next());
            });
            builder.Use(async (context, next) =>
            {
                throw await Assert.ThrowsAsync<Exception>(() => next(context));
            });
            builder.Run(context =>
            {
                if (shouldThrow)
                {
                    throw new Exception("From Use");
                }
                return Task.CompletedTask;
            });

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => builder.Build().Invoke(context));
            Assert.Equal("From Use", ex.Message);
        }
    }
}
