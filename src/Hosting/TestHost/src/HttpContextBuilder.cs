// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.TestHost
{
    internal class HttpContextBuilder : IHttpBodyControlFeature, IHttpResetFeature
    {
        private readonly ApplicationWrapper _application;
        private readonly bool _preserveExecutionContext;
        private readonly HttpContext _httpContext;
        
        private readonly TaskCompletionSource<HttpContext> _responseTcs = new TaskCompletionSource<HttpContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly ResponseBodyReaderStream _responseReaderStream;
        private readonly ResponseBodyPipeWriter _responsePipeWriter;
        private readonly ResponseFeature _responseFeature;
        private readonly RequestLifetimeFeature _requestLifetimeFeature;
        private readonly ResponseTrailersFeature _responseTrailersFeature = new ResponseTrailersFeature();
        private bool _pipelineFinished;
        private bool _returningResponse;
        private object? _testContext;
        private readonly Pipe _requestPipe;

        private Action<HttpContext>? _responseReadCompleteCallback;
        private Task? _sendRequestStreamTask;

        internal HttpContextBuilder(ApplicationWrapper application, bool allowSynchronousIO, bool preserveExecutionContext)
        {
            _application = application ?? throw new ArgumentNullException(nameof(application));
            AllowSynchronousIO = allowSynchronousIO;
            _preserveExecutionContext = preserveExecutionContext;
            _httpContext = new DefaultHttpContext();
            _responseFeature = new ResponseFeature(Abort);
            _requestLifetimeFeature = new RequestLifetimeFeature(Abort);

            var request = _httpContext.Request;
            request.Protocol = HttpProtocol.Http11;
            request.Method = HttpMethods.Get;

            _requestPipe = new Pipe();

            var responsePipe = new Pipe();
            _responseReaderStream = new ResponseBodyReaderStream(responsePipe, ClientInitiatedAbort, ResponseBodyReadComplete);
            _responsePipeWriter = new ResponseBodyPipeWriter(responsePipe, ReturnResponseMessageAsync);
            _responseFeature.Body = new ResponseBodyWriterStream(_responsePipeWriter, () => AllowSynchronousIO);
            _responseFeature.BodyWriter = _responsePipeWriter;

            _httpContext.Features.Set<IHttpBodyControlFeature>(this);
            _httpContext.Features.Set<IHttpResponseFeature>(_responseFeature);
            _httpContext.Features.Set<IHttpResponseBodyFeature>(_responseFeature);
            _httpContext.Features.Set<IHttpRequestLifetimeFeature>(_requestLifetimeFeature);
            _httpContext.Features.Set<IHttpResponseTrailersFeature>(_responseTrailersFeature);
        }

        public bool AllowSynchronousIO { get; set; }

        internal void Configure(Action<HttpContext, PipeReader> configureContext)
        {
            if (configureContext == null)
            {
                throw new ArgumentNullException(nameof(configureContext));
            }

            configureContext(_httpContext, _requestPipe.Reader);
        }

        internal void SendRequestStream(Func<PipeWriter, Task> sendRequestStream)
        {
            if (sendRequestStream == null)
            {
                throw new ArgumentNullException(nameof(sendRequestStream));
            }

            _sendRequestStreamTask = sendRequestStream(_requestPipe.Writer);
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
            var registration = cancellationToken.Register(ClientInitiatedAbort);

            // Everything inside this function happens in the SERVER's execution context (unless PreserveExecutionContext is true)
            async Task RunRequestAsync()
            {
                // HTTP/2 specific features must be added after the request has been configured.
                if (HttpProtocol.IsHttp2(_httpContext.Request.Protocol))
                {
                    _httpContext.Features.Set<IHttpResetFeature>(this);
                }

                // This will configure IHttpContextAccessor so it needs to happen INSIDE this function,
                // since we are now inside the Server's execution context. If it happens outside this cont
                // it will be lost when we abandon the execution context.
                _testContext = _application.CreateContext(_httpContext.Features);
                try
                {
                    await _application.ProcessRequestAsync(_testContext);

                    // Determine whether request body was complete when the delegate exited.
                    // This could throw an error if there was a pending server read. Needs to
                    // happen before completing the response so the response returns the error.
                    var requestBodyInProgress = RequestBodyReadInProgress();
                    if (requestBodyInProgress)
                    {
                        // If request is still in progress then abort it.
                        CancelRequestBody();
                    }

                    // Matches Kestrel server: response is completed before request is drained
                    await CompleteResponseAsync();

                    if (!requestBodyInProgress)
                    {
                        // Writer was already completed in send request callback.
                        await _requestPipe.Reader.CompleteAsync();

                        // Don't wait for request to drain. It could block indefinitely. In a real server
                        // we would wait for a timeout and then kill the socket.
                        // Potential future improvement: add logging that the request timed out
                    }

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
                _ = Task.Factory.StartNew(RunRequestAsync, default, TaskCreationOptions.LongRunning, TaskScheduler.Default);
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

        // Triggered by request CancellationToken canceling or response stream Disposal.
        internal void ClientInitiatedAbort()
        {
            if (!_pipelineFinished)
            {
                // We don't want to trigger the token for already completed responses.
                _requestLifetimeFeature.Cancel();
            }

            // Writes will still succeed, the app will only get an error if they check the CT.
            _responseReaderStream.Abort(new IOException("The client aborted the request."));

            // Cancel any pending request async activity when the client aborts a duplex
            // streaming scenario by disposing the HttpResponseMessage.
            CancelRequestBody();
        }

        private void ResponseBodyReadComplete()
        {
            _responseReadCompleteCallback?.Invoke(_httpContext);
        }

        private bool RequestBodyReadInProgress()
        {
            try
            {
                return !_requestPipe.Reader.TryRead(out var result) || !result.IsCompleted;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("An error occurred when completing the request. Request delegate may have finished while there is a pending read of the request body.", ex);
            }
        }

        internal async Task CompleteResponseAsync()
        {
            _pipelineFinished = true;
            await ReturnResponseMessageAsync();
            _responsePipeWriter.Complete();
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
                var serverResponseFeature = _httpContext.Features.Get<IHttpResponseFeature>()!;
                // The client gets a deep copy of this so they can interact with the body stream independently of the server.
                var clientResponseFeature = new HttpResponseFeature()
                {
                    StatusCode = serverResponseFeature.StatusCode,
                    ReasonPhrase = serverResponseFeature.ReasonPhrase,
                    Headers = serverResponseFeature.Headers,
                    Body = _responseReaderStream
                };
                newFeatures.Set<IHttpResponseFeature>(clientResponseFeature);
                newFeatures.Set<IHttpResponseBodyFeature>(new StreamResponseBodyFeature(_responseReaderStream));
                _responseTcs.TrySetResult(new DefaultHttpContext(newFeatures));
            }
        }

        internal void Abort(Exception exception)
        {
            _responsePipeWriter.Abort(exception);
            _responseReaderStream.Abort(exception);
            _requestLifetimeFeature.Cancel();
            _responseTcs.TrySetException(exception);
            CancelRequestBody();
        }

        private void CancelRequestBody()
        {
            _requestPipe.Writer.CancelPendingFlush();
            _requestPipe.Reader.CancelPendingRead();
        }

        void IHttpResetFeature.Reset(int errorCode)
        {
            Abort(new HttpResetTestException(errorCode));
        }
    }
}
