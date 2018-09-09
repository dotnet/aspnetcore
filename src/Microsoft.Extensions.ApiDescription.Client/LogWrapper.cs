// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Build.Utilities;

namespace Microsoft.Extensions.ApiDescription.Client
{
    internal class LogWrapper : ILogWrapper
    {
        private readonly TaskLoggingHelper _log;

        public LogWrapper(TaskLoggingHelper log)
        {
            _log = log;
        }

        public void LogError(string message, params object[] messageArgs)
        {
            _log.LogError(message, messageArgs);
        }

        public void LogError(Exception exception, bool showStackTrace)
        {
            _log.LogErrorFromException(exception, showStackTrace);
        }

        public void LogInformational(string message, params object[] messageArgs)
        {
            _log.LogMessage(message, messageArgs);
        }

        public void LogWarning(string message, params object[] messageArgs)
        {
            _log.LogWarning(message, messageArgs);
        }
    }
}
