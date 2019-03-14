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

        public override async Task<bool> ProcessRequestAsync()
        {
            InitializeContext();

            var context = default(TContext);
            var success = true;

            try
            {
                context = _application.CreateContext(this);
                await _application.ProcessRequestAsync(context);
                // TODO Verification of Response
                //if (Volatile.Read(ref _requestAborted) == 0)
                //{
                //    VerifyResponseContentLength();
                //}
            }
            catch (Exception ex)
            {
                ReportApplicationError(ex);
                success = false;
            }
            finally
            {
                _streams.Stop();

                if (!HasResponseStarted && _applicationException == null && _onStarting != null)
                {
                    await FireOnStarting();
                    // Dispose
                }

                if (_onCompleted != null)
                {
                    await FireOnCompleted();
                }
            }

            if (Volatile.Read(ref _requestAborted) == 0)
            {
                await ProduceEnd();
            }
            else if (!HasResponseStarted)
            {
                // If the request was aborted and no response was sent, there's no
                // meaningful status code to log.
                StatusCode = 0;
                success = false;
            }

            try
            {
                _application.DisposeContext(context, _applicationException);
            }
            catch (Exception ex)
            {
                // TODO Log this
                _applicationException = _applicationException ?? ex;
                success = false;
            }
            finally
            {
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
            return success;
        }
    }
}
