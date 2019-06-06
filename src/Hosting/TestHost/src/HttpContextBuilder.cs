// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using static Microsoft.AspNetCore.Hosting.Internal.HostingApplication;

namespace Microsoft.AspNetCore.TestHost
{
    internal class HttpContextBuilder : IHttpBodyControlFeature
    {
        private readonly IHttpApplication<Context> _application;
        private readonly bool _preserveExecutionContext;
        private readonly HttpContext _httpContext;
        
        private readonly TaskCompletionSource<HttpContext> _responseTcs = new TaskCompletionSource<HttpContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly ResponseStream _responseStream;
        private readonly ResponseFeature _responseFeature;
        private readonly RequestLifetimeFeature _requestLifetimeFeature = new RequestLifetimeFeature();
        private readonly ResponseTrailersFeature _responseTrailersFeature = new ResponseTrailersFeature();
        private bool _pipelineFinished;
        private bool _returningResponse;
        private Context _testContext;
        private Action<HttpContext> _responseReadCompleteCallback;

        internal HttpContextBuilder(IHttpApplication<Context> application, bool allowSynchronousIO, bool preserveExecutionContext)
        {
            _application = application ?? throw new ArgumentNullException(nameof(application));
            AllowSynchronousIO = allowSynchronousIO;
            _preserveExecutionContext = preserveExecutionContext;
            _httpContext = new DefaultHttpContext();
            _responseFeature = new ResponseFeature(Abort);

            var request = _httpContext.Request;
            request.Protocol = "HTTP/1.1";
            request.Method = HttpMethods.Get;

            _httpContext.Features.Set<IHttpBodyControlFeature>(this);
            _httpContext.Features.Set<IHttpResponseFeature>(_responseFeature);
            _httpContext.Features.Set<IHttpResponseStartFeature>(_responseFeature);
            _httpContext.Features.Set<IHttpRequestLifetimeFeature>(_requestLifetimeFeature);
            _httpContext.Features.Set<IHttpResponseTrailersFeature>(_responseTrailersFeature);

            _responseStream = new ResponseStream(ReturnResponseMessageAsync, AbortRequest, () => AllowSynchronousIO, () => _responseReadCompleteCallback?.Invoke(_httpContext));
            _responseFeature.Body = _responseStream;
        }

        public bool AllowSynchronousIO { get; set; }

        internal void Configure(Action<HttpContext> configureContext)
        {
            if (configureContext == null)
            {
                throw new ArgumentNullException(nameof(configureContext));
            }

            configureContext(_httpContext);
        }

        internal void RegisterResponseReadCompleteCallback(Action<HttpContext> responseReadCompleteCallback)
        {
            _responseReadCompleteCallback = responseReadCompleteCallback;
        }

        /// <summary>
        /// Start processing the request.
        /// </summary>
        /// <returns></returns>
        internal Task<HttpContext> SendAsync(CancellationToken cancellationToken)
        {
            var registration = cancellationToken.Register(AbortRequest);

            // Everything inside this function happens in the SERVER's execution context (unless PreserveExecutionContext is true)
            async Task RunRequestAsync()
            {
                // This will configure IHttpContextAccessor so it needs to happen INSIDE this function,
                // since we are now inside the Server's execution context. If it happens outside this cont
                // it will be lost when we abandon the execution context.
                _testContext = _application.CreateContext(_httpContext.Features);

                try
                {
                    await _application.ProcessRequestAsync(_testContext);
                    await CompleteResponseAsync();
                    _application.DisposeContext(_testContext, exception: null);
                }
                catch (Exception ex)
                {
                    Abort(ex);
                    _application.DisposeContext(_testContext, ex);
                }
                finally
                {
                    registration.Dispose();
                }
            }

            // Async offload, don't let the test code block the caller.
            if (_preserveExecutionContext)
            {
                _ = Task.Factory.StartNew(RunRequestAsync);
            }
            else
            {
                ThreadPool.UnsafeQueueUserWorkItem(_ =>
                {
                    _ = RunRequestAsync();
                }, null);
            }

            return _responseTcs.Task;
        }

        internal void AbortRequest()
        {
            if (!_pipelineFinished)
            {
                _requestLifetimeFeature.Abort();
            }
            _responseStream.CompleteWrites();
        }

        internal async Task CompleteResponseAsync()
        {
            _pipelineFinished = true;
            await ReturnResponseMessageAsync();
            _responseStream.CompleteWrites();
            await _responseFeature.FireOnResponseCompletedAsync();
        }

        internal async Task ReturnResponseMessageAsync()
        {
            // Check if the response is already returning because the TrySetResult below could happen a bit late
            // (as it happens on a different thread) by which point the CompleteResponseAsync could run and calls this
            // method again.
            if (!_returningResponse)
            {
                _returningResponse = true;

                try
                {
                    await _responseFeature.FireOnSendingHeadersAsync();
                }
                catch (Exception ex)
                {
                    Abort(ex);
                    return;
                }

                // Copy the feature collection so we're not multi-threading on the same collection.
                var newFeatures = new FeatureCollection();
                foreach (var pair in _httpContext.Features)
                {
                    newFeatures[pair.Key] = pair.Value;
                }
                _responseTcs.TrySetResult(new DefaultHttpContext(newFeatures));
            }
        }

        internal void Abort(Exception exception)
        {
            _pipelineFinished = true;
            _responseStream.Abort(exception);
            _responseTcs.TrySetException(exception);
        }
    }
}
