// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.TestHost.Tests;

public class ResponseBodyTests
{
    [Fact]
    public async Task BodyWriter_GetMemoryAdvance_AutoCompleted()
    {
        var length = -1;
        using var host = await CreateHost(httpContext =>
        {
            var writer = httpContext.Response.BodyWriter;
            length = writer.GetMemory().Length;
            writer.Advance(length);
            return Task.CompletedTask;
        });

        var response = await host.GetTestServer().CreateClient().GetAsync("/");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal(length, bytes.Length);
    }

    [Fact]
    public async Task BodyWriter_StartAsyncGetMemoryAdvance_AutoCompleted()
    {
        var length = -1;
        using var host = await CreateHost(async httpContext =>
        {
            await httpContext.Response.StartAsync();
            var writer = httpContext.Response.BodyWriter;
            length = writer.GetMemory().Length;
            writer.Advance(length);
        });

        var response = await host.GetTestServer().CreateClient().GetAsync("/");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal(length, bytes.Length);
    }

    [Fact]
    public async Task BodyStream_SyncDisabled_WriteThrows()
    {
        var contentBytes = new byte[] { 32 };
        using var host = await CreateHost(async httpContext =>
        {
            await httpContext.Response.StartAsync();
            httpContext.Response.Body.Write(contentBytes, 0, contentBytes.Length);
            await httpContext.Response.CompleteAsync();
        });

        var client = host.GetTestServer().CreateClient();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetAsync("/"));
        Assert.Contains("Synchronous operations are disallowed.", ex.Message);
    }

    [Fact]
    public async Task BodyStream_SyncEnabled_WriteSucceeds()
    {
        var contentBytes = new byte[] { 32 };
        using var host = await CreateHost(async httpContext =>
        {
            await httpContext.Response.StartAsync();
            httpContext.Response.Body.Write(contentBytes, 0, contentBytes.Length);
            await httpContext.Response.CompleteAsync();
        });

        host.GetTestServer().AllowSynchronousIO = true;

        var client = host.GetTestServer().CreateClient();
        var response = await client.GetAsync("/");
        var responseBytes = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal(contentBytes, responseBytes);
    }

    [Fact]
    public async Task BodyStream_SyncDisabled_FlushThrows()
    {
        var contentBytes = new byte[] { 32 };
        using var host = await CreateHost(async httpContext =>
        {
            await httpContext.Response.StartAsync();
            await httpContext.Response.Body.WriteAsync(contentBytes, 0, contentBytes.Length);
            httpContext.Response.Body.Flush();
            await httpContext.Response.CompleteAsync();
        });

        var client = host.GetTestServer().CreateClient();
        var requestException = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync("/"));
        var ex = (InvalidOperationException)requestException?.InnerException?.InnerException;
        Assert.NotNull(ex);
        Assert.Contains("Synchronous operations are disallowed.", ex.Message);
    }

    [Fact]
    public async Task BodyStream_SyncEnabled_FlushSucceeds()
    {
        var contentBytes = new byte[] { 32 };
        using var host = await CreateHost(async httpContext =>
        {
            await httpContext.Response.StartAsync();
            await httpContext.Response.Body.WriteAsync(contentBytes, 0, contentBytes.Length);
            httpContext.Response.Body.Flush();
            await httpContext.Response.CompleteAsync();
        });

        host.GetTestServer().AllowSynchronousIO = true;

        var client = host.GetTestServer().CreateClient();
        var response = await client.GetAsync("/");
        var responseBytes = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal(contentBytes, responseBytes);
    }

    [Fact]
    public async Task BodyStream_ZeroByteRead_Success()
    {
        var emptyReadStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var contentBytes = new byte[] { 32 };
        using var host = await CreateHost(async httpContext =>
        {
            await httpContext.Response.Body.WriteAsync(contentBytes);
            await emptyReadStarted.Task;
            await httpContext.Response.Body.WriteAsync(contentBytes);
        });

        var client = host.GetTestServer().CreateClient();
        var response = await client.GetAsync("/", HttpCompletionOption.ResponseHeadersRead);
        var stream = await response.Content.ReadAsStreamAsync();
        var bytes = new byte[5];
        var read = await stream.ReadAsync(bytes);
        Assert.Equal(1, read);
        Assert.Equal(contentBytes[0], bytes[0]);

        // This will chain to the Memory overload, but that does less restrictive validation on the input.
        // https://github.com/dotnet/aspnetcore/issues/41692#issuecomment-1248714684
        var zeroByteRead = stream.ReadAsync(Array.Empty<byte>(), 0, 0);
        Assert.False(zeroByteRead.IsCompleted);
        emptyReadStarted.SetResult();
        read = await zeroByteRead.DefaultTimeout();
        Assert.Equal(0, read);

        read = await stream.ReadAsync(bytes);
        Assert.Equal(1, read);
        Assert.Equal(contentBytes[0], bytes[0]);
    }

    private Task<IHost> CreateHost(RequestDelegate appDelegate)
    {
        return new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseTestServer()
                    .Configure(app =>
                    {
                        app.Run(appDelegate);
                    });
            })
            .StartAsync();
    }
}
