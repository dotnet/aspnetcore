// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Mvc.Logging
{
    public static class ValidateAntiforgeryTokenAuthorizationFilterLoggerExtensions
    {
        private static readonly Action<ILogger, string, Exception> _antiforgeryTokenInvalid;

        static ValidateAntiforgeryTokenAuthorizationFilterLoggerExtensions()
        {
            _antiforgeryTokenInvalid = LoggerMessage.Define<string>(
                LogLevel.Information,
                1,
                "Antiforgery token validation failed. {Message}");
        }

        public static void AntiforgeryTokenInvalid(this ILogger logger, string message, Exception exception)
        {
            _antiforgeryTokenInvalid(logger, message, exception);
        }
    }
}
