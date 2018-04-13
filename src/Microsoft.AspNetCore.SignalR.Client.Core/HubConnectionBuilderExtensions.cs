// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.SignalR.Client
{
    public static class HubConnectionBuilderExtensions
    {
        public static IHubConnectionBuilder WithConnectionFactory(this IHubConnectionBuilder hubConnectionBuilder, Func<TransferFormat, Task<ConnectionContext>> connectionFactory)
        {
            if (connectionFactory == null)
            {
                throw new ArgumentNullException(nameof(connectionFactory));
            }
            hubConnectionBuilder.Services.AddSingleton<IConnectionFactory>(new DelegateConnectionFactory(connectionFactory));
            return hubConnectionBuilder;
        }

        public static IHubConnectionBuilder WithHubProtocol(this IHubConnectionBuilder hubConnectionBuilder, IHubProtocol hubProtocol)
        {
            hubConnectionBuilder.Services.AddSingleton(hubProtocol);
            return hubConnectionBuilder;
        }

        public static IHubConnectionBuilder WithLogging(this IHubConnectionBuilder hubConnectionBuilder, Action<ILoggingBuilder> configureLogging)
        {
            hubConnectionBuilder.Services.AddLogging(configureLogging);
            return hubConnectionBuilder;
        }

        private class DelegateConnectionFactory : IConnectionFactory
        {
            private readonly Func<TransferFormat, Task<ConnectionContext>> _connectionFactory;

            public DelegateConnectionFactory(Func<TransferFormat, Task<ConnectionContext>> connectionFactory)
            {
                _connectionFactory = connectionFactory;
            }

            public Task<ConnectionContext> ConnectAsync(TransferFormat transferFormat)
            {
                return _connectionFactory(transferFormat);
            }
        }
    }
}