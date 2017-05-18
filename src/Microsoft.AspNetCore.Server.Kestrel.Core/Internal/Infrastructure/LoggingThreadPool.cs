// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    public class LoggingThreadPool : IThreadPool
    {
        private readonly IKestrelTrace _log;

        private WaitCallback _runAction;

        public LoggingThreadPool(IKestrelTrace log)
        {
            _log = log;

            // Curry and capture log in closures once
            // The currying is done in functions of the same name to improve the
            // call stack for exceptions and profiling else it shows up as LoggingThreadPool.ctor>b__4_0
            // and you aren't sure which of the 3 functions was called.
            RunAction();
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

        public void Run(Action action)
        {
            ThreadPool.QueueUserWorkItem(_runAction, action);
        }

        public void UnsafeRun(WaitCallback action, object state)
        {
            ThreadPool.QueueUserWorkItem(action, state);
        }

        public void Schedule(Action<object> action, object state)
        {
            Run(() => action(state));
        }
    }
}