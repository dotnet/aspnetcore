// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.SignalR.Client
{
    public static class HubConnectionBuilderExtensions
    {
        public static IHubConnectionBuilder WithHubProtocol(this IHubConnectionBuilder hubConnectionBuilder, IHubProtocol hubProtocol)
        {
            hubConnectionBuilder.AddSetting(HubConnectionBuilderDefaults.HubProtocolKey, hubProtocol);
            return hubConnectionBuilder;
        }

        public static IHubConnectionBuilder WithJsonProtocol(this IHubConnectionBuilder hubConnectionBuilder)
        {
            return hubConnectionBuilder.WithHubProtocol(new JsonHubProtocol());
        }

        public static IHubConnectionBuilder WithJsonProtocol(this IHubConnectionBuilder hubConnectionBuilder, JsonHubProtocolOptions options)
        {
            return hubConnectionBuilder.WithHubProtocol(new JsonHubProtocol(Options.Create(options)));
        }

        public static IHubConnectionBuilder WithLoggerFactory(this IHubConnectionBuilder hubConnectionBuilder, ILoggerFactory loggerFactory)
        {
            hubConnectionBuilder.AddSetting(HubConnectionBuilderDefaults.LoggerFactoryKey,
                loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory)));
            return hubConnectionBuilder;
        }

        public static IHubConnectionBuilder WithLogger(this IHubConnectionBuilder hubConnectionBuilder, Action<ILoggerFactory> configureLogging)
        {
            var loggerFactory = hubConnectionBuilder.GetLoggerFactory() ?? new LoggerFactory();
            configureLogging(loggerFactory);
            return hubConnectionBuilder.WithLoggerFactory(loggerFactory);
        }

        public static ILoggerFactory GetLoggerFactory(this IHubConnectionBuilder hubConnectionBuilder)
        {
            hubConnectionBuilder.TryGetSetting<ILoggerFactory>(HubConnectionBuilderDefaults.LoggerFactoryKey, out var loggerFactory);
            return loggerFactory;
        }

        public static IHubProtocol GetHubProtocol(this IHubConnectionBuilder hubConnectionBuilder)
        {
            hubConnectionBuilder.TryGetSetting<IHubProtocol>(HubConnectionBuilderDefaults.HubProtocolKey, out var hubProtocol);
            return hubProtocol;
        }
    }
}
