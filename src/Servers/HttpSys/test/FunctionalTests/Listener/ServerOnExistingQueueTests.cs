// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.HttpSys.Internal;
using Microsoft.AspNetCore.InternalTesting;
using Windows.Win32;
using Xunit;

namespace Microsoft.AspNetCore.Server.HttpSys.Listener;

public class ServerOnExistingQueueTests
{
    [ConditionalFact]
    public async Task Server_200OK_Success()
    {
        using var baseServer = Utilities.CreateHttpServer(out var address);
        using var server = Utilities.CreateServerOnExistingQueue(baseServer.Options.RequestQueueName);

        var responseTask = SendRequestAsync(address);

        var context = await server.AcceptAsync(Utilities.DefaultTimeout);
        context.Dispose();

        var response = await responseTask;
        Assert.Equal(string.Empty, response);
    }

    [ConditionalFact]
    public async Task Server_SendHelloWorld_Success()
    {
        using var baseServer = Utilities.CreateHttpServer(out var address);
        using var server = Utilities.CreateServerOnExistingQueue(baseServer.Options.RequestQueueName);

        Task<string> responseTask = SendRequestAsync(address);

        var context = await server.AcceptAsync(Utilities.DefaultTimeout);
        context.Response.ContentLength = 11;
        await using (var writer = new StreamWriter(context.Response.Body))
        {
            await writer.WriteAsync("Hello World");
        }

        string response = await responseTask;
        Assert.Equal("Hello World", response);
    }

    [ConditionalFact]
    public async Task Server_EchoHelloWorld_Success()
    {
        using var baseServer = Utilities.CreateHttpServer(out var address);
        using var server = Utilities.CreateServerOnExistingQueue(baseServer.Options.RequestQueueName);

        var responseTask = SendRequestAsync(address, "Hello World");

        var context = await server.AcceptAsync(Utilities.DefaultTimeout);
        string input = await new StreamReader(context.Request.Body).ReadToEndAsync();
        Assert.Equal("Hello World", input);
        context.Response.ContentLength = 11;
        await using (var writer = new StreamWriter(context.Response.Body))
        {
            await writer.WriteAsync("Hello World");
        }

        var response = await responseTask;
        Assert.Equal("Hello World", response);
    }

    [ConditionalFact]
    // No-ops if you did not create the queue
    public async Task Server_SetQueueLimit_Success()
    {
        using var baseServer = Utilities.CreateHttpServer(out var address);
        using var server = Utilities.CreateServerOnExistingQueue(baseServer.Options.RequestQueueName);
        server.Options.RequestQueueLimit = 1001;
        var responseTask = SendRequestAsync(address);

        var context = await server.AcceptAsync(Utilities.DefaultTimeout);
        context.Dispose();

        var response = await responseTask;
        Assert.Equal(string.Empty, response);
    }

    [ConditionalFact]
    public async Task Server_PathBase_Success()
    {
        using var baseServer = Utilities.CreateDynamicHttpServer("/PathBase", out var root, out var address);
        using var server = Utilities.CreateServerOnExistingQueue(baseServer.Options.RequestQueueName);
        server.Options.UrlPrefixes.Add(address); // Need to mirror the setting so we can parse out PathBase

        var responseTask = SendRequestAsync(root + "/pathBase/paTh");

        var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
        Assert.Equal("/pathBase", context.Request.PathBase);
        Assert.Equal("/paTh", context.Request.Path);
        context.Dispose();

        var response = await responseTask;
        Assert.Equal(string.Empty, response);
    }

    [ConditionalFact]
    public async Task Server_PathBaseMismatch_Success()
    {
        using var baseServer = Utilities.CreateDynamicHttpServer("/PathBase", out var root, out var address);
        using var server = Utilities.CreateServerOnExistingQueue(baseServer.Options.RequestQueueName);

        var responseTask = SendRequestAsync(root + "/pathBase/paTh");

        var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
        Assert.Equal(string.Empty, context.Request.PathBase);
        Assert.Equal("/pathBase/paTh", context.Request.Path);
        context.Dispose();

        var response = await responseTask;
        Assert.Equal(string.Empty, response);
    }

    [ConditionalTheory]
    [InlineData("/", "/", "", "/")]
    [InlineData("/basepath/", "/basepath", "/basepath", "")]
    [InlineData("/basepath/", "/basepath/", "/basepath", "/")]
    [InlineData("/basepath/", "/basepath/subpath", "/basepath", "/subpath")]
    [InlineData("/base path/", "/base%20path/sub%20path", "/base path", "/sub path")]
    [InlineData("/base葉path/", "/base%E8%91%89path/sub%E8%91%89path", "/base葉path", "/sub葉path")]
    [InlineData("/basepath/", "/basepath/sub%2Fpath", "/basepath", "/sub%2Fpath")]
    public async Task Server_PathSplitting(string pathBase, string requestPath, string expectedPathBase, string expectedPath)
    {
        using var baseServer = Utilities.CreateDynamicHttpServer(pathBase, out var root, out var baseAddress);
        using var server = Utilities.CreateServerOnExistingQueue(baseServer.Options.RequestQueueName);
        server.Options.UrlPrefixes.Add(baseAddress); // Keep them in sync

        var responseTask = SendRequestAsync(root + requestPath);

        var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
        Assert.Equal(expectedPathBase, context.Request.PathBase);
        Assert.Equal(expectedPath, context.Request.Path);
        context.Dispose();

        var response = await responseTask;
        Assert.Equal(string.Empty, response);
    }

    [ConditionalFact]
    public async Task Server_LongestPathSplitting()
    {
        using var baseServer = Utilities.CreateDynamicHttpServer("/basepath", out var root, out var baseAddress);
        baseServer.Options.UrlPrefixes.Add(baseAddress + "secondTier");
        using var server = Utilities.CreateServerOnExistingQueue(baseServer.Options.RequestQueueName);
        server.Options.UrlPrefixes.Add(baseAddress); // Keep them in sync
        server.Options.UrlPrefixes.Add(baseAddress + "secondTier");

        var responseTask = SendRequestAsync(root + "/basepath/secondTier/path/thing");

        var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
        Assert.Equal("/basepath/secondTier", context.Request.PathBase);
        Assert.Equal("/path/thing", context.Request.Path);
        context.Dispose();

        var response = await responseTask;
        Assert.Equal(string.Empty, response);
    }

    [ConditionalFact]
    // Changes to the base server are reflected
    public async Task Server_HotAddPrefix_Success()
    {
        using var baseServer = Utilities.CreateHttpServer(out var address);
        using var server = Utilities.CreateServerOnExistingQueue(baseServer.Options.RequestQueueName);
        server.Options.UrlPrefixes.Add(address); // Keep them in sync

        var responseTask = SendRequestAsync(address);

        var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
        Assert.Equal(string.Empty, context.Request.PathBase);
        Assert.Equal("/", context.Request.Path);
        context.Dispose();

        var response = await responseTask;
        Assert.Equal(string.Empty, response);

        address += "pathbase/";
        baseServer.Options.UrlPrefixes.Add(address);
        server.Options.UrlPrefixes.Add(address);

        responseTask = SendRequestAsync(address);

        context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
        Assert.Equal("/pathbase", context.Request.PathBase);
        Assert.Equal("/", context.Request.Path);
        context.Dispose();

        response = await responseTask;
        Assert.Equal(string.Empty, response);
    }

    [ConditionalFact]
    // Changes to the base server are reflected
    public async Task Server_HotRemovePrefix_Success()
    {
        using var baseServer = Utilities.CreateHttpServer(out var address);
        using var server = Utilities.CreateServerOnExistingQueue(baseServer.Options.RequestQueueName);
        server.Options.UrlPrefixes.Add(address); // Keep them in sync

        address += "pathbase/";
        baseServer.Options.UrlPrefixes.Add(address);
        server.Options.UrlPrefixes.Add(address);
        var responseTask = SendRequestAsync(address);

        var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
        Assert.Equal("/pathbase", context.Request.PathBase);
        Assert.Equal("/", context.Request.Path);
        context.Dispose();

        var response = await responseTask;
        Assert.Equal(string.Empty, response);

        Assert.True(baseServer.Options.UrlPrefixes.Remove(address));
        Assert.True(server.Options.UrlPrefixes.Remove(address));

        responseTask = SendRequestAsync(address);

        context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
        Assert.Equal(string.Empty, context.Request.PathBase);
        Assert.Equal("/pathbase/", context.Request.Path);
        context.Dispose();

        response = await responseTask;
        Assert.Equal(string.Empty, response);
    }

    [ConditionalFact]
    public async Task Server_CreateOrAttach_NoUrlPrefix_NewUrlPrefixWorks()
    {
        var queueName = Guid.NewGuid().ToString();

        // Create a queue without a UrlGroup or any UrlPrefixes
        var statusCode = PInvoke.HttpCreateRequestQueue(
                HttpApi.Version,
                queueName,
                default,
                0,
                out var requestQueueHandle);

        Assert.True(statusCode == ErrorCodes.ERROR_SUCCESS);

        using var server = Utilities.CreateServer(options =>
        {
            options.RequestQueueName = queueName;
            options.RequestQueueMode = RequestQueueMode.CreateOrAttach;
            options.UrlPrefixes.Add("http://localhost:0");
        });

        var address = server.Options.UrlPrefixes.First().FullPrefix;

        var responseTask = SendRequestAsync(address);

        var context = await server.AcceptAsync(Utilities.DefaultTimeout);
        context.Dispose();

        var response = await responseTask;
        Assert.Equal(string.Empty, response);
    }

    [ConditionalFact]
    public async Task Server_CreateOrAttach_UrlPrefixExist_ExistingUrlPrefixWorks()
    {
        using var baseServer = Utilities.CreateHttpServer(out var address);
        using var server = Utilities.CreateServer(options =>
        {
            options.RequestQueueName = baseServer.Options.RequestQueueName;
            options.RequestQueueMode = RequestQueueMode.CreateOrAttach;
            options.UrlPrefixes.Add(address);
        });

        var responseTask = SendRequestAsync(address);

        var context = await server.AcceptAsync(Utilities.DefaultTimeout);
        context.Dispose();

        var response = await responseTask;
        Assert.Equal(string.Empty, response);
    }

    [ConditionalFact]
    public async Task Server_CreateOrAttach_UrlPrefixExist_NewAndExistingUrlPrefixsWork()
    {
        using var baseServer = Utilities.CreateHttpServerReturnRoot("/baseServer", out string rootAddress);
        using var server = Utilities.CreateServer(options =>
        {
            options.RequestQueueName = baseServer.Options.RequestQueueName;
            options.RequestQueueMode = RequestQueueMode.CreateOrAttach;
            options.UrlPrefixes.Add(rootAddress + "/server");
        });

        var responseTask = SendRequestAsync(rootAddress + "/baseServer");

        var context = await server.AcceptAsync(Utilities.DefaultTimeout);
        context.Dispose();

        var response = await responseTask;
        Assert.Equal(string.Empty, response);

        responseTask = SendRequestAsync(rootAddress + "/server");

        context = await server.AcceptAsync(Utilities.DefaultTimeout);
        context.Dispose();

        response = await responseTask;
        Assert.Equal(string.Empty, response);
    }

    private async Task<string> SendRequestAsync(string uri)
    {
        using HttpClient client = new HttpClient();
        return await client.GetStringAsync(uri);
    }

    private async Task<string> SendRequestAsync(string uri, string upload)
    {
        using HttpClient client = new HttpClient();
        HttpResponseMessage response = await client.PostAsync(uri, new StringContent(upload));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
}
