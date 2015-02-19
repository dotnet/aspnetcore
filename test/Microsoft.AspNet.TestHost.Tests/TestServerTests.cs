// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Xunit;

namespace Microsoft.AspNet.TestHost
{
    public class TestServerTests
    {
        [Fact]
        public void CreateWithDelegate()
        {
            // Arrange
            var services = HostingServices.Create().BuildServiceProvider();

            // Act & Assert (Does not throw)
            TestServer.Create(services, app => { });
        }

        [Fact]
        public void ThrowsIfNoApplicationEnvironmentIsRegisteredWithTheProvider()
        {
            // Arrange
            var services = new ServiceCollection().BuildServiceProvider();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => TestServer.Create(services, new Startup().Configuration));
        }

        [Fact]
        public async Task CanAccessHttpContext()
        {
            TestServer server = TestServer.Create(app =>
            {
                app.Run(context =>
                {
                    var accessor = app.ApplicationServices.GetRequiredService<IHttpContextAccessor>();
                    return context.Response.WriteAsync("HasContext:"+(accessor.Value != null));
                });
            });

            string result = await server.CreateClient().GetStringAsync("/path");
            Assert.Equal("HasContext:True", result);
        }

        public class ContextHolder
        {
            public ContextHolder(IHttpContextAccessor accessor)
            {
                Accessor = accessor;
            }

            public IHttpContextAccessor Accessor { get; set; }
        }

        [Fact]
        public async Task CanAddNewHostServices()
        {
            TestServer server = TestServer.Create(app =>
            {
                var a = app.ApplicationServices.GetRequiredService<IHttpContextAccessor>();

                app.Run(context =>
                {
                    var b = app.ApplicationServices.GetRequiredService<IHttpContextAccessor>();
                    var accessor = app.ApplicationServices.GetRequiredService<ContextHolder>();
                    return context.Response.WriteAsync("HasContext:" + (accessor.Accessor.Value != null));
                });
            }, newHostServices => newHostServices.AddSingleton<ContextHolder>());

            string result = await server.CreateClient().GetStringAsync("/path");
            Assert.Equal("HasContext:True", result);
        }

        [Fact]
        public async Task CreateInvokesApp()
        {
            TestServer server = TestServer.Create(app =>
            {
                app.Run(context =>
                {
                    return context.Response.WriteAsync("CreateInvokesApp");
                });
            });

            string result = await server.CreateClient().GetStringAsync("/path");
            Assert.Equal("CreateInvokesApp", result);
        }

        [Fact]
        public void WebRootCanBeResolvedWhenNotInTheProjectJson()
        {
            TestServer server = TestServer.Create(app =>
            {
                var env = app.ApplicationServices.GetRequiredService<IHostingEnvironment>();
                Assert.Equal(Directory.GetCurrentDirectory(), env.WebRoot);
            });
        }

        [Fact]
        public async Task DisposeStreamIgnored()
        {
            TestServer server = TestServer.Create(app =>
            {
                app.Run(async context =>
                {
                    await context.Response.WriteAsync("Response");
                    context.Response.Body.Dispose();
                });
            });

            HttpResponseMessage result = await server.CreateClient().GetAsync("/");
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.Equal("Response", await result.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task DisposedServerThrows()
        {
            TestServer server = TestServer.Create(app =>
            {
                app.Run(async context =>
                {
                    await context.Response.WriteAsync("Response");
                    context.Response.Body.Dispose();
                });
            });

            HttpResponseMessage result = await server.CreateClient().GetAsync("/");
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            server.Dispose();
            await Assert.ThrowsAsync<ObjectDisposedException>(() => server.CreateClient().GetAsync("/"));
        }

        [Fact]
        public void CancelAborts()
        {
            TestServer server = TestServer.Create(app =>
            {
                app.Run(context =>
                {
                    TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();
                    tcs.SetCanceled();
                    return tcs.Task;
                });
            });

            Assert.Throws<AggregateException>(() => { string result = server.CreateClient().GetStringAsync("/path").Result; });
        }

        public class Startup
        {
            public void Configuration(IApplicationBuilder builder)
            {
                builder.Run(ctx => ctx.Response.WriteAsync("Startup"));
            }
        }

        public class AnotherStartup
        {
            public void Configuration(IApplicationBuilder builder)
            {
                builder.Run(ctx => ctx.Response.WriteAsync("Another Startup"));
            }
        }
    }
}
