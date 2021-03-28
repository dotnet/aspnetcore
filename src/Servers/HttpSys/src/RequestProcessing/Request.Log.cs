// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    internal partial class Request
    {
        private static class Log
        {
            private static readonly Action<ILogger, Exception?> _errorInReadingCertificate =
                LoggerMessage.Define(LogLevel.Debug, LoggerEventIds.ErrorInReadingCertificate, "An error occurred reading the client certificate.");

            public static void ErrorInReadingCertificate(ILogger logger, Exception exception)
            {
                _errorInReadingCertificate(logger, exception);
            }
        }
    }
}
