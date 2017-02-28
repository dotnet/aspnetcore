// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure
{
    public class InlineLoggingThreadPool : IThreadPool
    {
        private readonly IKestrelTrace _log;

        public InlineLoggingThreadPool(IKestrelTrace log)
        {
            _log = log;
        }

        public void Run(Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                _log.LogError(0, e, "InlineLoggingThreadPool.Run");
            }
        }

        public void Complete(TaskCompletionSource<object> tcs)
        {
            try
            {
                tcs.TrySetResult(null);
            }
            catch (Exception e)
            {
                _log.LogError(0, e, "InlineLoggingThreadPool.Complete");
            }
        }

        public void Cancel(TaskCompletionSource<object> tcs)
        {
            try
            {
                tcs.TrySetCanceled();
            }
            catch (Exception e)
            {
                _log.LogError(0, e, "InlineLoggingThreadPool.Cancel");
            }
        }

        public void Error(TaskCompletionSource<object> tcs, Exception ex)
        {
            try
            {
                tcs.TrySetException(ex);
            }
            catch (Exception e)
            {
                _log.LogError(0, e, "InlineLoggingThreadPool.Error");
            }
        }

        public void UnsafeRun(WaitCallback action, object state)
        {
            action(state);
        }

        public void Schedule(Action action)
        {
            Run(action);
        }
    }
}
