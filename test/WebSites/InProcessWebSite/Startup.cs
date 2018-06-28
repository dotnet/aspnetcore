// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
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

namespace IISTestSite
{
    public partial class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            TestStartup.Register(app, this);
        }

        private void ServerVariable(IApplicationBuilder app)
        {
            app.Run(async ctx =>
            {
                var varName = ctx.Request.Query["q"];
                await ctx.Response.WriteAsync($"{varName}: {ctx.GetIISServerVariable(varName) ?? "(null)"}");
            });
        }

        public void AuthenticationAnonymous(IApplicationBuilder app)
        {
            app.Run(async ctx =>
            {
                await ctx.Response.WriteAsync("Anonymous?" + !ctx.User.Identity.IsAuthenticated);
            });
        }

        private void AuthenticationRestricted(IApplicationBuilder app)
        {
            app.Run(async ctx =>
            {
                if (ctx.User.Identity.IsAuthenticated)
                {
                    await ctx.Response.WriteAsync(ctx.User.Identity.AuthenticationType);
                }
                else
                {
                    await ctx.ChallengeAsync(IISServerDefaults.AuthenticationScheme);
                }
            });
        }

        public void AuthenticationForbidden(IApplicationBuilder app)
        {
            app.Run(async ctx =>
            {
                await ctx.ForbidAsync(IISServerDefaults.AuthenticationScheme);
            });
        }

        public void AuthenticationRestrictedNTLM(IApplicationBuilder app)
        {
            app.Run(async ctx =>
            {
                if (string.Equals("NTLM", ctx.User.Identity.AuthenticationType, StringComparison.Ordinal))
                {
                    await ctx.Response.WriteAsync("NTLM");
                }
                else
                {
                    await ctx.ChallengeAsync(IISServerDefaults.AuthenticationScheme);
                }
            });
        }

        private void FeatureCollectionSetRequestFeatures(IApplicationBuilder app)
        {
            app.Run(async context =>
            {
                try
                {
                    Assert.Equal("GET", context.Request.Method);
                    context.Request.Method = "test";
                    Assert.Equal("test", context.Request.Method);

                    Assert.Equal("http", context.Request.Scheme);
                    context.Request.Scheme = "test";
                    Assert.Equal("test", context.Request.Scheme);

                    Assert.Equal("/FeatureCollectionSetRequestFeatures", context.Request.PathBase);
                    context.Request.PathBase = "/base";
                    Assert.Equal("/base", context.Request.PathBase);

                    Assert.Equal("/path", context.Request.Path);
                    context.Request.Path = "/path";
                    Assert.Equal("/path", context.Request.Path);

                    Assert.Equal("?query", context.Request.QueryString.Value);
                    context.Request.QueryString = QueryString.Empty;
                    Assert.Equal("", context.Request.QueryString.Value);

                    Assert.Equal("HTTP/1.1", context.Request.Protocol);
                    context.Request.Protocol = "HTTP/1.0";
                    Assert.Equal("HTTP/1.0", context.Request.Protocol);

                    Assert.NotNull(context.Request.Headers);
                    var headers = new HeaderDictionary();
                    context.Features.Get<IHttpRequestFeature>().Headers = headers;
                    Assert.Same(headers, context.Features.Get<IHttpRequestFeature>().Headers);

                    Assert.NotNull(context.Request.Body);
                    var body = new MemoryStream();
                    context.Request.Body = body;
                    Assert.Same(body, context.Request.Body);

                    //Assert.NotNull(context.Features.Get<IHttpRequestIdentifierFeature>().TraceIdentifier);
                    //Assert.NotEqual(CancellationToken.None, context.RequestAborted);
                    //var token = new CancellationTokenSource().Token;
                    //context.RequestAborted = token;
                    //Assert.Equal(token, context.RequestAborted);

                    await context.Response.WriteAsync("Success");
                    return;
                }
                catch (Exception exception)
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync(exception.ToString());
                }
                await context.Response.WriteAsync("_Failure");
            });
        }

        private void FeatureCollectionSetResponseFeatures(IApplicationBuilder app)
        {
            app.Run(async context =>
            {
                try
                {
                    Assert.Equal(200, context.Response.StatusCode);
                    context.Response.StatusCode = 404;
                    Assert.Equal(404, context.Response.StatusCode);
                    context.Response.StatusCode = 200;

                    Assert.Null(context.Features.Get<IHttpResponseFeature>().ReasonPhrase);
                    context.Features.Get<IHttpResponseFeature>().ReasonPhrase = "Set Response";
                    Assert.Equal("Set Response", context.Features.Get<IHttpResponseFeature>().ReasonPhrase);

                    Assert.NotNull(context.Response.Headers);
                    var headers = new HeaderDictionary();
                    context.Features.Get<IHttpResponseFeature>().Headers = headers;
                    Assert.Same(headers, context.Features.Get<IHttpResponseFeature>().Headers);

                    var originalBody = context.Response.Body;
                    Assert.NotNull(originalBody);
                    var body = new MemoryStream();
                    context.Response.Body = body;
                    Assert.Same(body, context.Response.Body);
                    context.Response.Body = originalBody;

                    await context.Response.WriteAsync("Success");
                    return;
                }
                catch (Exception exception)
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync(exception.ToString());
                }
                await context.Response.WriteAsync("_Failure");
            });
        }

        private void FeatureCollectionSetConnectionFeatures(IApplicationBuilder app)
        {
            app.Run(async context =>
            {
                try
                {
                    Assert.True(IPAddress.IsLoopback(context.Connection.LocalIpAddress));
                    context.Connection.LocalIpAddress = IPAddress.IPv6Any;
                    Assert.Equal(IPAddress.IPv6Any, context.Connection.LocalIpAddress);

                    Assert.True(IPAddress.IsLoopback(context.Connection.RemoteIpAddress));
                    context.Connection.RemoteIpAddress = IPAddress.IPv6Any;
                    Assert.Equal(IPAddress.IPv6Any, context.Connection.RemoteIpAddress);
                    await context.Response.WriteAsync("Success");
                    return;
                }
                catch (Exception exception)
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync(exception.ToString());
                }
                await context.Response.WriteAsync("_Failure");
            });
        }

        private void Throw(IApplicationBuilder app)
        {
            app.Run(ctx => { throw new Exception(); });
        }

        private void SetCustomErorCode(IApplicationBuilder app)
        {
            app.Run(async ctx => {
                    var feature = ctx.Features.Get<IHttpResponseFeature>();
                    feature.ReasonPhrase = ctx.Request.Query["reason"];
                    feature.StatusCode = int.Parse(ctx.Request.Query["code"]);
                    if (ctx.Request.Query["writeBody"] == "True")
                    {
                        await ctx.Response.WriteAsync(ctx.Request.Query["body"]);
                    }
                });
        }

        private void HelloWorld(IApplicationBuilder app)
        {
            app.Run(async ctx =>
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
            });
        }

        private void LargeResponseBody(IApplicationBuilder app)
        {
            app.Run(async context =>
            {
                if (int.TryParse(context.Request.Query["length"], out var length))
                {
                    await context.Response.WriteAsync(new string('a', length));
                }
            });
        }

        private void ResponseHeaders(IApplicationBuilder app)
        {
            app.Run(async context =>
            {
                context.Response.Headers["UnknownHeader"] = "test123=foo";
                context.Response.ContentType = "text/plain";
                context.Response.Headers["MultiHeader"] = new StringValues(new string[] { "1", "2" });
                await context.Response.WriteAsync("Request Complete");
            });
        }

        private void ResponseInvalidOrdering(IApplicationBuilder app)
        {
            app.Run(async context =>
            {
                if (context.Request.Path.Equals("/SetStatusCodeAfterWrite"))
                {
                    await context.Response.WriteAsync("Started_");
                    try
                    {
                        context.Response.StatusCode = 200;
                    }
                    catch (InvalidOperationException)
                    {
                        await context.Response.WriteAsync("SetStatusCodeAfterWriteThrew_");
                    }
                    await context.Response.WriteAsync("Finished");
                    return;
                }
                else if (context.Request.Path.Equals("/SetHeaderAfterWrite"))
                {
                    await context.Response.WriteAsync("Started_");
                    try
                    {
                        context.Response.Headers["This will fail"] = "some value";
                    }
                    catch (InvalidOperationException)
                    {
                        await context.Response.WriteAsync("SetHeaderAfterWriteThrew_");
                    }
                    await context.Response.WriteAsync("Finished");
                    return;
                }
            });
        }

        private void CheckEnvironmentVariable(IApplicationBuilder app)
        {
            app.Run(async context =>
            {
                var variable = Environment.GetEnvironmentVariable("ASPNETCORE_INPROCESS_TESTING_VALUE");
                await context.Response.WriteAsync(variable);
            });
        }

        private void CheckEnvironmentLongValueVariable(IApplicationBuilder app)
        {
            app.Run(async context =>
            {
                var variable = Environment.GetEnvironmentVariable("ASPNETCORE_INPROCESS_TESTING_LONG_VALUE");
                await context.Response.WriteAsync(variable);
            });
        }

        private void CheckAppendedEnvironmentVariable(IApplicationBuilder app)
        {
            app.Run(async context =>
            {
                var variable = Environment.GetEnvironmentVariable("ProgramFiles");
                await context.Response.WriteAsync(variable);
            });
        }

        private void CheckRemoveAuthEnvironmentVariable(IApplicationBuilder app)
        {
            app.Run(async context =>
            {
                var variable = Environment.GetEnvironmentVariable("ASPNETCORE_IIS_HTTPAUTH");
                await context.Response.WriteAsync(variable);
            });
        }
        private void ReadAndWriteSynchronously(IApplicationBuilder app)
        {
            app.Run(async context =>
            {
                var t2 = Task.Run(() => WriteManyTimesToResponseBody(context));
                var t1 = Task.Run(() => ReadRequestBody(context));
                await Task.WhenAll(t1, t2);
            });
        }

        private async Task ReadRequestBody(HttpContext context)
        {
            var readBuffer = new byte[1];
            var result = await context.Request.Body.ReadAsync(readBuffer, 0, 1);
            while (result != 0)
            {
                result = await context.Request.Body.ReadAsync(readBuffer, 0, 1);
            }
        }

        private async Task WriteManyTimesToResponseBody(HttpContext context)
        {
            for (var i = 0; i < 10000; i++)
            {
                await context.Response.WriteAsync("hello world");
            }
        }

        private void ReadAndWriteEcho(IApplicationBuilder app)
        {
            app.Run(async context => {
                var readBuffer = new byte[4096];
                var result = await context.Request.Body.ReadAsync(readBuffer, 0, readBuffer.Length);
                while (result != 0)
                {
                    await context.Response.WriteAsync(Encoding.UTF8.GetString(readBuffer, 0, result));
                    result = await context.Request.Body.ReadAsync(readBuffer, 0, readBuffer.Length);
                }
            });
        }

        private void ReadAndWriteEchoLines(IApplicationBuilder app)
        {
            app.Run(async context => {
                //Send headers
                await context.Response.Body.FlushAsync();

                var reader = new StreamReader(context.Request.Body);
                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    if (line == "")
                    {
                        return;
                    }
                    await context.Response.WriteAsync(line + Environment.NewLine);
                    await context.Response.Body.FlushAsync();
                }
            });
        }

        private void ReadPartialBody(IApplicationBuilder app)
        {
            app.Run(async context => {
                var data = new byte[5];
                var count = 0;
                do
                {
                    count += await context.Request.Body.ReadAsync(data, count, data.Length - count);
                } while (count != data.Length);
                await context.Response.Body.WriteAsync(data, 0, data.Length);
            });
        }

        private void SetHeaderFromBody(IApplicationBuilder app)
        {
            app.Run(async context => {
                using (var reader = new StreamReader(context.Request.Body))
                {
                    var value = await reader.ReadToEndAsync();
                    context.Response.Headers["BodyAsString"] = value;
                    await context.Response.WriteAsync(value);
                }
            });
        }

        private void ReadAndWriteEchoTwice(IApplicationBuilder app)
        {
            app.Run(async context =>
            {
                var readBuffer = new byte[4096];
                var result = await context.Request.Body.ReadAsync(readBuffer, 0, readBuffer.Length);
                while (result != 0)
                {
                    await context.Response.WriteAsync(Encoding.UTF8.GetString(readBuffer, 0, result));
                    await context.Response.Body.FlushAsync();
                    await context.Response.WriteAsync(Encoding.UTF8.GetString(readBuffer, 0, result));
                    await context.Response.Body.FlushAsync();
                    result = await context.Request.Body.ReadAsync(readBuffer, 0, readBuffer.Length);
                }
            });
        }

        private void ReadAndWriteSlowConnection(IApplicationBuilder app)
        {
            app.Run(async context =>
            {
                var t2 = Task.Run(() => WriteResponseBodyAFewTimes(context));
                var t1 = Task.Run(() => ReadRequestBody(context));
                await Task.WhenAll(t1, t2);
            });
        }

        private async Task WriteResponseBodyAFewTimes(HttpContext context)
        {
            for (var i = 0; i < 100; i++)
            {
                await context.Response.WriteAsync("hello world");
            }
        }

        private void ReadAndWriteCopyToAsync(IApplicationBuilder app)
        {
            app.Run(async context =>
            {
                await context.Request.Body.CopyToAsync(context.Response.Body);
            });
        }

        private void UpgradeFeatureDetection(IApplicationBuilder app)
        {
            app.Run(async ctx =>
            {
                if (ctx.Features.Get<IHttpUpgradeFeature>() != null)
                {
                    await ctx.Response.WriteAsync("Enabled");
                }
                else
                {
                    await ctx.Response.WriteAsync("Disabled");
                }
            });
        }

        private void TestReadOffsetWorks(IApplicationBuilder app)
        {
            app.Run(async ctx =>
            {
                var buffer = new byte[11];
                ctx.Request.Body.Read(buffer, 0, 6);
                ctx.Request.Body.Read(buffer, 6, 5);

                await ctx.Response.WriteAsync(Encoding.UTF8.GetString(buffer));
            });
        }

        private void TestInvalidReadOperations(IApplicationBuilder app)
        {
            app.Run(async context =>
            {
                var success = false;
                if (context.Request.Path.StartsWithSegments("/NullBuffer"))
                {
                    try
                    {
                        await context.Request.Body.ReadAsync(null, 0, 0);
                    }
                    catch (Exception)
                    {
                        success = true;
                    }
                }
                else if (context.Request.Path.StartsWithSegments("/InvalidOffsetSmall"))
                {
                    try
                    {
                        await context.Request.Body.ReadAsync(new byte[1], -1, 0);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        success = true;
                    }
                }
                else if (context.Request.Path.StartsWithSegments("/InvalidOffsetLarge"))
                {
                    try
                    {
                        await context.Request.Body.ReadAsync(new byte[1], 2, 0);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        success = true;
                    }
                }
                else if (context.Request.Path.StartsWithSegments("/InvalidCountSmall"))
                {
                    try
                    {
                        await context.Request.Body.ReadAsync(new byte[1], 0, -1);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        success = true;
                    }
                }
                else if (context.Request.Path.StartsWithSegments("/InvalidCountLarge"))
                {
                    try
                    {
                        await context.Request.Body.ReadAsync(new byte[1], 0, -1);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        success = true;
                    }
                }
                else if (context.Request.Path.StartsWithSegments("/InvalidCountWithOffset"))
                {
                    try
                    {
                        await context.Request.Body.ReadAsync(new byte[3], 1, 3);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        success = true;
                    }
                }


                await context.Response.WriteAsync(success ? "Success" : "Failure");
            });
        }

        private void TestValidReadOperations(IApplicationBuilder app)
        {
            app.Run(async context =>
            {
                var count = -1;

                if (context.Request.Path.StartsWithSegments("/NullBuffer"))
                {
                    count = await context.Request.Body.ReadAsync(null, 0, 0);
                }
                else if (context.Request.Path.StartsWithSegments("/NullBufferPost"))
                {
                    count = await context.Request.Body.ReadAsync(null, 0, 0);
                }
                else if (context.Request.Path.StartsWithSegments("/InvalidCountZeroRead"))
                {
                    count = await context.Request.Body.ReadAsync(new byte[1], 0, 0);
                }
                else if (context.Request.Path.StartsWithSegments("/InvalidCountZeroReadPost"))
                {
                    count = await context.Request.Body.ReadAsync(new byte[1], 0, 0);
                }

                await context.Response.WriteAsync(count == 0 ? "Success" : "Failure");
            });
        }

        private void TestInvalidWriteOperations(IApplicationBuilder app)
        {
            app.Run(async context =>
            {
                var success = false;

                if (context.Request.Path.StartsWithSegments("/InvalidOffsetSmall"))
                {
                    try
                    {
                        await context.Response.Body.WriteAsync(new byte[1], -1, 0);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        success = true;
                    }
                }
                else if (context.Request.Path.StartsWithSegments("/InvalidOffsetLarge"))
                {
                    try
                    {
                        await context.Response.Body.WriteAsync(new byte[1], 2, 0);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        success = true;
                    }
                }
                else if (context.Request.Path.StartsWithSegments("/InvalidCountSmall"))
                {
                    try
                    {
                        await context.Response.Body.WriteAsync(new byte[1], 0, -1);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        success = true;
                    }
                }
                else if (context.Request.Path.StartsWithSegments("/InvalidCountLarge"))
                {
                    try
                    {
                        await context.Response.Body.WriteAsync(new byte[1], 0, -1);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        success = true;
                    }
                }
                else if (context.Request.Path.StartsWithSegments("/InvalidCountWithOffset"))
                {
                    try
                    {
                        await context.Response.Body.WriteAsync(new byte[3], 1, 3);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        success = true;
                    }
                }

                await context.Response.WriteAsync(success ? "Success" : "Failure");
            });
        }

        private void TestValidWriteOperations(IApplicationBuilder app)
        {
            app.Run(async context =>
            {
                if (context.Request.Path.StartsWithSegments("/NullBuffer"))
                {
                    await context.Response.Body.WriteAsync(null, 0, 0);
                }
                else if (context.Request.Path.StartsWithSegments("/NullBufferPost"))
                {
                    await context.Response.Body.WriteAsync(null, 0, 0);
                }

                await context.Response.WriteAsync("Success");
            });
        }

        private void LargeResponseFile(IApplicationBuilder app)
        {
            app.Run(async ctx =>
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
            });
        }

        private void BasePath(IApplicationBuilder app)
        {
            app.Run(async ctx => { await ctx.Response.WriteAsync(AppDomain.CurrentDomain.BaseDirectory); });
        }

        private void CheckLogFile(IApplicationBuilder app)
        {
            app.Run(async ctx =>
            {
                Console.WriteLine("TEST MESSAGE");

                await ctx.Response.WriteAsync("Hello World");
            });
        }

        private void CheckErrLogFile(IApplicationBuilder app)
        {
            app.Run(async ctx =>
            {
                Console.Error.WriteLine("TEST MESSAGE");
                Console.Error.Flush();

                await ctx.Response.WriteAsync("Hello World");
            });
        }

        private void Shutdown(IApplicationBuilder app)
        {
            app.Run(async ctx =>
            {
                await ctx.Response.WriteAsync("Shutting down");
                ctx.RequestServices.GetService<IApplicationLifetime>().StopApplication();
            });
        }
    }
}
