// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.SignalR.Client
{
    public static class HubConnectionBuilderExtensions
    {
        public static IHubConnectionBuilder WithConnectionFactory(this IHubConnectionBuilder hubConnectionBuilder, Func<IConnection> connectionFactory)
        {
            hubConnectionBuilder.Services.AddSingleton(connectionFactory);
            return hubConnectionBuilder;
        }

        public static IHubConnectionBuilder WithHubProtocol(this IHubConnectionBuilder hubConnectionBuilder, IHubProtocol hubProtocol)
        {
            hubConnectionBuilder.Services.AddSingleton(hubProtocol);
            return hubConnectionBuilder;
        }

        public static IHubConnectionBuilder WithLoggerFactory(this IHubConnectionBuilder hubConnectionBuilder, ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            hubConnectionBuilder.Services.AddSingleton(loggerFactory);
            return hubConnectionBuilder;
        }

        public static IHubConnectionBuilder WithLogger(this IHubConnectionBuilder hubConnectionBuilder, Action<ILoggerFactory> configureLogging)
        {
            var loggerFactory = new LoggerFactory();
            configureLogging(loggerFactory);
            return hubConnectionBuilder.WithLoggerFactory(loggerFactory);
        }
    }
}