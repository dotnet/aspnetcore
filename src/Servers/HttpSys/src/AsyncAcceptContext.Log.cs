// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.HttpSys;

internal partial class AsyncAcceptContext
{
    private static partial class Log
    {
        [LoggerMessage(LoggerEventIds.AcceptSetResultFailed, LogLevel.Error, "Error attempting to set 'accept' outcome", EventName = "AcceptSetResultFailed")]
        public static partial void AcceptSetResultFailed(ILogger logger, Exception exception);

        // note on "critical": these represent an unexpected IO callback state that needs investigation; see https://github.com/dotnet/aspnetcore/pull/54368/

        [LoggerMessage(LoggerEventIds.AcceptSetExpectationMismatch, LogLevel.Critical, "Mismatch setting callback expectation - {Value}", EventName = "AcceptSetExpectationMismatch")]
        public static partial void AcceptSetExpectationMismatch(ILogger logger, int value);

        [LoggerMessage(LoggerEventIds.AcceptCancelExpectationMismatch, LogLevel.Critical, "Mismatch canceling accept state - {Value}", EventName = "AcceptCancelExpectationMismatch")]
        public static partial void AcceptCancelExpectationMismatch(ILogger logger, int value);

        [LoggerMessage(LoggerEventIds.AcceptObserveExpectationMismatch, LogLevel.Critical, "Mismatch observing {Kind} accept callback - {Value}", EventName = "AcceptObserveExpectationMismatch")]
        public static partial void AcceptObserveExpectationMismatch(ILogger logger, string kind, int value);
    }
}
