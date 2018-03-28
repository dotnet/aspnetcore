using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Client.Tests;

namespace Microsoft.AspNetCore.SignalR.Client.Tests
{
    public class TestHttpMessageHandler : HttpMessageHandler
    {
        private Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public TestHttpMessageHandler(bool autoNegotiate = true)
        {
            _handler = (request, cancellationToken) => BaseHandler(request, cancellationToken);

            if (autoNegotiate)
            {
                OnNegotiate((_, cancellationToken) => ResponseUtils.CreateResponse(HttpStatusCode.OK, ResponseUtils.CreateNegotiationContent()));
            }
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Yield();

            return await _handler(request, cancellationToken);
        }

        public static TestHttpMessageHandler CreateDefault()
        {
            var testHttpMessageHandler = new TestHttpMessageHandler();

            testHttpMessageHandler.OnSocketSend((_, __) => ResponseUtils.CreateResponse(HttpStatusCode.Accepted));
            testHttpMessageHandler.OnLongPoll(async cancellationToken =>
            {
                // Just block until canceled
                var tcs = new TaskCompletionSource<object>();
                using (cancellationToken.Register(() => tcs.TrySetResult(null)))
                {
                    await tcs.Task;
                }
                return ResponseUtils.CreateResponse(HttpStatusCode.NoContent);
            });

            return testHttpMessageHandler;
        }

        public void OnRequest(Func<HttpRequestMessage, Func<Task<HttpResponseMessage>>, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            var nextHandler = _handler;
            _handler = (request, cancellationToken) => handler(request, () => nextHandler(request, cancellationToken), cancellationToken);
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

        public void OnLongPoll(Func<CancellationToken, HttpResponseMessage> handler) => OnLongPoll(cancellationToken => Task.FromResult(handler(cancellationToken)));

        public void OnLongPoll(Func<CancellationToken, Task<HttpResponseMessage>> handler)
        {
            OnRequest((request, next, cancellationToken) =>
            {
                if (request.Method.Equals(HttpMethod.Get) && request.RequestUri.PathAndQuery.StartsWith("/?id="))
                {
                    return handler(cancellationToken);
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
                if (request.Method.Equals(HttpMethod.Post) && request.RequestUri.PathAndQuery.StartsWith("/?id="))
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
}
