// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;

namespace Microsoft.AspNetCore.Server.IISIntegration
{
    internal class IISHttpContextOfT<TContext> : IISHttpContext
    {
        private readonly IHttpApplication<TContext> _application;

        public IISHttpContextOfT(MemoryPool memoryPool, IHttpApplication<TContext> application, IntPtr pHttpContext, IISOptions options)
            : base(memoryPool, pHttpContext, options)
        {
            _application = application;
        }

        public override async Task<bool> ProcessRequestAsync()
        {
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
                // The app is finished and there should be nobody writing to the response pipe
                Output.Dispose();

                if (_writingTask != null)
                {
                    await _writingTask;
                }

                // The app is finished and there should be nobody reading from the request pipe
                Input.Reader.Complete();

                if (_readingTask != null)
                {
                    await _readingTask;
                }
            }
            return success;
        }
    }
}
