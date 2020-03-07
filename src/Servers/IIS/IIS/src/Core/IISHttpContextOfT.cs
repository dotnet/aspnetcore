// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.IIS.Core
{
    internal class IISHttpContextOfT<TContext> : IISHttpContext
    {
        private readonly IHttpApplication<TContext> _application;

        public IISHttpContextOfT(MemoryPool<byte> memoryPool, IHttpApplication<TContext> application, IntPtr pInProcessHandler, IISServerOptions options, IISHttpServer server, ILogger logger)
            : base(memoryPool, pInProcessHandler, options, server, logger)
        {
            _application = application;
        }

        public override async Task ProcessRequestAsync()
        {
            var context = default(TContext);
            var success = true;

            try
            {
                InitializeContext();

                try
                {
                    context = _application.CreateContext(this);

                    await _application.ProcessRequestAsync(context);
                }
                catch (BadHttpRequestException ex)
                {
                    SetBadRequestState(ex);
                    ReportApplicationError(ex);
                    success = false;
                }
                catch (Exception ex)
                {
                    ReportApplicationError(ex);
                    success = false;
                }

                await CompleteResponseBodyAsync();
                _streams.Stop();

                if (!HasResponseStarted && _applicationException == null && _onStarting != null)
                {
                    await FireOnStarting();
                    // Dispose
                }

                if (!_requestAborted)
                {
                    await ProduceEnd();
                }
                else if (!HasResponseStarted && _requestRejectedException == null)
                {
                    // If the request was aborted and no response was sent, there's no
                    // meaningful status code to log.
                    StatusCode = 0;
                    success = false;
                }

                // Complete response writer and request reader pipe sides
                _bodyOutput.Dispose();
                _bodyInputPipe?.Reader.Complete();

                // Allow writes to drain
                if (_writeBodyTask != null)
                {
                    await _writeBodyTask;
                }

                // Cancel all remaining IO, there might be reads pending if not entire request body was sent by client
                AsyncIO?.Dispose();

                if (_readBodyTask != null)
                {
                    await _readBodyTask;
                }
            }
            catch (Exception ex)
            {
                success = false;
                ReportApplicationError(ex);
            }
            finally
            {
                // We're done with anything that touches the request or response, unblock the client.
                PostCompletion(ConvertRequestCompletionResults(success));

                if (_onCompleted != null)
                {
                    await FireOnCompleted();
                }

                try
                {
                    _application.DisposeContext(context, _applicationException);
                }
                catch (Exception ex)
                {
                    ReportApplicationError(ex);
                }
            }
        }

        private static NativeMethods.REQUEST_NOTIFICATION_STATUS ConvertRequestCompletionResults(bool success)
        {
            return success ? NativeMethods.REQUEST_NOTIFICATION_STATUS.RQ_NOTIFICATION_CONTINUE
                           : NativeMethods.REQUEST_NOTIFICATION_STATUS.RQ_NOTIFICATION_FINISH_REQUEST;
        }
    }
}
