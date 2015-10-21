// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Mvc.Logging
{
    internal static class RedirectToActionResultLoggerExtensions
    {
        private static readonly Action<ILogger, string, Exception> _redirectToActionResultExecuting;

        static RedirectToActionResultLoggerExtensions()
        {
            _redirectToActionResultExecuting = LoggerMessage.Define<string>(
                LogLevel.Information,
                1,
                "Executing RedirectResult, redirecting to {Destination}.");
        }

        public static void RedirectToActionResultExecuting(this ILogger logger, string destination)
        {
            _redirectToActionResultExecuting(logger, destination, null);
        }
    }
}
