// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.HttpSys.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.HttpSys;

internal partial class MessagePump
{
    private static partial class Log
    {
        [LoggerMessage(LoggerEventIds.AcceptError, LogLevel.Error, "Failed to accept a request.", EventName = "AcceptError")]
        public static partial void AcceptError(ILogger logger, Exception exception);

        [LoggerMessage(LoggerEventIds.AcceptErrorStopping, LogLevel.Debug, "Failed to accept a request, the server is stopping.", EventName = "AcceptErrorStopping")]
        public static partial void AcceptErrorStopping(ILogger logger, Exception exception);

        [LoggerMessage(LoggerEventIds.BindingToDefault, LogLevel.Debug, $"No listening endpoints were configured. Binding to {Constants.DefaultServerAddress} by default.", EventName = "BindingToDefault")]
        public static partial void BindingToDefault(ILogger logger);

        public static void ClearedAddresses(ILogger logger, ICollection<string> serverAddresses)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                ClearedAddressesCore(logger, string.Join(", ", serverAddresses));
            }
        }

        [LoggerMessage(LoggerEventIds.ClearedAddresses, LogLevel.Warning,
            $"Overriding address(es) '{{ServerAddresses}}'. Binding to endpoints added to {nameof(HttpSysOptions.UrlPrefixes)} instead.",
            EventName = "ClearedAddresses",
            SkipEnabledCheck = true)]
        private static partial void ClearedAddressesCore(ILogger logger, string serverAddresses);

        public static void ClearedPrefixes(ILogger logger, ICollection<string> serverAddresses)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                ClearedPrefixesCore(logger, string.Join(", ", serverAddresses));
            }
        }

        [LoggerMessage(LoggerEventIds.ClearedPrefixes, LogLevel.Warning,
            $"Overriding endpoints added to {nameof(HttpSysOptions.UrlPrefixes)} since {nameof(IServerAddressesFeature.PreferHostingUrls)} is set to true. Binding to address(es) '{{ServerAddresses}}' instead.",
            EventName = "ClearedPrefixes",
            SkipEnabledCheck = true)]
        private static partial void ClearedPrefixesCore(ILogger logger, string serverAddresses);

        [LoggerMessage(LoggerEventIds.RequestListenerProcessError, LogLevel.Error, "ProcessRequestAsync", EventName = "RequestListenerProcessError")]
        public static partial void RequestListenerProcessError(ILogger logger, Exception exception);

        [LoggerMessage(LoggerEventIds.StopCancelled, LogLevel.Information, "Canceled, terminating {OutstandingRequests} request(s).", EventName = "StopCancelled")]
        public static partial void StopCancelled(ILogger logger, int outstandingRequests);

        [LoggerMessage(LoggerEventIds.WaitingForRequestsToDrain, LogLevel.Information, "Stopping, waiting for {OutstandingRequests} request(s) to drain.", EventName = "WaitingForRequestsToDrain")]
        public static partial void WaitingForRequestsToDrain(ILogger logger, int outstandingRequests);
    }
}
