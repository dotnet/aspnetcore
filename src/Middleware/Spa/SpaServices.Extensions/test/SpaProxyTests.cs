// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.SpaServices.Extensions.Proxy;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;
using System.Threading.Tasks;
using System;
using System.Net.Http;
using System.Net;
using System.Threading;
using Moq.Protected;

namespace Microsoft.AspNetCore.SpaServices.Extensions.Tests;

public class SpaProxyTests
{
    private static (HttpContext, HttpClient) GetHttpContextAndClient(string path, string queryString, Action<HttpRequestMessage> callback)
    {
        var messageHandler = new Mock<HttpMessageHandler>();
        messageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                                        ItExpr.IsAny<HttpRequestMessage>(),
                                        ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, c) => callback(req))
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("Test")
            });

        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.QueryString = new QueryString(queryString);
        context.Request.Method = "GET";

        return (context, new HttpClient(messageHandler.Object));
    }

    [Theory]
    [InlineData("http://localhost:3000", "", "", "http://localhost:3000/")]
    [InlineData("http://localhost:3000", "", "?a=b", "http://localhost:3000/?a=b")]
    [InlineData("http://localhost:3000/", "", "?a=b", "http://localhost:3000/?a=b")]
    [InlineData("http://localhost:3000", "/test", "?a=b", "http://localhost:3000/test?a=b")]
    [InlineData("http://localhost:3000/", "/test", "?a=b", "http://localhost:3000/test?a=b")]
    [InlineData("http://localhost:3000/spa", "/test", "?a=b", "http://localhost:3000/spa/test?a=b")]
    [InlineData("http://localhost:3000/spa/", "/test", "?a=b", "http://localhost:3000/spa/test?a=b")]
    [InlineData("http://localhost:3000", "///test", "?a=b", "http://localhost:3000///test?a=b")]
    [InlineData("http://localhost:3000/", "///test", "?a=b", "http://localhost:3000///test?a=b")]
    [InlineData("http://localhost:3000/spa", "///test", "?a=b", "http://localhost:3000/spa///test?a=b")]
    [InlineData("http://localhost:3000/spa/", "///test", "?a=b", "http://localhost:3000/spa///test?a=b")]
    public async Task PerformProxyRequest_TestUrlCombination(string baseUrl, string path, string queryString, string expected)
    {
        HttpRequestMessage forwardedRequestMessage = null;
        var (context, httpClient) = GetHttpContextAndClient(path, queryString, (req) => forwardedRequestMessage = req);
        var baseUriTask = Task.FromResult(new Uri(baseUrl));
        var res = await SpaProxy.PerformProxyRequest(context, httpClient, baseUriTask, CancellationToken.None, true);
        Assert.Equal(expected, forwardedRequestMessage.RequestUri.ToString());
    }

    [Fact]
    public async Task PerformProxyRequest_CancelsRequestWhenRequestAborted()
    {
        using var requestAbortedCts = new CancellationTokenSource();
        CancellationToken capturedToken = default;
        var sendStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var sendTcs = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);

        var messageHandler = new Mock<HttpMessageHandler>();
        messageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                                        ItExpr.IsAny<HttpRequestMessage>(),
                                        ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, ct) =>
            {
                capturedToken = ct;
                ct.Register(() => sendTcs.TrySetCanceled(ct));
                sendStarted.SetResult();
            })
            .Returns(sendTcs.Task);

        var context = new DefaultHttpContext();
        context.Request.Path = "/test";
        context.Request.Method = "GET";
        context.RequestAborted = requestAbortedCts.Token;

        var httpClient = new HttpClient(messageHandler.Object) { Timeout = Timeout.InfiniteTimeSpan };
        var baseUriTask = Task.FromResult(new Uri("http://localhost:3000"));

        var proxyTask = SpaProxy.PerformProxyRequest(context, httpClient, baseUriTask, CancellationToken.None, true);

        // Wait for the proxy to start the request
        await sendStarted.Task;

        Assert.True(capturedToken.CanBeCanceled);
        Assert.False(capturedToken.IsCancellationRequested);

        // Simulate client disconnect
        requestAbortedCts.Cancel();

        Assert.True(capturedToken.IsCancellationRequested);

        var result = await proxyTask;
        Assert.True(result);
    }
}
