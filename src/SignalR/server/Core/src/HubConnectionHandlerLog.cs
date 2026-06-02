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

    [LoggerMessage(7, LogLevel.Warning, "Authentication refresh produced a different user identifier (old: '{PreviousUserIdentifier}', new: '{NewUserIdentifier}'), but the hub lifetime manager does not support changing a connection's user identifier. Aborting the connection.", EventName = "UserIdentifierChangedOnRefresh")]
    public static partial void UserIdentifierChangedOnRefresh(ILogger logger, string? previousUserIdentifier, string? newUserIdentifier);

    [LoggerMessage(8, LogLevel.Debug, "Authentication refresh changed the user identifier (old: '{PreviousUserIdentifier}', new: '{NewUserIdentifier}'). Re-keyed the connection's user-targeted routing.", EventName = "UserIdentifierRekeyedOnRefresh")]
    public static partial void UserIdentifierRekeyedOnRefresh(ILogger logger, string? previousUserIdentifier, string? newUserIdentifier);

    [LoggerMessage(9, LogLevel.Error, "Failed to re-key the connection's user-targeted routing after an authentication refresh changed the user identifier (old: '{PreviousUserIdentifier}', new: '{NewUserIdentifier}'). Aborting the connection.", EventName = "UserIdentifierRekeyFailed")]
    public static partial void UserIdentifierRekeyFailed(ILogger logger, string? previousUserIdentifier, string? newUserIdentifier, Exception exception);
}
