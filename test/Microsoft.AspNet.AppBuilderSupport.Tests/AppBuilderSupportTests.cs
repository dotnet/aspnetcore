using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;
using Owin;
using Xunit;

namespace Microsoft.AspNet.AppBuilderSupport.Tests
{
    public class AppBuilderSupportTests
    {
        [Fact]
        public async Task BuildCanGoInsideAppBuilder()
        {
            var server = Microsoft.Owin.Testing.TestServer.Create(
                app => app.UseBuilder(HelloWorld));

            var result = await server.CreateRequest("/hello").GetAsync();
            var body = await result.Content.ReadAsStringAsync();

            Assert.Equal(result.StatusCode, HttpStatusCode.Accepted);
            Assert.Equal(body, "Hello world!");
        }

        private void HelloWorld(IBuilder builder)
        {
            builder.Use(next => async context =>
            {
                await next(context);
            });
            builder.Run(async context =>
            {
                context.Response.StatusCode = 202;
                await context.Response.WriteAsync("Hello world!");
            });
        }
    }
}
