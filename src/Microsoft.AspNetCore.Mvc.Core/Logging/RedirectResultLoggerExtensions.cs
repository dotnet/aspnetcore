// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Mvc.Logging
{
    internal static class RedirectResultLoggerExtensions
    {
        private static readonly Action<ILogger, string, Exception> _redirectResultExecuting;

        static RedirectResultLoggerExtensions()
        {
            _redirectResultExecuting = LoggerMessage.Define<string>(
                LogLevel.Information,
                1,
                "Executing RedirectResult, redirecting to {Destination}.");
        }

        public static void RedirectResultExecuting(this ILogger logger, string destination)
        {
            _redirectResultExecuting(logger, destination, null);
        }
    }
}
