// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Microsoft.AspNetCore.SignalR.StackExchangeRedis.Internal;

// We don't want to use our nested static class here because RedisHubLifetimeManager is generic.
// We'd end up creating separate instances of all the LoggerMessage.Define values for each Hub.
internal static partial class RedisLog
{
    public static void ConnectingToEndpoints(ILogger logger, EndPointCollection endpoints, string serverName)
    {
        if (logger.IsEnabled(LogLevel.Information) && endpoints.Count > 0)
        {
            ConnectingToEndpoints(logger, string.Join(", ", endpoints.Select(EndPointCollection.ToString)), serverName);
        }
    }

    [LoggerMessage(1, LogLevel.Information, "Connecting to Redis endpoints: {Endpoints}. Using Server Name: {ServerName}", EventName = "ConnectingToEndpoints")]
    private static partial void ConnectingToEndpoints(ILogger logger, string endpoints, string serverName);

    [LoggerMessage(2, LogLevel.Information, "Connected to Redis.", EventName = "Connected")]
    public static partial void Connected(ILogger logger);

    [LoggerMessage(3, LogLevel.Trace, "Subscribing to channel: {Channel}.", EventName = "Subscribing")]
    public static partial void Subscribing(ILogger logger, string channel);

    [LoggerMessage(4, LogLevel.Trace, "Received message from Redis channel {Channel}.", EventName = "ReceivedFromChannel")]
    public static partial void ReceivedFromChannel(ILogger logger, string channel);

    [LoggerMessage(5, LogLevel.Trace, "Publishing message to Redis channel {Channel}.", EventName = "PublishToChannel")]
    public static partial void PublishToChannel(ILogger logger, string channel);

    [LoggerMessage(6, LogLevel.Trace, "Unsubscribing from channel: {Channel}.", EventName = "Unsubscribe")]
    public static partial void Unsubscribe(ILogger logger, string channel);

    [LoggerMessage(7, LogLevel.Error, "Not connected to Redis.", EventName = "NotConnected")]
    public static partial void NotConnected(ILogger logger);

    [LoggerMessage(8, LogLevel.Information, "Connection to Redis restored.", EventName = "ConnectionRestored")]
    public static partial void ConnectionRestored(ILogger logger);

    [LoggerMessage(9, LogLevel.Error, "Connection to Redis failed.", EventName = "ConnectionFailed")]
    public static partial void ConnectionFailed(ILogger logger, Exception exception);

    [LoggerMessage(10, LogLevel.Debug, "Failed writing message.", EventName = "FailedWritingMessage")]
    public static partial void FailedWritingMessage(ILogger logger, Exception exception);

    [LoggerMessage(11, LogLevel.Warning, "Error processing message for internal server message.", EventName = "InternalMessageFailed")]
    public static partial void InternalMessageFailed(ILogger logger, Exception exception);

    [LoggerMessage(12, LogLevel.Error, "Received a client result for protocol {HubProtocol} which is not supported by this server. This likely means you have different versions of your server deployed.", EventName = "MismatchedServers")]
    public static partial void MismatchedServers(ILogger logger, string hubProtocol);

    [LoggerMessage(13, LogLevel.Error, "Error forwarding client result with ID '{InvocationID}' to server.", EventName = "ErrorForwardingResult")]
    public static partial void ErrorForwardingResult(ILogger logger, string invocationId, Exception ex);

    [LoggerMessage(14, LogLevel.Error, "Error connecting to Redis.", EventName = "ErrorConnecting")]
    public static partial void ErrorConnecting(ILogger logger, Exception ex);

    [LoggerMessage(15, LogLevel.Warning, "Error parsing client result with protocol {HubProtocol}.", EventName = "ErrorParsingResult")]
    public static partial void ErrorParsingResult(ILogger logger, string hubProtocol, Exception? ex);

    // This isn't DefineMessage-based because it's just the simple TextWriter logging from ConnectionMultiplexer
    public static void ConnectionMultiplexerMessage(ILogger logger, string? message)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            // We tag it with EventId 100 though so it can be pulled out of logs easily.
            logger.LogDebug(new EventId(100, "RedisConnectionLog"), message);
        }
    }
}
