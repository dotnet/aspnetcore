// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.Mvc.Logging
{
    internal static class ContentResultLoggerExtensions
    {
        private static readonly Action<ILogger, string, Exception> _contentResultExecuting;

        static ContentResultLoggerExtensions()
        {
            _contentResultExecuting = LoggerMessage.Define<string>(
                LogLevel.Information,
                1,
                "Executing ContentResult with HTTP Response ContentType of {ContentType}");
        }

        public static void ContentResultExecuting(this ILogger logger, string contentType)
        {
            _contentResultExecuting(logger, contentType, null);
        }
    }
}
