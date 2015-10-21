// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Mvc.Logging
{
    public static class LocalRedirectResultLoggerExtensions
    {
        private static Action<ILogger, string, Exception> _localRedirectResultExecuting;

        static LocalRedirectResultLoggerExtensions()
        {
            _localRedirectResultExecuting = LoggerMessage.Define<string>(
                LogLevel.Information,
                1,
                "Executing LocalRedirectResult, redirecting to {Destination}.");
        }

        public static void LocalRedirectResultExecuting(this ILogger logger, string destination)
        {
            _localRedirectResultExecuting(logger, destination, null);
        }
    }
}
