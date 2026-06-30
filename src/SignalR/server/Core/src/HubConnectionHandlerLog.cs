// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SignalR;

internal static partial class HubConnectionHandlerLog
{
    [LoggerMessage(1, LogLevel.Error, "Error when dispatching '{HubMethod}' on hub.", EventName = "ErrorDispatchingHubEvent")]
    public static partial void ErrorDispatchingHubEvent(ILogger logger, string hubMethod, Exception exception);

    [LoggerMessage(2, LogLevel.Debug, "Error when processing requests.", EventName = "ErrorProcessingRequest")]
    public static partial void ErrorProcessingRequest(ILogger logger, Exception exception);

    [LoggerMessage(3, LogLevel.Trace, "Abort callback failed.", EventName = "AbortFailed")]
    public static partial void AbortFailed(ILogger logger, Exception exception);

    [LoggerMessage(4, LogLevel.Debug, "Error when sending Close message.", EventName = "ErrorSendingClose")]
    public static partial void ErrorSendingClose(ILogger logger, Exception exception);

    [LoggerMessage(5, LogLevel.Debug, "OnConnectedAsync started.", EventName = "ConnectedStarting")]
    public static partial void ConnectedStarting(ILogger logger);

    [LoggerMessage(6, LogLevel.Debug, "OnConnectedAsync ending.", EventName = "ConnectedEnding")]
    public static partial void ConnectedEnding(ILogger logger);

    [LoggerMessage(7, LogLevel.Warning, "Authentication refresh produced a different user identifier (old: '{PreviousUserIdentifier}', new: '{NewUserIdentifier}'). Changing a connection's user identifier during refresh is not supported, so the connection is aborted.", EventName = "UserIdentifierChangedOnRefresh")]
    public static partial void UserIdentifierChangedOnRefresh(ILogger logger, string? previousUserIdentifier, string? newUserIdentifier);

    [LoggerMessage(8, LogLevel.Error, "Error when applying refreshed authentication state.", EventName = "ErrorApplyingAuthenticationRefresh")]
    public static partial void ErrorApplyingAuthenticationRefresh(ILogger logger, Exception exception);
}
