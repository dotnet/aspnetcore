// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Microsoft.Net.Http.Server
{
    internal static class LogHelper
    {
        internal static void LogInfo(ILogger logger, string data)
        {
            if (logger == null)
            {
                Debug.WriteLine(data);
            }
            else
            {
                logger.LogInformation(data);
            }
        }

        internal static void LogDebug(ILogger logger, string location, string data)
        {
            if (logger == null)
            {
                Debug.WriteLine(data);
            }
            else
            {
                logger.LogDebug(location + "; " + data);
            }
        }

        internal static void LogDebug(ILogger logger, string location, Exception exception)
        {
            if (logger == null)
            {
                Debug.WriteLine(exception);
            }
            else
            {
                logger.LogDebug(0, exception, location);
            }
        }

        internal static void LogException(ILogger logger, string location, Exception exception)
        {
            if (logger == null)
            {
                Debug.WriteLine(exception);
            }
            else
            {
                logger.LogError(0, exception, location);
            }
        }

        internal static void LogError(ILogger logger, string location, string message)
        {
            if (logger == null)
            {
                Debug.WriteLine(message);
            }
            else
            {
                logger.LogError(location + "; " + message);
            }
        }
    }
}
