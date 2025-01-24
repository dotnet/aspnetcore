// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
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
using Microsoft.Net.Http.Headers;
using Xunit;

namespace TestSite;

public partial class Startup
{
    public static bool StartupHookCalled;
    private IHttpContextAccessor _httpContextAccessor;

    public void Configure(IApplicationBuilder app, IHttpContextAccessor httpContextAccessor)
    {
        if (Environment.GetEnvironmentVariable("ENABLE_HTTPS_REDIRECTION") != null)
        {
            app.UseHttpsRedirection();
        }

#if !FORWARDCOMPAT
        app.UseWebSockets();
#endif

        TestStartup.Register(app, this);
        _httpContextAccessor = httpContextAccessor;
    }

    public void ConfigureServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddResponseCompression();
        serviceCollection.AddHttpContextAccessor();
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

    private async Task IIISEnvironmentFeatureConfig(HttpContext ctx)
    {
        var config = ctx.RequestServices.GetService<IConfiguration>();

        await ctx.Response.WriteAsync("IIS Version: " + config["IIS_VERSION"] + Environment.NewLine);
        await ctx.Response.WriteAsync("ApplicationId: " + config["IIS_APPLICATION_ID"] + Environment.NewLine);
        await ctx.Response.WriteAsync("Application Path: " + config["IIS_PHYSICAL_PATH"] + Environment.NewLine);
        await ctx.Response.WriteAsync("Application Virtual Path: " + config["IIS_APPLICATION_VIRTUAL_PATH"] + Environment.NewLine);
        await ctx.Response.WriteAsync("Application Config Path: " + config["IIS_APP_CONFIG_PATH"] + Environment.NewLine);
        await ctx.Response.WriteAsync("AppPool ID: " + config["IIS_APP_POOL_ID"] + Environment.NewLine);
        await ctx.Response.WriteAsync("AppPool Config File: " + config["IIS_APP_POOL_CONFIG_FILE"] + Environment.NewLine);
        await ctx.Response.WriteAsync("Site ID: " + config["IIS_SITE_ID"] + Environment.NewLine);
        await ctx.Response.WriteAsync("Site Name: " + config["IIS_SITE_NAME"]);
    }

#if !FORWARDCOMPAT
    private async Task IIISEnvironmentFeature(HttpContext ctx)
    {
        var envFeature = ctx.RequestServices.GetService<IServer>().Features.Get<IIISEnvironmentFeature>();

        await ctx.Response.WriteAsync("IIS Version: " + envFeature.IISVersion + Environment.NewLine);
        await ctx.Response.WriteAsync("ApplicationId: " + envFeature.ApplicationId + Environment.NewLine);
        await ctx.Response.WriteAsync("Application Path: " + envFeature.ApplicationPhysicalPath + Environment.NewLine);
        await ctx.Response.WriteAsync("Application Virtual Path: " + envFeature.ApplicationVirtualPath + Environment.NewLine);
        await ctx.Response.WriteAsync("Application Config Path: " + envFeature.AppConfigPath + Environment.NewLine);
        await ctx.Response.WriteAsync("AppPool ID: " + envFeature.AppPoolId + Environment.NewLine);
        await ctx.Response.WriteAsync("AppPool Config File: " + envFeature.AppPoolConfigFile + Environment.NewLine);
        await ctx.Response.WriteAsync("Site ID: " + envFeature.SiteId + Environment.NewLine);
        await ctx.Response.WriteAsync("Site Name: " + envFeature.SiteName);
    }
#endif

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
        await context.Response.WriteAsync(_waitingRequestCount.ToString(CultureInfo.InvariantCulture));
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

    private Task PathAndPathBase(HttpContext ctx)
    {
        return ctx.Response.WriteAsync($"PathBase: {ctx.Request.PathBase.Value}; Path: {ctx.Request.Path.Value}");
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
        feature.StatusCode = int.Parse(ctx.Request.Query["code"], CultureInfo.InvariantCulture);
        if (ctx.Request.Query["writeBody"] == "True")
        {
            await ctx.Response.WriteAsync(ctx.Request.Query["body"]);
        }
    }

    private async Task HelloWorld(HttpContext ctx)
    {
        if (ctx.Request.Path.Value.StartsWith("/Path", StringComparison.Ordinal))
        {
            await ctx.Response.WriteAsync(ctx.Request.Path.Value);
            return;
        }
        if (ctx.Request.Path.Value.StartsWith("/Query", StringComparison.Ordinal))
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

    private Task TestRequestHeaders(HttpContext ctx)
    {
        // Test optimized and non-optimized headers behave equivalently
        foreach (var headerName in new[] { "custom", "Content-Type" })
        {
            // StringValues.Empty.Equals(default(StringValues)), so we check if the implicit conversion
            // to string[] returns null or Array.Empty<string>() to tell the difference.
            if ((string[])ctx.Request.Headers[headerName] != Array.Empty<string>())
            {
                return ctx.Response.WriteAsync($"Failure: '{headerName}' indexer");
            }
            if (ctx.Request.Headers.TryGetValue(headerName, out var headerValue) || (string[])headerValue is not null)
            {
                return ctx.Response.WriteAsync($"Failure: '{headerName}' TryGetValue");
            }

            // Both default and StringValues.Empty should unset the header, allowing it to be added again.
            ArgumentException duplicateKeyException = null;
            ctx.Request.Headers.Add(headerName, "test");
            ctx.Request.Headers[headerName] = default;
            ctx.Request.Headers.Add(headerName, "test");
            ctx.Request.Headers[headerName] = StringValues.Empty;
            ctx.Request.Headers.Add(headerName, "test");

            try
            {
                // Repeated adds should throw.
                ctx.Request.Headers.Add(headerName, "test");
            }
            catch (ArgumentException ex)
            {
                duplicateKeyException = ex;
                ctx.Request.Headers[headerName] = default;
            }

            if (duplicateKeyException is null)
            {
                return ctx.Response.WriteAsync($"Failure: Repeated '{headerName}' Add did not throw");
            }
        }

#if !FORWARDCOMPAT
        if ((string[])ctx.Request.Headers.ContentType != Array.Empty<string>())
        {
            return ctx.Response.WriteAsync("Failure: ContentType property");
        }

        ctx.Request.Headers.ContentType = default;
        if ((string[])ctx.Request.Headers.ContentType != Array.Empty<string>())
        {
            return ctx.Response.WriteAsync("Failure: ContentType property after setting default");
        }
#endif

        return ctx.Response.WriteAsync("Success");
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
#if !FORWARDCOMPAT
        Assert.True(ctx.Request.CanHaveBody());
#endif
        var readBuffer = new byte[1];
        var result = await ctx.Request.Body.ReadAsync(readBuffer, 0, 1);
        while (result != 0)
        {
            result = await ctx.Request.Body.ReadAsync(readBuffer, 0, 1);
        }
    }

    private async Task ReadRequestBodyLarger(HttpContext ctx)
    {
#if !FORWARDCOMPAT
        Assert.True(ctx.Request.CanHaveBody());
#endif
        var readBuffer = new byte[4096];
        var result = await ctx.Request.Body.ReadAsync(readBuffer, 0, 4096);
        while (result != 0)
        {
            result = await ctx.Request.Body.ReadAsync(readBuffer, 0, 4096);
        }
    }

    private int _requestsInFlight = 0;
    private async Task ReadAndCountRequestBody(HttpContext ctx)
    {
        Interlocked.Increment(ref _requestsInFlight);
        await ctx.Response.WriteAsync(_requestsInFlight.ToString(CultureInfo.InvariantCulture));

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
#if !FORWARDCOMPAT
        Assert.True(ctx.Request.CanHaveBody());
#endif
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
#if !FORWARDCOMPAT
        Assert.True(ctx.Request.CanHaveBody());
#endif
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
#if !FORWARDCOMPAT
        Assert.True(ctx.Request.CanHaveBody());
#endif
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
#if !FORWARDCOMPAT
        Assert.True(ctx.Request.CanHaveBody());
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
        Assert.True(ctx.Request.CanHaveBody());
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
#if !FORWARDCOMPAT
        Assert.True(ctx.Request.CanHaveBody());
#endif
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
#if !FORWARDCOMPAT
        Assert.True(ctx.Request.CanHaveBody());
#endif
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

    public Task BodyLimit(HttpContext ctx) => ctx.Response.WriteAsync(ctx.Features.Get<IHttpMaxRequestBodySizeFeature>()?.MaxRequestBodySize?.ToString(CultureInfo.InvariantCulture) ?? "null");

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
        context.Response.WriteAsync(context.Features.Get<IHttpUpgradeFeature>() != null ? "Enabled" : "Disabled");

    public Task CheckRequestHandlerVersion(HttpContext context)
    {
        // We need to check if the aspnetcorev2_outofprocess dll is loaded by iisexpress.exe
        // As they aren't in the same process, we will try to delete the file and expect a file
        // in use error
        try
        {
            File.Delete(context.Request.Headers["ANCMRHPath"]);
        }
        catch (UnauthorizedAccessException)
        {
            // TODO calling delete on the file will succeed when running with IIS
            return context.Response.WriteAsync("Hello World");
        }

        return context.Response.WriteAsync(context.Request.Headers["ANCMRHPath"]);
    }

    private async Task ProcessId(HttpContext context)
    {
        await context.Response.WriteAsync(Environment.ProcessId.ToString(CultureInfo.InvariantCulture));
    }

    public async Task ANCM_HTTPS_PORT(HttpContext context)
    {
        var httpsPort = context.RequestServices.GetService<IConfiguration>().GetValue<int?>("ANCM_HTTPS_PORT");

        await context.Response.WriteAsync(httpsPort.HasValue ? httpsPort.Value.ToString(CultureInfo.InvariantCulture) : "NOVALUE");
    }

    public async Task HTTPS_PORT(HttpContext context)
    {
        var httpsPort = context.RequestServices.GetService<IConfiguration>().GetValue<int?>("HTTPS_PORT");

        await context.Response.WriteAsync(httpsPort.HasValue ? httpsPort.Value.ToString(CultureInfo.InvariantCulture) : "NOVALUE");
    }

    public Task Latin1(HttpContext context)
    {
        var value = context.Request.Headers["foo"];
        Assert.Equal("£", value);
        return Task.CompletedTask;
    }

    public Task InvalidCharacter(HttpContext context)
    {
        var value = context.Request.Headers["foo"];
        Assert.Equal("�", value);
        return Task.CompletedTask;
    }

    private async Task TransferEncodingHeadersWithMultipleValues(HttpContext ctx)
    {
        try
        {
#if !FORWARDCOMPAT
            Assert.True(ctx.Request.CanHaveBody());
#endif
            Assert.True(ctx.Request.Headers.ContainsKey("Transfer-Encoding"));
            Assert.Equal("gzip, chunked", ctx.Request.Headers["Transfer-Encoding"]);
            return;
        }
        catch (Exception exception)
        {
            ctx.Response.StatusCode = 500;
            await ctx.Response.WriteAsync(exception.ToString());
        }
    }

    private async Task TransferEncodingAndContentLengthShouldBeRemove(HttpContext ctx)
    {
        try
        {
#if !FORWARDCOMPAT
            Assert.True(ctx.Request.CanHaveBody());
#endif
            Assert.True(ctx.Request.Headers.ContainsKey("Transfer-Encoding"));
            Assert.Equal("gzip, chunked", ctx.Request.Headers["Transfer-Encoding"]);
            Assert.False(ctx.Request.Headers.ContainsKey("Content-Length"));
            Assert.True(ctx.Request.Headers.ContainsKey("X-Content-Length"));
            Assert.Equal("5", ctx.Request.Headers["X-Content-Length"]);
            return;
        }
        catch (Exception exception)
        {
            ctx.Response.StatusCode = 500;
            await ctx.Response.WriteAsync(exception.ToString());
        }
    }

#if !FORWARDCOMPAT
    public Task ResponseTrailers_HTTP2_TrailersAvailable(HttpContext context)
    {
        Assert.Equal("HTTP/2", context.Request.Protocol);
        Assert.True(context.Response.SupportsTrailers());
        return Task.FromResult(0);
    }

    public Task ResponseTrailers_HTTP1_TrailersNotAvailable(HttpContext context)
    {
        Assert.Equal("HTTP/1.1", context.Request.Protocol);
        Assert.False(context.Response.SupportsTrailers());
        return Task.FromResult(0);
    }

    public Task ResponseTrailers_ProhibitedTrailers_Blocked(HttpContext context)
    {
        Assert.True(context.Response.SupportsTrailers());
        foreach (var header in DisallowedTrailers)
        {
            Assert.Throws<InvalidOperationException>(() => context.Response.AppendTrailer(header, "value"));
        }
        return Task.FromResult(0);
    }

    public Task ResponseTrailers_NoBody_TrailersSent(HttpContext context)
    {
        context.Response.DeclareTrailer("trailername");
        context.Response.AppendTrailer("trailername", "TrailerValue");
        return Task.FromResult(0);
    }

    public async Task ResponseTrailers_WithBody_TrailersSent(HttpContext context)
    {
        await context.Response.WriteAsync("Hello World");
        context.Response.AppendTrailer("TrailerName", "Trailer Value");
    }

    public async Task ResponseTrailers_WithContentLengthBody_TrailersSent(HttpContext context)
    {
        var body = "Hello World";
        context.Response.ContentLength = body.Length;
        await context.Response.WriteAsync(body);
        context.Response.AppendTrailer("TrailerName", "Trailer Value");
    }

    public async Task ResponseTrailers_WithTrailersBeforeContentLengthBody_TrailersSent(HttpContext context)
    {
        var body = "Hello World";
        context.Response.ContentLength = body.Length * 2;
        await context.Response.WriteAsync(body);
        context.Response.AppendTrailer("TrailerName", "Trailer Value");
        await context.Response.WriteAsync(body);
    }

    public async Task ResponseTrailers_WithContentLengthBodyAndDeclared_TrailersSent(HttpContext context)
    {
        var body = "Hello World";
        context.Response.ContentLength = body.Length;
        context.Response.DeclareTrailer("TrailerName");
        await context.Response.WriteAsync(body);
        context.Response.AppendTrailer("TrailerName", "Trailer Value");
    }

    public async Task ResponseTrailers_WithContentLengthBodyAndDeclaredButMissingTrailers_Completes(HttpContext context)
    {
        var body = "Hello World";
        context.Response.ContentLength = body.Length;
        context.Response.DeclareTrailer("TrailerName");
        await context.Response.WriteAsync(body);
    }

    public Task ResponseTrailers_MultipleValues_SentAsSeparateHeaders(HttpContext context)
    {
        context.Response.AppendTrailer("trailername", new StringValues(new[] { "TrailerValue0", "TrailerValue1" }));
        return Task.FromResult(0);
    }

    public Task ResponseTrailers_LargeTrailers_Success(HttpContext context)
    {
        var values = new[] {
                new string('a', 1024),
                new string('b', 1024 * 4),
                new string('c', 1024 * 8),
                new string('d', 1024 * 16),
                new string('e', 1024 * 32),
                new string('f', 1024 * 64 - 1) }; // Max header size

        context.Response.AppendTrailer("ThisIsALongerHeaderNameThatStillWorksForReals", new StringValues(values));
        return Task.FromResult(0);
    }

    public Task ResponseTrailers_NullValues_Ignored(HttpContext context)
    {
        foreach (var kvp in NullTrailers)
        {
            context.Response.AppendTrailer(kvp.Item1, kvp.Item2);
        }

        return Task.FromResult(0);
    }

    public Task AppException_BeforeResponseHeaders_500(HttpContext context)
    {
        throw new Exception("Application exception");
    }

    public async Task AppException_AfterHeaders_PriorOSVersions_ResetCancel(HttpContext httpContext)
    {
        await httpContext.Response.Body.FlushAsync();
        throw new Exception("Application exception");
    }

    public async Task AppException_AfterHeaders_ResetInternalError(HttpContext httpContext)
    {
        await httpContext.Response.Body.FlushAsync();
        throw new Exception("Application exception");
    }

    public Task Reset_PriorOSVersions_NotSupported(HttpContext httpContext)
    {
        Assert.Equal("HTTP/2", httpContext.Request.Protocol);
        var feature = httpContext.Features.Get<IHttpResetFeature>();
        Assert.Null(feature);
        return httpContext.Response.WriteAsync("Hello World");
    }

    public Task Reset_Http1_NotSupported(HttpContext httpContext)
    {
        Assert.Equal("HTTP/1.1", httpContext.Request.Protocol);
        var feature = httpContext.Features.Get<IHttpResetFeature>();
        Assert.Null(feature);
        return httpContext.Response.WriteAsync("Hello World");
    }

    private TaskCompletionSource _resetBeforeResponseResetsCts = new TaskCompletionSource();
    public Task Reset_BeforeResponse_Resets(HttpContext httpContext)
    {
        try
        {
            Assert.Equal("HTTP/2", httpContext.Request.Protocol);
            var feature = httpContext.Features.Get<IHttpResetFeature>();
            Assert.NotNull(feature);
            feature.Reset(1111); // Custom
            _resetBeforeResponseResetsCts.SetResult();
        }
        catch (Exception ex)
        {
            _resetBeforeResponseResetsCts.SetException(ex);
        }
        return Task.FromResult(0);
    }

    public async Task Reset_BeforeResponse_Resets_Complete(HttpContext httpContext)
    {
        await _resetBeforeResponseResetsCts.Task;
    }

    private TaskCompletionSource _resetBeforeResponseZeroResetsCts = new TaskCompletionSource();
    public Task Reset_BeforeResponse_Zero_Resets(HttpContext httpContext)
    {
        try
        {
            Assert.Equal("HTTP/2", httpContext.Request.Protocol);
            var feature = httpContext.Features.Get<IHttpResetFeature>();
            Assert.NotNull(feature);
            feature.Reset(0); // Zero should be an allowed errorCode
            _resetBeforeResponseZeroResetsCts.SetResult();
        }
        catch (Exception ex)
        {
            _resetBeforeResponseZeroResetsCts.SetException(ex);
        }
        return Task.FromResult(0);
    }

    public async Task Reset_BeforeResponse_Resets_Zero_Complete(HttpContext httpContext)
    {
        await _resetBeforeResponseZeroResetsCts.Task;
    }

    private TaskCompletionSource _resetAfterResponseHeadersResetsCts = new TaskCompletionSource();

    public async Task Reset_AfterResponseHeaders_Resets(HttpContext httpContext)
    {
        try
        {
            Assert.Equal("HTTP/2", httpContext.Request.Protocol);
            var feature = httpContext.Features.Get<IHttpResetFeature>();
            Assert.NotNull(feature);
            await httpContext.Response.Body.FlushAsync();
            feature.Reset(1111); // Custom
            _resetAfterResponseHeadersResetsCts.SetResult();
        }
        catch (Exception ex)
        {
            _resetAfterResponseHeadersResetsCts.SetException(ex);
        }
    }
    public async Task Reset_AfterResponseHeaders_Resets_Complete(HttpContext httpContext)
    {
        await _resetAfterResponseHeadersResetsCts.Task;
    }

    private TaskCompletionSource _resetDuringResponseBodyResetsCts = new TaskCompletionSource();

    public async Task Reset_DuringResponseBody_Resets(HttpContext httpContext)
    {
        try
        {
            Assert.Equal("HTTP/2", httpContext.Request.Protocol);
            var feature = httpContext.Features.Get<IHttpResetFeature>();
            Assert.NotNull(feature);
            await httpContext.Response.WriteAsync("Hello World");
            await httpContext.Response.Body.FlushAsync();
            feature.Reset(1111); // Custom
            _resetDuringResponseBodyResetsCts.SetResult();
        }
        catch (Exception ex)
        {
            _resetDuringResponseBodyResetsCts.SetException(ex);
        }
    }

    public async Task Reset_DuringResponseBody_Resets_Complete(HttpContext httpContext)
    {
        await _resetDuringResponseBodyResetsCts.Task;
    }

    private TaskCompletionSource _resetBeforeRequestBodyResetsCts = new TaskCompletionSource();

    public async Task Reset_BeforeRequestBody_Resets(HttpContext httpContext)
    {
        try
        {
            Assert.Equal("HTTP/2", httpContext.Request.Protocol);
            var feature = httpContext.Features.Get<IHttpResetFeature>();
            Assert.NotNull(feature);
            var readTask = httpContext.Request.Body.ReadAsync(new byte[10], 0, 10);

            feature.Reset(1111);

            await Assert.ThrowsAsync<IOException>(() => readTask);

            _resetBeforeRequestBodyResetsCts.SetResult();
        }
        catch (Exception ex)
        {
            _resetBeforeRequestBodyResetsCts.SetException(ex);
        }
    }

    public async Task Reset_BeforeRequestBody_Resets_Complete(HttpContext httpContext)
    {
        await _resetBeforeRequestBodyResetsCts.Task;
    }

    private TaskCompletionSource _resetDuringRequestBodyResetsCts = new TaskCompletionSource();

    public async Task Reset_DuringRequestBody_Resets(HttpContext httpContext)
    {
        try
        {
            Assert.Equal("HTTP/2", httpContext.Request.Protocol);
            var feature = httpContext.Features.Get<IHttpResetFeature>();
            Assert.NotNull(feature);

#if !FORWARDCOMPAT
            Assert.True(httpContext.Request.CanHaveBody());
#endif

            var read = await httpContext.Request.Body.ReadAsync(new byte[10], 0, 10);
            Assert.Equal(10, read);

            var readTask = httpContext.Request.Body.ReadAsync(new byte[10], 0, 10);
            feature.Reset(1111);
            await Assert.ThrowsAsync<IOException>(() => readTask);

            _resetDuringRequestBodyResetsCts.SetResult();
        }
        catch (Exception ex)
        {
            _resetDuringRequestBodyResetsCts.SetException(ex);
        }
    }

    public Task Goaway(HttpContext httpContext)
    {
        httpContext.Response.Headers["Connection"] = "close";
        return Task.CompletedTask;
    }

    public Task ConnectionRequestClose(HttpContext httpContext)
    {
        httpContext.Connection.RequestClose();
        return Task.CompletedTask;
    }

    private TaskCompletionSource _completeAsync = new TaskCompletionSource();

    public async Task CompleteAsync(HttpContext httpContext)
    {
        await httpContext.Response.CompleteAsync();
        await _completeAsync.Task;
    }

    public Task CompleteAsync_Completed(HttpContext httpContext)
    {
        _completeAsync.TrySetResult();
        return Task.CompletedTask;
    }

    public async Task Reset_DuringRequestBody_Resets_Complete(HttpContext httpContext)
    {
        await _resetDuringRequestBodyResetsCts.Task;
    }

    private TaskCompletionSource<object> _onCompletedHttpContext = new TaskCompletionSource<object>();
    public async Task OnCompletedHttpContext(HttpContext context)
    {
        // This shouldn't block the response or the server from shutting down.
        context.Response.OnCompleted(async () =>
        {
            var context = _httpContextAccessor.HttpContext;

            await Task.Delay(500);
            // Access all fields of the connection after final flush.
            try
            {
                _ = context.Connection.RemoteIpAddress;
                _ = context.Connection.LocalIpAddress;
                _ = context.Connection.Id;
                _ = context.Connection.ClientCertificate;
                _ = context.Connection.LocalPort;
                _ = context.Connection.RemotePort;

                _ = context.Request.ContentLength;
                _ = context.Request.Headers;
                _ = context.Request.Query;
                _ = context.Request.Body;
                _ = context.Request.ContentType;

                _ = context.Response.StatusCode;
                _ = context.Response.Body;
                _ = context.Response.Headers;
                _ = context.Response.ContentType;
            }
            catch (Exception ex)
            {
                _onCompletedHttpContext.TrySetResult(ex);
            }

            _onCompletedHttpContext.TrySetResult(null);
        });

        await context.Response.WriteAsync("SlowOnCompleted");
    }

    public async Task OnCompletedHttpContext_Completed(HttpContext httpContext)
    {
        await _onCompletedHttpContext.Task;
    }

    private TaskCompletionSource _responseTrailers_CompleteAsyncNoBody_TrailersSent = new TaskCompletionSource();
    public async Task ResponseTrailers_CompleteAsyncNoBody_TrailersSent(HttpContext httpContext)
    {
        httpContext.Response.AppendTrailer("trailername", "TrailerValue");
        await httpContext.Response.CompleteAsync();
        await _responseTrailers_CompleteAsyncNoBody_TrailersSent.Task;
    }

    public Task ResponseTrailers_CompleteAsyncNoBody_TrailersSent_Completed(HttpContext httpContext)
    {
        _responseTrailers_CompleteAsyncNoBody_TrailersSent.TrySetResult();
        return Task.CompletedTask;
    }

    private TaskCompletionSource _responseTrailers_CompleteAsyncWithBody_TrailersSent = new TaskCompletionSource();
    public async Task ResponseTrailers_CompleteAsyncWithBody_TrailersSent(HttpContext httpContext)
    {
        await httpContext.Response.WriteAsync("Hello World");
        httpContext.Response.AppendTrailer("TrailerName", "Trailer Value");
        await httpContext.Response.CompleteAsync();
        await _responseTrailers_CompleteAsyncWithBody_TrailersSent.Task;
    }

    public Task ResponseTrailers_CompleteAsyncWithBody_TrailersSent_Completed(HttpContext httpContext)
    {
        _responseTrailers_CompleteAsyncWithBody_TrailersSent.TrySetResult();
        return Task.CompletedTask;
    }

    public async Task Reset_AfterCompleteAsync_NoReset(HttpContext httpContext)
    {
        Assert.Equal("HTTP/2", httpContext.Request.Protocol);
        var feature = httpContext.Features.Get<IHttpResetFeature>();
        Assert.NotNull(feature);
        await httpContext.Response.WriteAsync("Hello World");
        await httpContext.Response.CompleteAsync();
        // The request and response are fully complete, the reset doesn't get sent.
        feature.Reset(1111);
    }

    public async Task Reset_CompleteAsyncDuringRequestBody_Resets(HttpContext httpContext)
    {
        Assert.Equal("HTTP/2", httpContext.Request.Protocol);
        var feature = httpContext.Features.Get<IHttpResetFeature>();
        Assert.NotNull(feature);

        var read = await httpContext.Request.Body.ReadAsync(new byte[10], 0, 10);
        Assert.Equal(10, read);

        var readTask = httpContext.Request.Body.ReadAsync(new byte[10], 0, 10);
        await httpContext.Response.CompleteAsync();
        feature.Reset((int)0); // GRPC does this
        await Assert.ThrowsAsync<IOException>(() => readTask);
    }

    public Task Http2_MethodsRequestWithoutData_Success(HttpContext httpContext)
    {
        Assert.Equal("HTTP/2", httpContext.Request.Protocol);
#if !FORWARDCOMPAT
        Assert.False(httpContext.Request.CanHaveBody());
        var feature = httpContext.Features.Get<IHttpUpgradeFeature>();
        // The upgrade feature won't be present if WebSockets aren't enabled in IIS.
        // IsUpgradableRequest should always return false for HTTP/2.
        Assert.False(feature?.IsUpgradableRequest ?? false);
#endif
        Assert.Null(httpContext.Request.ContentLength);
        Assert.False(httpContext.Request.Headers.ContainsKey(HeaderNames.TransferEncoding));
        return Task.CompletedTask;
    }

    public Task Http2_RequestWithDataAndContentLength_Success(HttpContext httpContext)
    {
        Assert.Equal("HTTP/2", httpContext.Request.Protocol);
#if !FORWARDCOMPAT
        Assert.True(httpContext.Request.CanHaveBody());
#endif
        Assert.Equal(11, httpContext.Request.ContentLength);
        Assert.False(httpContext.Request.Headers.ContainsKey(HeaderNames.TransferEncoding));
        return httpContext.Request.Body.CopyToAsync(httpContext.Response.Body);
    }

    public Task Http2_RequestWithDataAndNoContentLength_Success(HttpContext httpContext)
    {
        Assert.Equal("HTTP/2", httpContext.Request.Protocol);
#if !FORWARDCOMPAT
        Assert.True(httpContext.Request.CanHaveBody());
#endif
        Assert.Null(httpContext.Request.ContentLength);
        // The client didn't send this header, Http.Sys added it for back compat with HTTP/1.1.
        Assert.Equal("chunked", httpContext.Request.Headers.TransferEncoding);
        return httpContext.Request.Body.CopyToAsync(httpContext.Response.Body);
    }

    public Task Http2_ResponseWithData_Success(HttpContext httpContext)
    {
        Assert.Equal("HTTP/2", httpContext.Request.Protocol);
        return httpContext.Response.WriteAsync("Hello World");
    }

    public Task IncreaseRequestLimit(HttpContext httpContext)
    {
        var maxRequestBodySizeFeature = httpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
        maxRequestBodySizeFeature.MaxRequestBodySize = 2;
        return Task.CompletedTask;
    }

    public Task OnCompletedThrows(HttpContext httpContext)
    {
        httpContext.Response.OnCompleted(() =>
        {
            throw new Exception();
        });

        return Task.CompletedTask;
    }

    public Task Http3_Direct(HttpContext context)
    {
        try
        {
            Assert.True(context.Request.IsHttps);
            return context.Response.WriteAsync(context.Request.Protocol);
        }
        catch (Exception ex)
        {
            return context.Response.WriteAsync(ex.ToString());
        }
    }

    public Task Http3_AltSvcHeader_UpgradeFromHttp1(HttpContext context)
    {
        var altsvc = $@"h3="":{context.Connection.LocalPort}""";
        try
        {
            Assert.True(context.Request.IsHttps);
            context.Response.Headers.AltSvc = altsvc;
            return context.Response.WriteAsync(context.Request.Protocol);
        }
        catch (Exception ex)
        {
            return context.Response.WriteAsync(ex.ToString());
        }
    }

    public Task Http3_AltSvcHeader_UpgradeFromHttp2(HttpContext context)
    {
        return Http3_AltSvcHeader_UpgradeFromHttp1(context);
    }

    public async Task Http3_ResponseTrailers(HttpContext context)
    {
        try
        {
            Assert.True(context.Request.IsHttps);
            await context.Response.WriteAsync(context.Request.Protocol);
            context.Response.AppendTrailer("custom", "value");
        }
        catch (Exception ex)
        {
            await context.Response.WriteAsync(ex.ToString());
        }
    }

    public Task Http3_ResetBeforeHeaders(HttpContext context)
    {
        try
        {
            Assert.True(context.Request.IsHttps);
            context.Features.Get<IHttpResetFeature>().Reset(0x010b); // H3_REQUEST_REJECTED
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            return context.Response.WriteAsync(ex.ToString());
        }
    }

    private TaskCompletionSource _http3_ResetAfterHeadersCts = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

    public async Task Http3_ResetAfterHeaders(HttpContext context)
    {
        try
        {
            Assert.True(context.Request.IsHttps);
            await context.Response.Body.FlushAsync();
            await _http3_ResetAfterHeadersCts.Task;
            context.Features.Get<IHttpResetFeature>().Reset(0x010c); // H3_REQUEST_CANCELLED
        }
        catch (Exception ex)
        {
            await context.Response.WriteAsync(ex.ToString());
        }
    }

    public Task Http3_ResetAfterHeaders_SetResult(HttpContext context)
    {
        _http3_ResetAfterHeadersCts.SetResult();
        return Task.CompletedTask;
    }

    private TaskCompletionSource _http3_AppExceptionAfterHeaders_InternalErrorCts = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

    public async Task Http3_AppExceptionAfterHeaders_InternalError(HttpContext context)
    {
        await context.Response.Body.FlushAsync();
        await _http3_AppExceptionAfterHeaders_InternalErrorCts.Task;
        throw new Exception("App Exception");
    }

    public Task Http3_AppExceptionAfterHeaders_InternalError_SetResult(HttpContext context)
    {
        _http3_AppExceptionAfterHeaders_InternalErrorCts.SetResult();
        return Task.CompletedTask;
    }

    public Task Http3_Abort_Cancel(HttpContext context)
    {
        context.Abort();
        return Task.CompletedTask;
    }

    internal static readonly HashSet<(string, StringValues, StringValues)> NullTrailers = new HashSet<(string, StringValues, StringValues)>()
        {
            ("NullString", (string)null, (string)null),
            ("EmptyString", "", ""),
            ("NullStringArray", new string[] { null }, ""),
            ("EmptyStringArray", new string[] { "" }, ""),
            ("MixedStringArray", new string[] { null, "" }, new string[] { "", "" }),
            ("WithValidStrings", new string[] { null, "Value", "" }, new string[] { "", "Value", "" })
        };

    // https://tools.ietf.org/html/rfc7230#section-4.1.2
    internal static readonly HashSet<string> DisallowedTrailers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Message framing headers.
            HeaderNames.TransferEncoding, HeaderNames.ContentLength,

            // Routing headers.
            HeaderNames.Host,

            // Request modifiers: controls and conditionals.
            // rfc7231#section-5.1: Controls.
            HeaderNames.CacheControl, HeaderNames.Expect, HeaderNames.MaxForwards, HeaderNames.Pragma, HeaderNames.Range, HeaderNames.TE,

            // rfc7231#section-5.2: Conditionals.
            HeaderNames.IfMatch, HeaderNames.IfNoneMatch, HeaderNames.IfModifiedSince, HeaderNames.IfUnmodifiedSince, HeaderNames.IfRange,

            // Authentication headers.
            HeaderNames.WWWAuthenticate, HeaderNames.Authorization, HeaderNames.ProxyAuthenticate, HeaderNames.ProxyAuthorization, HeaderNames.SetCookie, HeaderNames.Cookie,

            // Response control data.
            // rfc7231#section-7.1: Control Data.
            HeaderNames.Age, HeaderNames.Expires, HeaderNames.Date, HeaderNames.Location, HeaderNames.RetryAfter, HeaderNames.Vary, HeaderNames.Warning,

            // Content-Encoding, Content-Type, Content-Range, and Trailer itself.
            HeaderNames.ContentEncoding, HeaderNames.ContentType, HeaderNames.ContentRange, HeaderNames.Trailer
        };
#endif
}
