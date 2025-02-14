// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.Server.HttpSys;

public class ResponseTests : LoggedTest
{
    [ConditionalFact]
    public async Task Response_ServerSendsDefaultResponse_ServerProvidesStatusCodeAndReasonPhrase()
    {
        string address;
        using (Utilities.CreateHttpServer(out address, httpContext =>
        {
            Assert.Equal(200, httpContext.Response.StatusCode);
            Assert.False(httpContext.Response.HasStarted);
            return Task.FromResult(0);
        }, LoggerFactory))
        {
            HttpResponseMessage response = await SendRequestAsync(address);
            Assert.Equal(200, (int)response.StatusCode);
            Assert.Equal("OK", response.ReasonPhrase);
            Assert.Equal(new Version(1, 1), response.Version);
            Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
        }
    }

    [ConditionalFact]
    public async Task Response_ServerSendsSpecificStatus_ServerProvidesReasonPhrase()
    {
        string address;
        using (Utilities.CreateHttpServer(out address, httpContext =>
        {
            httpContext.Response.StatusCode = 201;
            // TODO: httpContext["owin.ResponseProtocol"] = "HTTP/1.0"; // Http.Sys ignores this value
            return Task.FromResult(0);
        }, LoggerFactory))
        {
            HttpResponseMessage response = await SendRequestAsync(address);
            Assert.Equal(201, (int)response.StatusCode);
            Assert.Equal("Created", response.ReasonPhrase);
            Assert.Equal(new Version(1, 1), response.Version);
            Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
        }
    }

    [ConditionalFact]
    public async Task Response_ServerSendsSpecificStatusAndReasonPhrase_PassedThrough()
    {
        string address;
        using (Utilities.CreateHttpServer(out address, httpContext =>
        {
            httpContext.Response.StatusCode = 201;
            httpContext.Features.Get<IHttpResponseFeature>().ReasonPhrase = "CustomReasonPhrase"; // TODO?
                                                                                                  // TODO: httpContext["owin.ResponseProtocol"] = "HTTP/1.0"; // Http.Sys ignores this value
            return Task.FromResult(0);
        }, LoggerFactory))
        {
            HttpResponseMessage response = await SendRequestAsync(address);
            Assert.Equal(201, (int)response.StatusCode);
            Assert.Equal("CustomReasonPhrase", response.ReasonPhrase);
            Assert.Equal(new Version(1, 1), response.Version);
            Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
        }
    }

    [ConditionalFact]
    public async Task Response_ServerSendsCustomStatus_NoReasonPhrase()
    {
        string address;
        using (Utilities.CreateHttpServer(out address, httpContext =>
        {
            httpContext.Response.StatusCode = 901;
            return Task.FromResult(0);
        }, LoggerFactory))
        {
            HttpResponseMessage response = await SendRequestAsync(address);
            Assert.Equal(901, (int)response.StatusCode);
            Assert.Equal(string.Empty, response.ReasonPhrase);
            Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
        }
    }

    [ConditionalFact]
    public async Task Response_StatusCode100_Throws()
    {
        string address;
        using (Utilities.CreateHttpServer(out address, httpContext =>
        {
            httpContext.Response.StatusCode = 100;
            return Task.FromResult(0);
        }, LoggerFactory))
        {
            HttpResponseMessage response = await SendRequestAsync(address);
            Assert.Equal(500, (int)response.StatusCode);
        }
    }

    [ConditionalFact]
    public async Task Response_StatusCode0_Throws()
    {
        string address;
        using (Utilities.CreateHttpServer(out address, httpContext =>
        {
            httpContext.Response.StatusCode = 0;
            return Task.FromResult(0);
        }, LoggerFactory))
        {
            HttpResponseMessage response = await SendRequestAsync(address);
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }
    }

    [ConditionalFact]
    public async Task Response_Empty_CallsOnStartingAndOnCompleted()
    {
        var onStartingCalled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var onCompletedCalled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        using (Utilities.CreateHttpServer(out var address, httpContext =>
        {
            httpContext.Response.OnStarting(state =>
            {
                Assert.Same(state, httpContext);
                onStartingCalled.SetResult();
                return Task.CompletedTask;
            }, httpContext);
            httpContext.Response.OnCompleted(state =>
            {
                Assert.Same(state, httpContext);
                onCompletedCalled.SetResult();
                return Task.CompletedTask;
            }, httpContext);
            return Task.CompletedTask;
        }, LoggerFactory))
        {
            var response = await SendRequestAsync(address);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            await onStartingCalled.Task.TimeoutAfter(TimeSpan.FromSeconds(1));
            // Fires after the response completes
            await onCompletedCalled.Task.TimeoutAfter(TimeSpan.FromSeconds(5));
        }
    }

    [ConditionalFact]
    public async Task Response_OnStartingThrows_StillCallsOnCompleted()
    {
        var onStartingCalled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var onCompletedCalled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using (Utilities.CreateHttpServer(out var address, httpContext =>
        {
            httpContext.Response.OnStarting(state =>
            {
                onStartingCalled.SetResult();
                throw new Exception("Failed OnStarting");
            }, httpContext);
            httpContext.Response.OnCompleted(state =>
            {
                Assert.Same(state, httpContext);
                onCompletedCalled.SetResult();
                return Task.CompletedTask;
            }, httpContext);
            return Task.CompletedTask;
        }, LoggerFactory))
        {
            var response = await SendRequestAsync(address);
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            await onStartingCalled.Task.TimeoutAfter(TimeSpan.FromSeconds(1));
            // Fires after the response completes
            await onCompletedCalled.Task.TimeoutAfter(TimeSpan.FromSeconds(5));
        }
    }

    [ConditionalFact]
    public async Task Response_OnStartingThrowsAfterWrite_WriteThrowsAndStillCallsOnCompleted()
    {
        var onStartingCalled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var onCompletedCalled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using (Utilities.CreateHttpServer(out var address, httpContext =>
        {
            httpContext.Response.OnStarting(state =>
            {
                onStartingCalled.SetResult();
                throw new InvalidTimeZoneException("Failed OnStarting");
            }, httpContext);
            httpContext.Response.OnCompleted(state =>
            {
                Assert.Same(state, httpContext);
                onCompletedCalled.SetResult();
                return Task.CompletedTask;
            }, httpContext);
            Assert.Throws<InvalidTimeZoneException>(() => httpContext.Response.Body.Write(new byte[10], 0, 10));
            return Task.CompletedTask;
        }, LoggerFactory))
        {
            var response = await SendRequestAsync(address);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            await onStartingCalled.Task.TimeoutAfter(TimeSpan.FromSeconds(1));
            // Fires after the response completes
            await onCompletedCalled.Task.TimeoutAfter(TimeSpan.FromSeconds(5));
        }
    }

    [ConditionalFact]
    public async Task ClientDisconnectsBeforeResponse_ResponseCanStillBeModified()
    {
        var readStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var readCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var server = Utilities.CreateHttpServer(out var address, async httpContext =>
        {
            var readTask = httpContext.Request.Body.ReadAsync(new byte[10]);
            readStarted.SetResult();
            try
            {
                await readTask;
                readCompleted.SetException(new InvalidOperationException("The read wasn't supposed to succeed"));
                return;
            }
            catch (IOException)
            {
            }

            try
            {
                // https://github.com/dotnet/aspnetcore/issues/12194
                // Modifying the response after the client has disconnected must be allowed.
                Assert.False(httpContext.Response.HasStarted);
                httpContext.Response.StatusCode = 400;
                httpContext.Response.ContentType = "text/plain";
                await httpContext.Response.WriteAsync("Body");
            }
            catch (Exception ex)
            {
                readCompleted.SetException(ex);
                return;
            }

            readCompleted.SetResult();
        }, LoggerFactory);

        // Send a request without the body.
        var uri = new Uri(address);
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("POST / HTTP/1.1");
        builder.AppendLine("Connection: close");
        builder.Append("HOST: ");
        builder.AppendLine(uri.Authority);
        builder.AppendLine("Content-Length: 10");
        builder.AppendLine();

        byte[] request = Encoding.ASCII.GetBytes(builder.ToString());

        using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        socket.Connect(uri.Host, uri.Port);
        socket.Send(request);

        await readStarted.Task.DefaultTimeout();

        // Disconnect
        socket.Close();

        // Make sure the server code behaved as expected.
        await readCompleted.Task.DefaultTimeout();
    }

    private async Task<HttpResponseMessage> SendRequestAsync(string uri)
    {
        using (var client = new HttpClient())
        {
            return await client.GetAsync(uri);
        }
    }
}
