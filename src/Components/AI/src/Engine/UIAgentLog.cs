// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.AI;

internal static partial class UIAgentLog
{
    [LoggerMessage(100, LogLevel.Information, "SendMessageAsync: Streaming assistant response")]
    internal static partial void StreamingAssistantResponse(ILogger logger);

    [LoggerMessage(101, LogLevel.Debug, "SendMessageAsync: Received update #{Index} Role={Role}, ContentTypes=[{ContentTypes}]")]
    internal static partial void ReceivedUpdate(ILogger logger, int index, string? role, string contentTypes);

    [LoggerMessage(102, LogLevel.Information, "SendMessageAsync: Stream complete. Total updates={UpdateCount}")]
    internal static partial void StreamComplete(ILogger logger, int updateCount);

    [LoggerMessage(103, LogLevel.Debug, "SendMessageAsync: Added {MessageCount} messages to history")]
    internal static partial void AddedToHistory(ILogger logger, int messageCount);
}
