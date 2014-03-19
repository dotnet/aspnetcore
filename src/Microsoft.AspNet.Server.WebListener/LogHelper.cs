//------------------------------------------------------------------------------
// <copyright file="HttpListener.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNet.Logging;

namespace Microsoft.AspNet.Server.WebListener
{
    internal static class LogHelper
    {
        private static readonly Func<object, Exception, string> LogState =
            (state, error) => Convert.ToString(state, CultureInfo.CurrentCulture);

        private static readonly Func<object, Exception, string> LogStateAndError =
            (state, error) => string.Format(CultureInfo.CurrentCulture, "{0}\r\n{1}", state, error);

        internal static ILogger CreateLogger(ILoggerFactory factory, Type type)
        {
            if (factory == null)
            {
                return null;
            }

            return factory.Create(type.FullName);
        }

        internal static void LogInfo(ILogger logger, string data)
        {
            if (logger == null)
            {
                Debug.WriteLine(data);
            }
            else
            {
                logger.WriteInformation(data);
            }
        }

        internal static void LogVerbose(ILogger logger, string data)
        {
            if (logger == null)
            {
                Debug.WriteLine(data);
            }
            else
            {
                logger.WriteVerbose(data);
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
                logger.WriteError(location, exception);
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
                logger.WriteError(location + "; " + message);
            }
        }
    }
}
