// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SignalR.Redis.Internal
{
    internal static class RedisLoggerExtensions
    {
        // Category: RedisHubLifetimeManager<THub>
        private static readonly Action<ILogger, string, Exception> _connectingToEndpoints =
            LoggerMessage.Define<string>(LogLevel.Information, new EventId(0, nameof(ConnectingToEndpoints)), "Connecting to Redis endpoints: {endpoints}.");

        private static readonly Action<ILogger, Exception> _connected =
            LoggerMessage.Define(LogLevel.Information, new EventId(1, nameof(Connected)), "Connected to Redis.");

        private static readonly Action<ILogger, string, Exception> _subscribing =
            LoggerMessage.Define<string>(LogLevel.Trace, new EventId(2, nameof(Subscribing)), "Subscribing to channel: {channel}.");

        private static readonly Action<ILogger, string, Exception> _receivedFromChannel =
            LoggerMessage.Define<string>(LogLevel.Trace, new EventId(3, nameof(ReceivedFromChannel)), "Received message from Redis channel {channel}.");

        private static readonly Action<ILogger, string, Exception> _publishToChannel =
            LoggerMessage.Define<string>(LogLevel.Trace, new EventId(4, nameof(PublishToChannel)), "Publishing message to Redis channel {channel}.");

        private static readonly Action<ILogger, string, Exception> _unsubscribe =
            LoggerMessage.Define<string>(LogLevel.Trace, new EventId(5, nameof(Unsubscribe)), "Unsubscribing from channel: {channel}.");

        public static void ConnectingToEndpoints(this ILogger logger, string endpoints)
        {
            _connectingToEndpoints(logger, endpoints, null);
        }

        public static void Connected(this ILogger logger)
        {
            _connected(logger, null);
        }

        public static void Subscribing(this ILogger logger, string channelName)
        {
            _subscribing(logger, channelName, null);
        }

        public static void ReceivedFromChannel(this ILogger logger, string channelName)
        {
            _receivedFromChannel(logger, channelName, null);
        }

        public static void PublishToChannel(this ILogger logger, string channelName)
        {
            _publishToChannel(logger, channelName, null);
        }

        public static void Unsubscribe(this ILogger logger, string channelName)
        {
            _unsubscribe(logger, channelName, null);
        }
    }
}