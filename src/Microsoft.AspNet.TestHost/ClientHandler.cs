// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Http.Internal;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.TestHost
{
    /// <summary>
    /// This adapts HttpRequestMessages to ASP.NET requests, dispatches them through the pipeline, and returns the
    /// associated HttpResponseMessage.
    /// </summary>
    public class ClientHandler : HttpMessageHandler
    {
        private readonly Func<IFeatureCollection, Task> _next;
        private readonly PathString _pathBase;

        /// <summary>
        /// Create a new handler.
        /// </summary>
        /// <param name="next">The pipeline entry point.</param>
        public ClientHandler([NotNull] Func<IFeatureCollection, Task> next, PathString pathBase)
        {
            _next = next;

            // PathString.StartsWithSegments that we use below requires the base path to not end in a slash.
            if (pathBase.HasValue && pathBase.Value.EndsWith("/"))
            {
                pathBase = new PathString(pathBase.Value.Substring(0, pathBase.Value.Length - 1));
            }
            _pathBase = pathBase;
        }

        /// <summary>
        /// This adapts HttpRequestMessages to ASP.NET requests, dispatches them through the pipeline, and returns the
        /// associated HttpResponseMessage.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task<HttpResponseMessage> SendAsync(
            [NotNull] HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var state = new RequestState(request, _pathBase, cancellationToken);
            var requestContent = request.Content ?? new StreamContent(Stream.Null);
            var body = await requestContent.ReadAsStreamAsync();
            if (body.CanSeek)
            {
                // This body may have been consumed before, rewind it.
                body.Seek(0, SeekOrigin.Begin);
            }
            state.HttpContext.Request.Body = body;
            var registration = cancellationToken.Register(state.Abort);

            // Async offload, don't let the test code block the caller.
            var offload = Task.Factory.StartNew(async () =>
                {
                    try
                    {
                        await _next(state.FeatureCollection);
                        state.CompleteResponse();
                    }
                    catch (Exception ex)
                    {
                        state.Abort(ex);
                    }
                    finally
                    {
                        registration.Dispose();
                        state.Dispose();
                    }
                });

            return await state.ResponseTask;
        }

        private class RequestState : IDisposable
        {
            private readonly HttpRequestMessage _request;
            private TaskCompletionSource<HttpResponseMessage> _responseTcs;
            private ResponseStream _responseStream;
            private ResponseFeature _responseFeature;

            internal RequestState(HttpRequestMessage request, PathString pathBase, CancellationToken cancellationToken)
            {
                _request = request;
                _responseTcs = new TaskCompletionSource<HttpResponseMessage>();

                if (request.RequestUri.IsDefaultPort)
                {
                    request.Headers.Host = request.RequestUri.Host;
                }
                else
                {
                    request.Headers.Host = request.RequestUri.GetComponents(UriComponents.HostAndPort, UriFormat.UriEscaped);
                }

                FeatureCollection = new FeatureCollection();
                HttpContext = new DefaultHttpContext(FeatureCollection);
                HttpContext.SetFeature<IHttpRequestFeature>(new RequestFeature());
                _responseFeature = new ResponseFeature();
                HttpContext.SetFeature<IHttpResponseFeature>(_responseFeature);
                var serverRequest = HttpContext.Request;
                serverRequest.Protocol = "HTTP/" + request.Version.ToString(2);
                serverRequest.Scheme = request.RequestUri.Scheme;
                serverRequest.Method = request.Method.ToString();

                var fullPath = PathString.FromUriComponent(request.RequestUri);
                PathString remainder;
                if (fullPath.StartsWithSegments(pathBase, out remainder))
                {
                    serverRequest.PathBase = pathBase;
                    serverRequest.Path = remainder;
                }
                else
                {
                    serverRequest.PathBase = PathString.Empty;
                    serverRequest.Path = fullPath;
                }

                serverRequest.QueryString = QueryString.FromUriComponent(request.RequestUri);
                // TODO: serverRequest.CallCancelled = cancellationToken;

                foreach (var header in request.Headers)
                {
                    serverRequest.Headers.AppendValues(header.Key, header.Value.ToArray());
                }
                var requestContent = request.Content;
                if (requestContent != null)
                {
                    foreach (var header in request.Content.Headers)
                    {
                        serverRequest.Headers.AppendValues(header.Key, header.Value.ToArray());
                    }
                }

                _responseStream = new ResponseStream(CompleteResponse);
                HttpContext.Response.Body = _responseStream;
                HttpContext.Response.StatusCode = 200;
            }

            public HttpContext HttpContext { get; private set; }

            public IFeatureCollection FeatureCollection { get; private set; }

            public Task<HttpResponseMessage> ResponseTask
            {
                get { return _responseTcs.Task; }
            }

            internal void CompleteResponse()
            {
                if (!_responseTcs.Task.IsCompleted)
                {
                    var response = GenerateResponse();
                    _responseFeature.FireOnResponseCompleted();
                    // Dispatch, as TrySetResult will synchronously execute the waiters callback and block our Write.
                    Task.Factory.StartNew(() => _responseTcs.TrySetResult(response));
                }
            }

            [SuppressMessage("Microsoft.Reliability", "CA2000:DisposeObjectsBeforeLosingScope",
                Justification = "HttpResposneMessage must be returned to the caller.")]
            internal HttpResponseMessage GenerateResponse()
            {
                _responseFeature.FireOnSendingHeaders();

                var response = new HttpResponseMessage();
                response.StatusCode = (HttpStatusCode)HttpContext.Response.StatusCode;
                response.ReasonPhrase = HttpContext.GetFeature<IHttpResponseFeature>().ReasonPhrase;
                response.RequestMessage = _request;
                // response.Version = owinResponse.Protocol;

                response.Content = new StreamContent(_responseStream);

                foreach (var header in HttpContext.Response.Headers)
                {
                    if (!response.Headers.TryAddWithoutValidation(header.Key, header.Value))
                    {
                        bool success = response.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                        Contract.Assert(success, "Bad header");
                    }
                }
                return response;
            }

            internal void Abort()
            {
                Abort(new OperationCanceledException());
            }

            internal void Abort(Exception exception)
            {
                _responseStream.Abort(exception);
                _responseTcs.TrySetException(exception);
            }

            public void Dispose()
            {
                _responseStream.Dispose();
                // Do not dispose the request, that will be disposed by the caller.
            }
        }
    }
}
