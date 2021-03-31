// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.HttpSys.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    internal partial class MessagePump
    {
        private static class Log
        {
            private static readonly Action<ILogger, Exception?> _acceptError =
                LoggerMessage.Define(LogLevel.Error, LoggerEventIds.AcceptError, "Failed to accept a request.");

            private static readonly Action<ILogger, Exception?> _acceptErrorStopping =
                LoggerMessage.Define(LogLevel.Debug, LoggerEventIds.AcceptErrorStopping, "Failed to accept a request, the server is stopping.");

            private static readonly Action<ILogger, Exception?> _bindingToDefault =
                LoggerMessage.Define(LogLevel.Debug, LoggerEventIds.BindingToDefault, $"No listening endpoints were configured. Binding to {Constants.DefaultServerAddress} by default.");

            private static readonly Action<ILogger, string, Exception?> _clearedAddresses =
                LoggerMessage.Define<string>(LogLevel.Warning, LoggerEventIds.ClearedAddresses, $"Overriding address(es) '{{ServerAddresses)}}'. Binding to endpoints added to {nameof(HttpSysOptions.UrlPrefixes)} instead.");

            private static readonly Action<ILogger, string, Exception?> _clearedPrefixes =
                LoggerMessage.Define<string>(LogLevel.Warning, LoggerEventIds.ClearedPrefixes, $"Overriding endpoints added to {nameof(HttpSysOptions.UrlPrefixes)} since {nameof(IServerAddressesFeature.PreferHostingUrls)} is set to true. Binding to address(es) '{{ServerAddresses}}' instead. ");

            private static readonly Action<ILogger, Exception?> _requestListenerProcessError =
                LoggerMessage.Define(LogLevel.Error, LoggerEventIds.RequestListenerProcessError, "ProcessRequestAsync");

            private static readonly Action<ILogger, int, Exception?> _stopCancelled =
                LoggerMessage.Define<int>(LogLevel.Information, LoggerEventIds.StopCancelled, "Canceled, terminating {OutstandingRequests} request(s).");

            private static readonly Action<ILogger, int, Exception?> _waitingForRequestsToDrain =
                LoggerMessage.Define<int>(LogLevel.Information, LoggerEventIds.WaitingForRequestsToDrain, "Stopping, waiting for {OutstandingRequests} request(s) to drain.");

            public static void AcceptError(ILogger logger, Exception exception)
            {
                _acceptError(logger, exception);
            }

            public static void AcceptErrorStopping(ILogger logger, Exception exception)
            {
                _acceptErrorStopping(logger, exception);
            }

            public static void BindingToDefault(ILogger logger)
            {
                _bindingToDefault(logger, null);
            }

            public static void ClearedAddresses(ILogger logger, ICollection<string> serverAddresses)
            {
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    _clearedAddresses(logger, string.Join(", ", serverAddresses), null);
                }
            }

            public static void ClearedPrefixes(ILogger logger, ICollection<string> serverAddresses)
            {
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    _clearedPrefixes(logger, string.Join(", ", serverAddresses), null);
                }
            }

            public static void RequestListenerProcessError(ILogger logger, Exception exception)
            {
                _requestListenerProcessError(logger, exception);
            }

            public static void StopCancelled(ILogger logger, int outstandingRequests)
            {
                _stopCancelled(logger, outstandingRequests, null);
            }

            public static void WaitingForRequestsToDrain(ILogger logger, int outstandingRequests)
            {
                _waitingForRequestsToDrain(logger, outstandingRequests, null);
            }
        }
    }
}
