// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    internal static class LogHelper
    {
        internal static ILogger CreateLogger(ILoggerFactory factory, Type type)
        {
            if (factory == null)
            {
                return null;
            }

            return factory.CreateLogger(type.FullName);
        }

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

        internal static void LogWarning(ILogger logger, string data)
        {
            if (logger == null)
            {
                Debug.WriteLine(data);
            }
            else
            {
                logger.LogWarning(data);
            }
        }

        internal static void LogDebug(ILogger logger, string data)
        {
            if (logger == null)
            {
                Debug.WriteLine(data);
            }
            else
            {
                logger.LogDebug(data);
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
                Debug.WriteLine(location + Environment.NewLine + exception.ToString());
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
                Debug.WriteLine(location + Environment.NewLine + exception.ToString());
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
