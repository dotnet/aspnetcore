// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure
{
    public class LoggingThreadPool : IThreadPool
    {
        private readonly IKestrelTrace _log;

        private WaitCallback _runAction;
        private WaitCallback _cancelTcs;
        private WaitCallback _completeTcs;

        public LoggingThreadPool(IKestrelTrace log)
        {
            _log = log;

            // Curry and capture log in closures once
            // The currying is done in functions of the same name to improve the 
            // call stack for exceptions and profiling else it shows up as LoggingThreadPool.ctor>b__4_0
            // and you aren't sure which of the 3 functions was called.
            RunAction();
            CompleteTcs();
            CancelTcs();
        }

        private void RunAction()
        {
            // Capture _log in a singleton closure
            _runAction = (o) =>
            {
                try
                {
                    ((Action)o)();
                }
                catch (Exception e)
                {
                    _log.LogError(0, e, "LoggingThreadPool.Run");
                }
            };
        }

        private void CompleteTcs()
        {
            // Capture _log in a singleton closure
            _completeTcs = (o) =>
            {
                try
                {
                    ((TaskCompletionSource<object>)o).TrySetResult(null);
                }
                catch (Exception e)
                {
                    _log.LogError(0, e, "LoggingThreadPool.Complete");
                }
            };
        }

        private void CancelTcs()
        {
            // Capture _log in a singleton closure
            _cancelTcs = (o) =>
            {
                try
                {
                    ((TaskCompletionSource<object>)o).TrySetCanceled();
                }
                catch (Exception e)
                {
                    _log.LogError(0, e, "LoggingThreadPool.Cancel");
                }
            };
        }

        public void Run(Action action)
        {
            ThreadPool.QueueUserWorkItem(_runAction, action);
        }

        public void UnsafeRun(WaitCallback action, object state)
        {
            ThreadPool.QueueUserWorkItem(action, state);
        }

        public void Complete(TaskCompletionSource<object> tcs)
        {
            ThreadPool.QueueUserWorkItem(_completeTcs, tcs);
        }

        public void Cancel(TaskCompletionSource<object> tcs)
        {
            ThreadPool.QueueUserWorkItem(_cancelTcs, tcs);
        }

        public void Error(TaskCompletionSource<object> tcs, Exception ex)
        {
            // ex and _log are closure captured 
            ThreadPool.QueueUserWorkItem((o) =>
            {
                try
                {
                    ((TaskCompletionSource<object>)o).TrySetException(ex);
                }
                catch (Exception e)
                {
                    _log.LogError(0, e, "LoggingThreadPool.Error");
                }
            }, tcs);
        }

        public void Schedule(Action action)
        {
            Run(action);
        }
    }
}