// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for <see cref="ISignalRBuilder"/>.
    /// </summary>
    public static class MessagePackProtocolDependencyInjectionExtensions
    {
        /// <summary>
        /// Enables the MessagePack protocol for SignalR.
        /// </summary>
        /// <remarks>
        /// This has no effect if the MessagePack protocol has already been enabled.
        /// </remarks>
        /// <param name="builder">The <see cref="ISignalRBuilder"/> representing the SignalR server to add MessagePack protocol support to.</param>
        /// <returns>The value of <paramref name="builder"/></returns>
        public static TBuilder AddMessagePackProtocol<TBuilder>(this TBuilder builder) where TBuilder : ISignalRBuilder
            => AddMessagePackProtocol(builder, _ => { });

        /// <summary>
        /// Enables the MessagePack protocol for SignalR and allows options for the MessagePack protocol to be configured.
        /// </summary>
        /// <remarks>
        /// Any options configured here will be applied, even if the MessagePack protocol has already been registered.
        /// </remarks>
        /// <param name="builder">The <see cref="ISignalRBuilder"/> representing the SignalR server to add MessagePack protocol support to.</param>
        /// <param name="configure">A delegate that can be used to configure the <see cref="MessagePackHubProtocolOptions"/></param>
        /// <returns>The value of <paramref name="builder"/></returns>
        public static TBuilder AddMessagePackProtocol<TBuilder>(this TBuilder builder, Action<MessagePackHubProtocolOptions> configure) where TBuilder : ISignalRBuilder
        {
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IHubProtocol, MessagePackHubProtocol>());
            builder.Services.Configure(configure);
            return builder;
        }
    }
}
