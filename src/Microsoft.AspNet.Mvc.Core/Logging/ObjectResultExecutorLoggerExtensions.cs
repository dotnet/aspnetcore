// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Mvc.Logging
{
    public static class ObjectResultExecutorLoggerExtensions
    {
        private static Action<ILogger, string, Exception> _objectResultExecuting;

        static ObjectResultExecutorLoggerExtensions()
        {
            _objectResultExecuting = LoggerMessage.Define<string>(
                LogLevel.Information,
                1,
                "Executing ObjectResult, writing value {Value}.");
        }

        public static void ObjectResultExecuting(this ILogger logger, object value)
        {
            _objectResultExecuting(logger, Convert.ToString(value), null);
        }
    }
}
