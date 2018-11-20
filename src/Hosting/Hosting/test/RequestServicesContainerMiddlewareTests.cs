// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Builder.Internal;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Hosting.Tests
{
    public class RequestServicesContainerMiddlewareTests
    {
        [Fact]
        public async Task RequestServicesAreSet()
        {
            var serviceProvider = new ServiceCollection()
                        .BuildServiceProvider();

            var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

            var middleware = new RequestServicesContainerMiddleware(
                ctx => Task.CompletedTask,
                scopeFactory);

            var context = new DefaultHttpContext();
            await middleware.Invoke(context);

            Assert.NotNull(context.RequestServices);
        }

        [Fact]
        public async Task RequestServicesAreNotOverwrittenIfAlreadySet()
        {
            var serviceProvider = new ServiceCollection()
                        .BuildServiceProvider();

            var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

            var middleware = new RequestServicesContainerMiddleware(
                ctx => Task.CompletedTask,
                scopeFactory);

            var context = new DefaultHttpContext();
            context.RequestServices = serviceProvider;
            await middleware.Invoke(context);

            Assert.Same(serviceProvider, context.RequestServices);
        }

        [Fact]
        public async Task RequestServicesAreDisposedOnCompleted()
        {
            var serviceProvider = new ServiceCollection()
                        .AddTransient<DisposableThing>()
                        .BuildServiceProvider();

            var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
            DisposableThing instance = null;

            var middleware = new RequestServicesContainerMiddleware(
                ctx =>
                {
                    instance = ctx.RequestServices.GetRequiredService<DisposableThing>();
                    return Task.CompletedTask;
                },
                scopeFactory);

            var context = new DefaultHttpContext();
            var responseFeature = new TestHttpResponseFeature();
            context.Features.Set<IHttpResponseFeature>(responseFeature);

            await middleware.Invoke(context);

            Assert.NotNull(context.RequestServices);
            Assert.Single(responseFeature.CompletedCallbacks);

            var callback = responseFeature.CompletedCallbacks[0];
            await callback.callback(callback.state);

            Assert.Null(context.RequestServices);
            Assert.True(instance.Disposed);
        }

        private class DisposableThing : IDisposable
        {
            public bool Disposed { get; set; }
            public void Dispose()
            {
                Disposed = true;
            }
        }

        private class TestHttpResponseFeature : IHttpResponseFeature
        {
            public List<(Func<object, Task> callback, object state)> CompletedCallbacks = new List<(Func<object, Task> callback, object state)>();

            public int StatusCode { get; set; }
            public string ReasonPhrase { get; set; }
            public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
            public Stream Body { get; set; }

            public bool HasStarted => false;

            public void OnCompleted(Func<object, Task> callback, object state)
            {
                CompletedCallbacks.Add((callback, state));
            }

            public void OnStarting(Func<object, Task> callback, object state)
            {
            }
        }
    }
}