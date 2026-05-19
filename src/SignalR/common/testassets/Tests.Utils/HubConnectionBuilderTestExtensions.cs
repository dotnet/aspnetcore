// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SignalR.Client;

public static class HubConnectionBuilderTestExtensions
{
    // Tests want to override the built in LoggerFactory, it internally calls AddLogging
    // https://github.com/aspnet/Logging/blob/671af986ec3b46dc81e28e4a6c37a9d0ee283c65/src/Microsoft.Extensions.Logging.Testing/AssemblyTestLog.cs#L130
    public static IHubConnectionBuilder WithLoggerFactory(this IHubConnectionBuilder hubConnectionBuilder, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        hubConnectionBuilder.Services.AddSingleton(loggerFactory);
        return hubConnectionBuilder;
    }
}
