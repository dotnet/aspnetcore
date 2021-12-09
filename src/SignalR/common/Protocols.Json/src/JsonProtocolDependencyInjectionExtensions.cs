// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for <see cref="ISignalRBuilder"/>.
/// </summary>
public static class JsonProtocolDependencyInjectionExtensions
{
    /// <summary>
    /// Enables the JSON protocol for SignalR.
    /// </summary>
    /// <remarks>
    /// This has no effect if the JSON protocol has already been enabled.
    /// </remarks>
    /// <param name="builder">The <see cref="ISignalRBuilder"/> representing the SignalR server to add JSON protocol support to.</param>
    /// <returns>The value of <paramref name="builder"/></returns>
    public static TBuilder AddJsonProtocol<TBuilder>(this TBuilder builder) where TBuilder : ISignalRBuilder
        => AddJsonProtocol(builder, _ => { });

    /// <summary>
    /// Enables the JSON protocol for SignalR and allows options for the JSON protocol to be configured.
    /// </summary>
    /// <remarks>
    /// Any options configured here will be applied, even if the JSON protocol has already been registered with the server.
    /// </remarks>
    /// <param name="builder">The <see cref="ISignalRBuilder"/> representing the SignalR server to add JSON protocol support to.</param>
    /// <param name="configure">A delegate that can be used to configure the <see cref="JsonHubProtocolOptions"/></param>
    /// <returns>The value of <paramref name="builder"/></returns>
    public static TBuilder AddJsonProtocol<TBuilder>(this TBuilder builder, Action<JsonHubProtocolOptions> configure) where TBuilder : ISignalRBuilder
    {
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IHubProtocol, JsonHubProtocol>());
        builder.Services.Configure(configure);
        return builder;
    }
}
