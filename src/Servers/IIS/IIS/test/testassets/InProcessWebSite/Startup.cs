// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.IISIntegration.FunctionalTests;
using Microsoft.AspNetCore.Server.IIS;
using Microsoft.AspNetCore.Server.IISIntegration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Primitives;
using Xunit;
using HttpFeatures = Microsoft.AspNetCore.Http.Features;

namespace TestSite
{
    public partial class Startup
    {
        public static bool StartupHookCalled;

        public void Configure(IApplicationBuilder app)
        {
            if (Environment.GetEnvironmentVariable("ENABLE_HTTPS_REDIRECTION") != null)
            {
                app.UseHttpsRedirection();
            }
            TestStartup.Register(app, this);
        }

        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddResponseCompression();
        }
#if FORWARDCOMPAT
        private async Task ContentRootPath(HttpContext ctx) => await ctx.Response.WriteAsync(ctx.RequestServices.GetService<Microsoft.AspNetCore.Hosting.IHostingEnvironment>().ContentRootPath);

        private async Task WebRootPath(HttpContext ctx) => await ctx.Response.WriteAsync(ctx.RequestServices.GetService<Microsoft.AspNetCore.Hosting.IHostingEnvironment>().WebRootPath);
#else
        private async Task ContentRootPath(HttpContext ctx) => await ctx.Response.WriteAsync(ctx.RequestServices.GetService<IWebHostEnvironment>().ContentRootPath);

        private async Task WebRootPath(HttpContext ctx) => await ctx.Response.WriteAsync(ctx.RequestServices.GetService<IWebHostEnvironment>().WebRootPath);
#endif

        private async Task CurrentDirectory(HttpContext ctx) => await ctx.Response.WriteAsync(Environment.CurrentDirectory);

        private async Task BaseDirectory(HttpContext ctx) => await ctx.Response.WriteAsync(AppContext.BaseDirectory);

        private async Task ASPNETCORE_IIS_PHYSICAL_PATH(HttpContext ctx) => await ctx.Response.WriteAsync(Environment.GetEnvironmentVariable("ASPNETCORE_IIS_PHYSICAL_PATH"));

        private async Task ServerAddresses(HttpContext ctx)
        {
            var serverAddresses = ctx.RequestServices.GetService<IServer>().Features.Get<IServerAddressesFeature>();
            await ctx.Response.WriteAsync(string.Join(",", serverAddresses.Addresses));
        }

        private async Task CheckProtocol(HttpContext ctx)
        {
            await ctx.Response.WriteAsync(ctx.Request.Protocol);
        }

        private async Task ConsoleWrite(HttpContext ctx)
        {
            Console.WriteLine("TEST MESSAGE");

            await ctx.Response.WriteAsync("Hello World");
        }

        private async Task ConsoleErrorWrite(HttpContext ctx)
        {
            Console.Error.WriteLine("TEST MESSAGE");

            await ctx.Response.WriteAsync("Hello World");
        }

        public async Task Auth(HttpContext ctx)
        {
            var authProvider = ctx.RequestServices.GetService<IAuthenticationSchemeProvider>();
            var authScheme = (await authProvider.GetAllSchemesAsync()).SingleOrDefault();

            await ctx.Response.WriteAsync(authScheme?.Name ?? "null");
            if (ctx.User.Identity.Name != null)
            {
                await ctx.Response.WriteAsync(":" + ctx.User.Identity.Name);
            }
        }

        public async Task GetClientCert(HttpContext context)
        {
            var clientCert = context.Connection.ClientCertificate;
            await context.Response.WriteAsync(clientCert != null ? $"Enabled;{clientCert.GetCertHashString()}" : "Disabled");
        }

        private static int _waitingRequestCount;

        public Task WaitForAbort(HttpContext context)
        {
            Interlocked.Increment(ref _waitingRequestCount);
            try
            {
                context.RequestAborted.WaitHandle.WaitOne();
                return Task.CompletedTask;
            }
            finally
            {
                Interlocked.Decrement(ref _waitingRequestCount);
            }
        }

        public Task Abort(HttpContext context)
        {
            context.Abort();
            return Task.CompletedTask;
        }

        public async Task WaitingRequestCount(HttpContext context)
        {
            await context.Response.WriteAsync(_waitingRequestCount.ToString());
        }

        public Task CreateFile(HttpContext context)
        {
#if FORWARDCOMPAT
            var hostingEnv = context.RequestServices.GetService<Microsoft.AspNetCore.Hosting.IHostingEnvironment>();
#else
            var hostingEnv = context.RequestServices.GetService<IWebHostEnvironment>();
#endif

            if (context.Connection.LocalIpAddress == null || context.Connection.RemoteIpAddress == null)
            {
                throw new Exception("Failed to set local and remote ip addresses");
            }

            File.WriteAllText(System.IO.Path.Combine(hostingEnv.ContentRootPath, "Started.txt"), "");
            return Task.CompletedTask;
        }

        public Task ConnectionClose(HttpContext context)
        {
            context.Response.Headers["connection"] = "close";
            return Task.CompletedTask;
        }

        public Task OverrideServer(HttpContext context)
        {
            context.Response.Headers["Server"] = "MyServer/7.8";
            return Task.CompletedTask;
        }

        public void CompressedData(IApplicationBuilder builder)
        {
            builder.UseResponseCompression();
            // write random bytes to check that compressed data is passed through
            builder.Run(
                async context =>
                {
                    context.Response.ContentType = "text/html";
                    await context.Response.Body.WriteAsync(new byte[100], 0, 100);
                });
        }

        [DllImport("kernel32.dll")]
        static extern uint GetDllDirectory(uint nBufferLength, [Out] StringBuilder lpBuffer);

        private async Task DllDirectory(HttpContext context)
        {
            var builder = new StringBuilder(1024);
            GetDllDirectory(1024, builder);
            await context.Response.WriteAsync(builder.ToString());
        }

        private async Task GetEnvironmentVariable(HttpContext ctx)
        {
            await ctx.Response.WriteAsync(Environment.GetEnvironmentVariable(ctx.Request.Query["name"].ToString()));
        }

        private async Task ServerVariable(HttpContext ctx)
        {
            var varName = ctx.Request.Query["q"];
            var newValue = ctx.Request.Query["v"];
            var feature = ctx.Features.Get<IServerVariablesFeature>();
            if (newValue.Count != 0)
            {
                feature[varName] = newValue;
            }
            await ctx.Response.WriteAsync($"{varName}: {feature[varName] ?? "(null)"}");
        }

        private async Task AuthenticationAnonymous(HttpContext ctx)
        {
            await ctx.Response.WriteAsync("Anonymous?" + !ctx.User.Identity.IsAuthenticated);
        }

        private async Task AuthenticationRestricted(HttpContext ctx)
        {
            if (ctx.User.Identity.IsAuthenticated)
            {
                await ctx.Response.WriteAsync(ctx.User.Identity.AuthenticationType);
            }
            else
            {
                await ctx.ChallengeAsync(IISServerDefaults.AuthenticationScheme);
            }
        }

        private async Task AuthenticationForbidden(HttpContext ctx)
        {
            await ctx.ForbidAsync(IISServerDefaults.AuthenticationScheme);
        }

        private async Task AuthenticationRestrictedNTLM(HttpContext ctx)
        {
            if (string.Equals("NTLM", ctx.User.Identity.AuthenticationType, StringComparison.Ordinal))
            {
                await ctx.Response.WriteAsync("NTLM");
            }
            else
            {
                await ctx.ChallengeAsync(IISServerDefaults.AuthenticationScheme);
            }
        }

        private async Task FeatureCollectionSetRequestFeatures(HttpContext ctx)
        {
            try
            {
                Assert.Equal("GET", ctx.Request.Method);
                ctx.Request.Method = "test";
                Assert.Equal("test", ctx.Request.Method);

                Assert.Equal("http", ctx.Request.Scheme);
                ctx.Request.Scheme = "test";
                Assert.Equal("test", ctx.Request.Scheme);

                Assert.Equal("/FeatureCollectionSetRequestFeatures", ctx.Request.PathBase);
                ctx.Request.PathBase = "/base";
                Assert.Equal("/base", ctx.Request.PathBase);

                Assert.Equal("/path", ctx.Request.Path);
                ctx.Request.Path = "/path";
                Assert.Equal("/path", ctx.Request.Path);

                Assert.Equal("?query", ctx.Request.QueryString.Value);
                ctx.Request.QueryString = QueryString.Empty;
                Assert.Equal("", ctx.Request.QueryString.Value);

                Assert.Equal("HTTP/1.1", ctx.Request.Protocol);
                ctx.Request.Protocol = "HTTP/1.0";
                Assert.Equal("HTTP/1.0", ctx.Request.Protocol);

                Assert.NotNull(ctx.Request.Headers);
                var headers = new HeaderDictionary();
                ctx.Features.Get<IHttpRequestFeature>().Headers = headers;
                Assert.Same(headers, ctx.Features.Get<IHttpRequestFeature>().Headers);

                Assert.NotNull(ctx.Request.Body);
                var body = new MemoryStream();
                ctx.Request.Body = body;
                Assert.Same(body, ctx.Request.Body);

                //Assert.NotNull(ctx.Features.Get<IHttpRequestIdentifierFeature>().TraceIdentifier);
                //Assert.NotEqual(CancellationToken.None, ctx.RequestAborted);
                //var token = new CancellationTokenSource().Token;
                //ctx.RequestAborted = token;
                //Assert.Equal(token, ctx.RequestAborted);

                await ctx.Response.WriteAsync("Success");
                return;
            }
            catch (Exception exception)
            {
                ctx.Response.StatusCode = 500;
                await ctx.Response.WriteAsync(exception.ToString());
            }
            await ctx.Response.WriteAsync("_Failure");
        }

        private async Task FeatureCollectionSetResponseFeatures(HttpContext ctx)
        {
            try
            {
                Assert.Equal(200, ctx.Response.StatusCode);
                ctx.Response.StatusCode = 404;
                Assert.Equal(404, ctx.Response.StatusCode);
                ctx.Response.StatusCode = 200;

                Assert.Null(ctx.Features.Get<IHttpResponseFeature>().ReasonPhrase);
                ctx.Features.Get<IHttpResponseFeature>().ReasonPhrase = "Set Response";
                Assert.Equal("Set Response", ctx.Features.Get<IHttpResponseFeature>().ReasonPhrase);

                Assert.NotNull(ctx.Response.Headers);
                var headers = new HeaderDictionary();
                ctx.Features.Get<IHttpResponseFeature>().Headers = headers;
                Assert.Same(headers, ctx.Features.Get<IHttpResponseFeature>().Headers);

                var originalBody = ctx.Response.Body;
                Assert.NotNull(originalBody);
                var body = new MemoryStream();
                ctx.Response.Body = body;
                Assert.Same(body, ctx.Response.Body);
                ctx.Response.Body = originalBody;

                await ctx.Response.WriteAsync("Success");
                return;
            }
            catch (Exception exception)
            {
                ctx.Response.StatusCode = 500;
                await ctx.Response.WriteAsync(exception.ToString());
            }
            await ctx.Response.WriteAsync("_Failure");
        }

        private async Task FeatureCollectionSetConnectionFeatures(HttpContext ctx)
        {
            try
            {
                Assert.True(IPAddress.IsLoopback(ctx.Connection.LocalIpAddress));
                ctx.Connection.LocalIpAddress = IPAddress.IPv6Any;
                Assert.Equal(IPAddress.IPv6Any, ctx.Connection.LocalIpAddress);

                Assert.True(IPAddress.IsLoopback(ctx.Connection.RemoteIpAddress));
                ctx.Connection.RemoteIpAddress = IPAddress.IPv6Any;
                Assert.Equal(IPAddress.IPv6Any, ctx.Connection.RemoteIpAddress);
                await ctx.Response.WriteAsync("Success");
                return;
            }
            catch (Exception exception)
            {
                ctx.Response.StatusCode = 500;
                await ctx.Response.WriteAsync(exception.ToString());
            }
            await ctx.Response.WriteAsync("_Failure");
        }

        private void Throw(HttpContext ctx)
        {
            throw new Exception();
        }

        private async Task SetCustomErorCode(HttpContext ctx)
        {
            var feature = ctx.Features.Get<IHttpResponseFeature>();
            feature.ReasonPhrase = ctx.Request.Query["reason"];
            feature.StatusCode = int.Parse(ctx.Request.Query["code"]);
            if (ctx.Request.Query["writeBody"] == "True")
            {
                await ctx.Response.WriteAsync(ctx.Request.Query["body"]);
            }
        }

        private async Task HelloWorld(HttpContext ctx)
        {
            if (ctx.Request.Path.Value.StartsWith("/Path"))
            {
                await ctx.Response.WriteAsync(ctx.Request.Path.Value);
                return;
            }
            if (ctx.Request.Path.Value.StartsWith("/Query"))
            {
                await ctx.Response.WriteAsync(ctx.Request.QueryString.Value);
                return;
            }

            await ctx.Response.WriteAsync("Hello World");
        }

        private async Task LargeResponseBody(HttpContext ctx)
        {
            if (int.TryParse(ctx.Request.Query["length"], out var length))
            {
                await ctx.Response.WriteAsync(new string('a', length));
            }
        }

#if !FORWARDCOMPAT
        private Task UnflushedResponsePipe(HttpContext ctx)
        {
            var writer = ctx.Response.BodyWriter;
            var memory = writer.GetMemory(10);
            Assert.True(10 <= memory.Length);
            writer.Advance(10);
            return Task.CompletedTask;
        }

        private async Task FlushedPipeAndThenUnflushedPipe(HttpContext ctx)
        {
            var writer = ctx.Response.BodyWriter;
            var memory = writer.GetMemory(10);
            Assert.True(10 <= memory.Length);
            writer.Advance(10);
            await writer.FlushAsync();
            memory = writer.GetMemory(10);
            Assert.True(10 <= memory.Length);
            writer.Advance(10);
        }
#endif
        private async Task ResponseHeaders(HttpContext ctx)
        {
            ctx.Response.Headers["UnknownHeader"] = "test123=foo";
            ctx.Response.ContentType = "text/plain";
            ctx.Response.Headers["MultiHeader"] = new StringValues(new string[] { "1", "2" });
            await ctx.Response.WriteAsync("Request Complete");
        }

        private async Task ResponseEmptyHeaders(HttpContext ctx)
        {
            ctx.Response.Headers["EmptyHeader"] = "";
            await ctx.Response.WriteAsync("EmptyHeaderShouldBeSkipped");
        }

        private async Task ResponseInvalidOrdering(HttpContext ctx)
        {
            if (ctx.Request.Path.Equals("/SetStatusCodeAfterWrite"))
            {
                await ctx.Response.WriteAsync("Started_");
                try
                {
                    ctx.Response.StatusCode = 200;
                }
                catch (InvalidOperationException)
                {
                    await ctx.Response.WriteAsync("SetStatusCodeAfterWriteThrew_");
                }
                await ctx.Response.WriteAsync("Finished");
                return;
            }
            else if (ctx.Request.Path.Equals("/SetHeaderAfterWrite"))
            {
                await ctx.Response.WriteAsync("Started_");
                try
                {
                    ctx.Response.Headers["This will fail"] = "some value";
                }
                catch (InvalidOperationException)
                {
                    await ctx.Response.WriteAsync("SetHeaderAfterWriteThrew_");
                }
                await ctx.Response.WriteAsync("Finished");
                return;
            }
        }

        private async Task ReadAndWriteSynchronously(HttpContext ctx)
        {
            var t2 = Task.Run(() => WriteManyTimesToResponseBody(ctx));
            var t1 = Task.Run(() => ReadRequestBody(ctx));
            await Task.WhenAll(t1, t2);
        }

        private async Task ReadRequestBody(HttpContext ctx)
        {
            var readBuffer = new byte[1];
            var result = await ctx.Request.Body.ReadAsync(readBuffer, 0, 1);
            while (result != 0)
            {
                result = await ctx.Request.Body.ReadAsync(readBuffer, 0, 1);
            }
        }

        private int _requestsInFlight = 0;
        private async Task ReadAndCountRequestBody(HttpContext ctx)
        {
            Interlocked.Increment(ref _requestsInFlight);
            await ctx.Response.WriteAsync(_requestsInFlight.ToString());

            var readBuffer = new byte[1];
            await ctx.Request.Body.ReadAsync(readBuffer, 0, 1);

            await ctx.Response.WriteAsync("done");
            Interlocked.Decrement(ref _requestsInFlight);
        }

        private async Task WaitForAppToStartShuttingDown(HttpContext ctx)
        {
            await ctx.Response.WriteAsync("test1");
#if FORWARDCOMPAT
            var lifetime = ctx.RequestServices.GetService<Microsoft.AspNetCore.Hosting.IApplicationLifetime>();
#else
            var lifetime = ctx.RequestServices.GetService<IHostApplicationLifetime>();
#endif
            lifetime.ApplicationStopping.WaitHandle.WaitOne();
            await ctx.Response.WriteAsync("test2");
        }

        private async Task ReadFullBody(HttpContext ctx)
        {
            await ReadRequestBody(ctx);
            ctx.Response.ContentLength = 9;
            await ctx.Response.WriteAsync("Completed");
        }

        private async Task WriteManyTimesToResponseBody(HttpContext ctx)
        {
            for (var i = 0; i < 10000; i++)
            {
                await ctx.Response.WriteAsync("hello world");
            }
        }

        private async Task ReadAndWriteEcho(HttpContext ctx)
        {
            var readBuffer = new byte[4096];
            var result = await ctx.Request.Body.ReadAsync(readBuffer, 0, readBuffer.Length);
            while (result != 0)
            {
                await ctx.Response.WriteAsync(Encoding.UTF8.GetString(readBuffer, 0, result));
                result = await ctx.Request.Body.ReadAsync(readBuffer, 0, readBuffer.Length);
            }
        }
        private async Task ReadAndFlushEcho(HttpContext ctx)
        {
            var readBuffer = new byte[4096];
            var result = await ctx.Request.Body.ReadAsync(readBuffer, 0, readBuffer.Length);
            while (result != 0)
            {
                await ctx.Response.WriteAsync(Encoding.UTF8.GetString(readBuffer, 0, result));
                await ctx.Response.Body.FlushAsync();
                result = await ctx.Request.Body.ReadAsync(readBuffer, 0, readBuffer.Length);
            }
        }

        private async Task ReadAndWriteEchoLines(HttpContext ctx)
        {
            if (ctx.Request.Headers.TryGetValue("Response-Content-Type", out var contentType))
            {
                ctx.Response.ContentType = contentType;
            }

            //Send headers
            await ctx.Response.Body.FlushAsync();

            var reader = new StreamReader(ctx.Request.Body);
            while (true)
            {
                var line = await reader.ReadLineAsync();
                if (line == null)
                {
                    return;
                }
                await ctx.Response.WriteAsync(line + Environment.NewLine);
                await ctx.Response.Body.FlushAsync();
            }
        }

        private async Task ReadAndWriteEchoLinesNoBuffering(HttpContext ctx)
        {
#if FORWARDCOMPAT
            var feature = ctx.Features.Get<IHttpBufferingFeature>();
            feature.DisableResponseBuffering();
#else
            var feature = ctx.Features.Get<IHttpResponseBodyFeature>();
            feature.DisableBuffering();
#endif

            if (ctx.Request.Headers.TryGetValue("Response-Content-Type", out var contentType))
            {
                ctx.Response.ContentType = contentType;
            }

            //Send headers
            await ctx.Response.Body.FlushAsync();

            var reader = new StreamReader(ctx.Request.Body);
            while (true)
            {
                var line = await reader.ReadLineAsync();
                if (line == null)
                {
                    return;
                }
                await ctx.Response.WriteAsync(line + Environment.NewLine);
            }
        }

        private async Task ReadPartialBody(HttpContext ctx)
        {
            var data = new byte[5];
            var count = 0;
            do
            {
                count += await ctx.Request.Body.ReadAsync(data, count, data.Length - count);
            } while (count != data.Length);
            await ctx.Response.Body.WriteAsync(data, 0, data.Length);
        }

        private async Task SetHeaderFromBody(HttpContext ctx)
        {
            using (var reader = new StreamReader(ctx.Request.Body))
            {
                var value = await reader.ReadToEndAsync();
                ctx.Response.Headers["BodyAsString"] = value;
                await ctx.Response.WriteAsync(value);
            }
        }

        private async Task ReadAndWriteEchoTwice(HttpContext ctx)
        {
            var readBuffer = new byte[4096];
            var result = await ctx.Request.Body.ReadAsync(readBuffer, 0, readBuffer.Length);
            while (result != 0)
            {
                await ctx.Response.WriteAsync(Encoding.UTF8.GetString(readBuffer, 0, result));
                await ctx.Response.Body.FlushAsync();
                await ctx.Response.WriteAsync(Encoding.UTF8.GetString(readBuffer, 0, result));
                await ctx.Response.Body.FlushAsync();
                result = await ctx.Request.Body.ReadAsync(readBuffer, 0, readBuffer.Length);
            }
        }

        private async Task ReadAndWriteSlowConnection(HttpContext ctx)
        {
            var t2 = Task.Run(() => WriteResponseBodyAFewTimes(ctx));
            var t1 = Task.Run(() => ReadRequestBody(ctx));
            await Task.WhenAll(t1, t2);
        }

        private async Task WriteResponseBodyAFewTimes(HttpContext ctx)
        {
            for (var i = 0; i < 100; i++)
            {
                await ctx.Response.WriteAsync("hello world");
            }
        }

        private async Task ReadAndWriteCopyToAsync(HttpContext ctx)
        {
            await ctx.Request.Body.CopyToAsync(ctx.Response.Body);
        }

        private async Task TestReadOffsetWorks(HttpContext ctx)
        {
            var buffer = new byte[11];
            await ctx.Request.Body.ReadAsync(buffer, 0, 6);
            await ctx.Request.Body.ReadAsync(buffer, 6, 5);

            await ctx.Response.WriteAsync(Encoding.UTF8.GetString(buffer));
        }

        private async Task TestInvalidReadOperations(HttpContext ctx)
        {
            var success = false;
            if (ctx.Request.Path.StartsWithSegments("/NullBuffer"))
            {
                try
                {
                    await ctx.Request.Body.ReadAsync(null, 0, 0);
                }
                catch (Exception)
                {
                    success = true;
                }
            }
            else if (ctx.Request.Path.StartsWithSegments("/InvalidOffsetSmall"))
            {
                try
                {
                    await ctx.Request.Body.ReadAsync(new byte[1], -1, 0);
                }
                catch (ArgumentOutOfRangeException)
                {
                    success = true;
                }
            }
            else if (ctx.Request.Path.StartsWithSegments("/InvalidOffsetLarge"))
            {
                try
                {
                    await ctx.Request.Body.ReadAsync(new byte[1], 2, 0);
                }
                catch (ArgumentOutOfRangeException)
                {
                    success = true;
                }
            }
            else if (ctx.Request.Path.StartsWithSegments("/InvalidCountSmall"))
            {
                try
                {
                    await ctx.Request.Body.ReadAsync(new byte[1], 0, -1);
                }
                catch (ArgumentOutOfRangeException)
                {
                    success = true;
                }
            }
            else if (ctx.Request.Path.StartsWithSegments("/InvalidCountLarge"))
            {
                try
                {
                    await ctx.Request.Body.ReadAsync(new byte[1], 0, -1);
                }
                catch (ArgumentOutOfRangeException)
                {
                    success = true;
                }
            }
            else if (ctx.Request.Path.StartsWithSegments("/InvalidCountWithOffset"))
            {
                try
                {
                    await ctx.Request.Body.ReadAsync(new byte[3], 1, 3);
                }
                catch (ArgumentOutOfRangeException)
                {
                    success = true;
                }
            }


            await ctx.Response.WriteAsync(success ? "Success" : "Failure");
        }

        private async Task TestValidReadOperations(HttpContext ctx)
        {
            var count = -1;

            if (ctx.Request.Path.StartsWithSegments("/NullBuffer"))
            {
                count = await ctx.Request.Body.ReadAsync(null, 0, 0);
            }
            else if (ctx.Request.Path.StartsWithSegments("/NullBufferPost"))
            {
                count = await ctx.Request.Body.ReadAsync(null, 0, 0);
            }
            else if (ctx.Request.Path.StartsWithSegments("/InvalidCountZeroRead"))
            {
                count = await ctx.Request.Body.ReadAsync(new byte[1], 0, 0);
            }
            else if (ctx.Request.Path.StartsWithSegments("/InvalidCountZeroReadPost"))
            {
                count = await ctx.Request.Body.ReadAsync(new byte[1], 0, 0);
            }

            await ctx.Response.WriteAsync(count == 0 ? "Success" : "Failure");
        }

        private async Task TestInvalidWriteOperations(HttpContext ctx)
        {
            var success = false;

            if (ctx.Request.Path.StartsWithSegments("/InvalidOffsetSmall"))
            {
                try
                {
                    await ctx.Response.Body.WriteAsync(new byte[1], -1, 0);
                }
                catch (ArgumentOutOfRangeException)
                {
                    success = true;
                }
            }
            else if (ctx.Request.Path.StartsWithSegments("/InvalidOffsetLarge"))
            {
                try
                {
                    await ctx.Response.Body.WriteAsync(new byte[1], 2, 0);
                }
                catch (ArgumentOutOfRangeException)
                {
                    success = true;
                }
            }
            else if (ctx.Request.Path.StartsWithSegments("/InvalidCountSmall"))
            {
                try
                {
                    await ctx.Response.Body.WriteAsync(new byte[1], 0, -1);
                }
                catch (ArgumentOutOfRangeException)
                {
                    success = true;
                }
            }
            else if (ctx.Request.Path.StartsWithSegments("/InvalidCountLarge"))
            {
                try
                {
                    await ctx.Response.Body.WriteAsync(new byte[1], 0, -1);
                }
                catch (ArgumentOutOfRangeException)
                {
                    success = true;
                }
            }
            else if (ctx.Request.Path.StartsWithSegments("/InvalidCountWithOffset"))
            {
                try
                {
                    await ctx.Response.Body.WriteAsync(new byte[3], 1, 3);
                }
                catch (ArgumentOutOfRangeException)
                {
                    success = true;
                }
            }

            await ctx.Response.WriteAsync(success ? "Success" : "Failure");
        }

        private async Task TestValidWriteOperations(HttpContext ctx)
        {
            if (ctx.Request.Path.StartsWithSegments("/NullBuffer"))
            {
                await ctx.Response.Body.WriteAsync(null, 0, 0);
            }
            else if (ctx.Request.Path.StartsWithSegments("/NullBufferPost"))
            {
                await ctx.Response.Body.WriteAsync(null, 0, 0);
            }

            await ctx.Response.WriteAsync("Success");
        }

        private async Task LargeResponseFile(HttpContext ctx)
        {
            var tempFile = System.IO.Path.GetTempFileName();
            var fileContent = new string('a', 200000);
            var fileStream = File.OpenWrite(tempFile);

            for (var i = 0; i < 1000; i++)
            {
                await fileStream.WriteAsync(Encoding.UTF8.GetBytes(fileContent), 0, fileContent.Length);
            }
            fileStream.Close();

            await ctx.Response.SendFileAsync(tempFile, 0, null);

            // Try to delete the file from the temp directory. If it fails, don't report an error
            // to the application. File should eventually be cleaned up from the temp directory
            // by OS.
            try
            {
                File.Delete(tempFile);
            }
            catch (Exception)
            {
            }
        }

        private async Task BasePath(HttpContext ctx)
        {
            await ctx.Response.WriteAsync(AppDomain.CurrentDomain.BaseDirectory);
        }

        private Task RequestPath(HttpContext ctx)
        {
            ctx.Request.Headers.ContentLength = ctx.Request.Path.Value.Length;
            return ctx.Response.WriteAsync(ctx.Request.Path.Value);
        }

        private async Task Shutdown(HttpContext ctx)
        {
            await ctx.Response.WriteAsync("Shutting down");
#if FORWARDCOMPAT
            ctx.RequestServices.GetService<Microsoft.AspNetCore.Hosting.IApplicationLifetime>().StopApplication();
#else
            ctx.RequestServices.GetService<IHostApplicationLifetime>().StopApplication();
#endif
        }

        private async Task ShutdownStopAsync(HttpContext ctx)
        {
            await ctx.Response.WriteAsync("Shutting down");
            var server = ctx.RequestServices.GetService<IServer>();
            await server.StopAsync(default);
        }

        private async Task ShutdownStopAsyncWithCancelledToken(HttpContext ctx)
        {
            await ctx.Response.WriteAsync("Shutting down");
            var server = ctx.RequestServices.GetService<IServer>();
            var cts = new CancellationTokenSource();
            cts.Cancel();
            await server.StopAsync(cts.Token);
        }

        private async Task StackSize(HttpContext ctx)
        {
            // This would normally stackoverflow if we didn't increase the stack size per thread.
            RecursiveFunction(10000);
            await ctx.Response.WriteAsync("Hello World");
        }

        private async Task StackSizeLarge(HttpContext ctx)
        {
            // This would normally stackoverflow if we didn't increase the stack size per thread.
            RecursiveFunction(30000);
            await ctx.Response.WriteAsync("Hello World");
        }

        private void RecursiveFunction(int i)
        {
            if (i == 0)
            {
                return;
            }
            RecursiveFunction(i - 1);
        }

        private async Task StartupHook(HttpContext ctx)
        {
            await ctx.Response.WriteAsync(StartupHookCalled.ToString());
        }

        private async Task GetServerVariableStress(HttpContext ctx)
        {
            // This test simulates the scenario where native Flush call is being
            // executed on background thread while request thread calls GetServerVariable
            // concurrent native calls may cause native object corruption
            var serverVariableFeature = ctx.Features.Get<IServerVariablesFeature>();

            await ctx.Response.WriteAsync("Response Begin");
            for (int i = 0; i < 1000; i++)
            {
                await ctx.Response.WriteAsync(serverVariableFeature["REMOTE_PORT"]);
                await ctx.Response.Body.FlushAsync();
            }
            await ctx.Response.WriteAsync("Response End");
        }

        private async Task CommandLineArgs(HttpContext ctx)
        {
            await ctx.Response.WriteAsync(string.Join("|", Environment.GetCommandLineArgs().Skip(1)));
        }

        public Task HttpsHelloWorld(HttpContext ctx) =>
           ctx.Response.WriteAsync("Scheme:" + ctx.Request.Scheme + "; Original:" + ctx.Request.Headers["x-original-proto"]);

                   public Task Path(HttpContext ctx) => ctx.Response.WriteAsync(ctx.Request.Path.Value);

        public Task Query(HttpContext ctx) => ctx.Response.WriteAsync(ctx.Request.QueryString.Value);

        public Task BodyLimit(HttpContext ctx) => ctx.Response.WriteAsync(ctx.Features.Get<IHttpMaxRequestBodySizeFeature>()?.MaxRequestBodySize?.ToString() ?? "null");

        public Task Anonymous(HttpContext context) => context.Response.WriteAsync("Anonymous?" + !context.User.Identity.IsAuthenticated);

        public Task Restricted(HttpContext context)
        {
            if (context.User.Identity.IsAuthenticated)
            {
                Assert.IsType<WindowsPrincipal>(context.User);
                return context.Response.WriteAsync(context.User.Identity.AuthenticationType);
            }
            else
            {
                return context.ChallengeAsync(IISDefaults.AuthenticationScheme);
            }
        }

        public Task Forbidden(HttpContext context) => context.ForbidAsync(IISDefaults.AuthenticationScheme);

        public Task RestrictedNTLM(HttpContext context)
        {
            if (string.Equals("NTLM", context.User.Identity.AuthenticationType, StringComparison.Ordinal))
            {
                return context.Response.WriteAsync("NTLM");
            }
            else
            {
                return context.ChallengeAsync(IISDefaults.AuthenticationScheme);
            }
        }

        public Task UpgradeFeatureDetection(HttpContext context) =>
            context.Response.WriteAsync(context.Features.Get<IHttpUpgradeFeature>() != null? "Enabled": "Disabled");

        public Task CheckRequestHandlerVersion(HttpContext context)
        {
            // We need to check if the aspnetcorev2_outofprocess dll is loaded by iisexpress.exe
            // As they aren't in the same process, we will try to delete the file and expect a file
            // in use error
            try
            {
                File.Delete(context.Request.Headers["ANCMRHPath"]);
            }
            catch(UnauthorizedAccessException)
            {
                // TODO calling delete on the file will succeed when running with IIS
                return context.Response.WriteAsync("Hello World");
            }

            return context.Response.WriteAsync(context.Request.Headers["ANCMRHPath"]);
        }

        private async Task ProcessId(HttpContext context)
        {
            await context.Response.WriteAsync(Process.GetCurrentProcess().Id.ToString());
        }

        public async Task ANCM_HTTPS_PORT(HttpContext context)
        {
            var httpsPort = context.RequestServices.GetService<IConfiguration>().GetValue<int?>("ANCM_HTTPS_PORT");

            await context.Response.WriteAsync(httpsPort.HasValue ? httpsPort.Value.ToString() : "NOVALUE");
        }

        public async Task HTTPS_PORT(HttpContext context)
        {
            var httpsPort = context.RequestServices.GetService<IConfiguration>().GetValue<int?>("HTTPS_PORT");

            await context.Response.WriteAsync(httpsPort.HasValue ? httpsPort.Value.ToString() : "NOVALUE");
        }

        public async Task SlowOnCompleted(HttpContext context)
        {
            // This shouldn't block the response or the server from shutting down.
            context.Response.OnCompleted(() => Task.Delay(TimeSpan.FromMinutes(5)));
            await context.Response.WriteAsync("SlowOnCompleted");
        }
    }
}
