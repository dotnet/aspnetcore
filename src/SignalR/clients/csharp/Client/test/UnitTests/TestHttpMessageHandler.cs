// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.SignalR.Client.Tests;

delegate Task<HttpResponseMessage> RequestDelegate(HttpRequestMessage requestMessage, CancellationToken cancellationToken);

public class TestHttpMessageHandler : HttpMessageHandler
{
    private readonly List<HttpRequestMessage> _receivedRequests = new List<HttpRequestMessage>();
    private RequestDelegate _app;
    private readonly ILogger _logger;

    private readonly List<Func<RequestDelegate, RequestDelegate>> _middleware = new List<Func<RequestDelegate, RequestDelegate>>();

    public bool Disposed { get; private set; }

    public IReadOnlyList<HttpRequestMessage> ReceivedRequests
    {
        get
        {
            lock (_receivedRequests)
            {
                return _receivedRequests.ToArray();
            }
        }
    }

    public TestHttpMessageHandler(ILoggerFactory loggerFactory, bool autoNegotiate = true, bool handleFirstPoll = true)
    {
        _logger = loggerFactory?.CreateLogger<TestHttpMessageHandler>() ?? NullLoggerFactory.Instance.CreateLogger<TestHttpMessageHandler>();

        if (autoNegotiate)
        {
            OnNegotiate((_, cancellationToken) => ResponseUtils.CreateResponse(HttpStatusCode.OK, ResponseUtils.CreateNegotiationContent()));
        }

        if (handleFirstPoll)
        {
            var firstPoll = true;
            OnRequest(async (request, next, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (ResponseUtils.IsLongPollRequest(request) && firstPoll)
                {
                    firstPoll = false;
                    return ResponseUtils.CreateResponse(HttpStatusCode.OK);
                }
                else
                {
                    return await next();
                }
            });
        }
    }

    public TestHttpMessageHandler(bool autoNegotiate = true, bool handleFirstPoll = true)
        : this(NullLoggerFactory.Instance, autoNegotiate, handleFirstPoll)
    {
    }

    protected override void Dispose(bool disposing)
    {
        Disposed = true;
        base.Dispose(disposing);
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Calling handlers for a '{Method}' going to '{Url}'.", request.Method, request.RequestUri);
        await Task.Yield();

        lock (_receivedRequests)
        {
            _receivedRequests.Add(request);

            if (_app == null)
            {
                _middleware.Reverse();
                RequestDelegate handler = BaseHandler;
                foreach (var middleware in _middleware)
                {
                    handler = middleware(handler);
                }

                _app = handler;
            }
        }

        return await _app(request, cancellationToken);
    }

    public static TestHttpMessageHandler CreateDefault()
    {
        var testHttpMessageHandler = new TestHttpMessageHandler();

        var deleteCts = new CancellationTokenSource();

        testHttpMessageHandler.OnSocketSend((_, __) => ResponseUtils.CreateResponse(HttpStatusCode.Accepted));
        testHttpMessageHandler.OnLongPoll(async cancellationToken =>
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, deleteCts.Token);

            // Just block until canceled
            var tcs = new TaskCompletionSource();
            using (cts.Token.Register(() => tcs.TrySetResult()))
            {
                await tcs.Task;
            }
            return ResponseUtils.CreateResponse(HttpStatusCode.NoContent);
        });
        testHttpMessageHandler.OnRequest((request, next, cancellationToken) =>
        {
            if (request.Method.Equals(HttpMethod.Delete) && request.RequestUri.PathAndQuery.Contains("id="))
            {
                deleteCts.Cancel();
                return Task.FromResult(ResponseUtils.CreateResponse(HttpStatusCode.Accepted));
            }

            return next();
        });

        return testHttpMessageHandler;
    }

    public void OnRequest(Func<HttpRequestMessage, Func<Task<HttpResponseMessage>>, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        void OnRequestCore(Func<RequestDelegate, RequestDelegate> middleware)
        {
            _middleware.Add(middleware);
        }

        OnRequestCore(next =>
        {
            return (request, cancellationToken) =>
            {
                return handler(request, () => next(request, cancellationToken), cancellationToken);
            };
        });
    }

    public void OnGet(string pathAndQuery, Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) => OnRequest(HttpMethod.Get, pathAndQuery, handler);
    public void OnPost(string pathAndQuery, Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) => OnRequest(HttpMethod.Post, pathAndQuery, handler);
    public void OnPut(string pathAndQuery, Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) => OnRequest(HttpMethod.Put, pathAndQuery, handler);
    public void OnDelete(string pathAndQuery, Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) => OnRequest(HttpMethod.Delete, pathAndQuery, handler);
    public void OnHead(string pathAndQuery, Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) => OnRequest(HttpMethod.Head, pathAndQuery, handler);
    public void OnOptions(string pathAndQuery, Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) => OnRequest(HttpMethod.Options, pathAndQuery, handler);
    public void OnTrace(string pathAndQuery, Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) => OnRequest(HttpMethod.Trace, pathAndQuery, handler);

    public void OnRequest(HttpMethod method, string pathAndQuery, Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        OnRequest((request, next, cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (request.Method.Equals(method) && string.Equals(request.RequestUri.PathAndQuery, pathAndQuery))
            {
                return handler(request, cancellationToken);
            }
            else
            {
                return next();
            }
        });
    }

    public void OnNegotiate(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> handler) => OnNegotiate((req, cancellationToken) => Task.FromResult(handler(req, cancellationToken)));

    public void OnNegotiate(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        OnRequest((request, next, cancellationToken) =>
        {
            if (ResponseUtils.IsNegotiateRequest(request))
            {
                return handler(request, cancellationToken);
            }
            else
            {
                return next();
            }
        });
    }

    public void OnLongPollDelete(Func<CancellationToken, HttpResponseMessage> handler) => OnLongPollDelete((cancellationToken) => Task.FromResult(handler(cancellationToken)));

    public void OnLongPollDelete(Func<CancellationToken, Task<HttpResponseMessage>> handler)
    {
        OnRequest((request, next, cancellationToken) =>
        {
            if (ResponseUtils.IsLongPollDeleteRequest(request))
            {
                return handler(cancellationToken);
            }
            else
            {
                return next();
            }
        });
    }

    public void OnLongPoll(Func<CancellationToken, HttpResponseMessage> handler) => OnLongPoll(cancellationToken => Task.FromResult(handler(cancellationToken)));

    public void OnLongPoll(Func<CancellationToken, Task<HttpResponseMessage>> handler)
    {
        OnLongPoll((request, token) => handler(token));
    }

    public void OnLongPoll(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> handler)
    {
        OnLongPoll((request, token) => Task.FromResult(handler(request, token)));
    }

    public void OnLongPoll(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        OnRequest((request, next, cancellationToken) =>
        {
            if (ResponseUtils.IsLongPollRequest(request))
            {
                return handler(request, cancellationToken);
            }
            else
            {
                return next();
            }
        });
    }

    public void OnSocketSend(Func<byte[], CancellationToken, HttpResponseMessage> handler) => OnSocketSend((data, cancellationToken) => Task.FromResult(handler(data, cancellationToken)));

    public void OnSocketSend(Func<byte[], CancellationToken, Task<HttpResponseMessage>> handler)
    {
        OnRequest(async (request, next, cancellationToken) =>
        {
            if (ResponseUtils.IsSocketSendRequest(request))
            {
                var data = await request.Content.ReadAsByteArrayAsync();
                return await handler(data, cancellationToken);
            }
            else
            {
                return await next();
            }
        });
    }

    private Task<HttpResponseMessage> BaseHandler(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromException<HttpResponseMessage>(new InvalidOperationException($"Http endpoint not implemented: {request.Method} {request.RequestUri}"));
    }
}
