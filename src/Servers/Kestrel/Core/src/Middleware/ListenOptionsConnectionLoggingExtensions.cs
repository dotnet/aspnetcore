// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Hosting;

/// <summary>
/// Extensions for connection logging.
/// </summary>
public static class ListenOptionsConnectionLoggingExtensions
{
    /// <summary>
    /// Emits verbose logs for bytes read from and written to the connection.
    /// </summary>
    /// <returns>
    /// The <see cref="ListenOptions"/>.
    /// </returns>
    public static ListenOptions UseConnectionLogging(this ListenOptions listenOptions)
    {
        return listenOptions.UseConnectionLogging(loggerName: null);
    }

    /// <summary>
    /// Emits verbose logs for bytes read from and written to the connection.
    /// </summary>
    /// <returns>
    /// The <see cref="ListenOptions"/>.
    /// </returns>
    public static ListenOptions UseConnectionLogging(this ListenOptions listenOptions, string? loggerName)
    {
        var loggerFactory = listenOptions.KestrelServerOptions.ApplicationServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerName == null ? loggerFactory.CreateLogger<LoggingConnectionMiddleware>() : loggerFactory.CreateLogger(loggerName);

        listenOptions.Use(next => new LoggingConnectionMiddleware(next, logger).OnConnectionAsync);

        IMultiplexedConnectionBuilder multiplexedConnectionBuilder = listenOptions;
        multiplexedConnectionBuilder.Use(next => new LoggingMultiplexedConnectionMiddleware(next, logger).OnConnectionAsync);

        return listenOptions;
    }
}
