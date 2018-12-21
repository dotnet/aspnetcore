// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.IISIntegration.FunctionalTests;
using Microsoft.AspNetCore.Server.IIS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace TestSite
{
    public partial class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            TestStartup.Register(app, this);
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

        private async Task ResponseHeaders(HttpContext ctx)
        {
            ctx.Response.Headers["UnknownHeader"] = "test123=foo";
            ctx.Response.ContentType = "text/plain";
            ctx.Response.Headers["MultiHeader"] = new StringValues(new string[] { "1", "2" });
            await ctx.Response.WriteAsync("Request Complete");
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
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (line == "")
                {
                    return;
                }
                await ctx.Response.WriteAsync(line + Environment.NewLine);
                await ctx.Response.Body.FlushAsync();
            }
        }

        private async Task ReadAndWriteEchoLinesNoBuffering(HttpContext ctx)
        {
            var feature = ctx.Features.Get<IHttpBufferingFeature>();
            feature.DisableResponseBuffering();

            if (ctx.Request.Headers.TryGetValue("Response-Content-Type", out var contentType))
            {
                ctx.Response.ContentType = contentType;
            }

            //Send headers
            await ctx.Response.Body.FlushAsync();

            var reader = new StreamReader(ctx.Request.Body);
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (line == "")
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

        private async Task UpgradeFeatureDetection(HttpContext ctx)
        {
            if (ctx.Features.Get<IHttpUpgradeFeature>() != null)
            {
                await ctx.Response.WriteAsync("Enabled");
            }
            else
            {
                await ctx.Response.WriteAsync("Disabled");
            }
        }

        private async Task TestReadOffsetWorks(HttpContext ctx)
        {
            var buffer = new byte[11];
            ctx.Request.Body.Read(buffer, 0, 6);
            ctx.Request.Body.Read(buffer, 6, 5);

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
            var tempFile = Path.GetTempFileName();
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

        private async Task Shutdown(HttpContext ctx)
        {
            await ctx.Response.WriteAsync("Shutting down");
            ctx.RequestServices.GetService<IApplicationLifetime>().StopApplication();
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
    }
}
