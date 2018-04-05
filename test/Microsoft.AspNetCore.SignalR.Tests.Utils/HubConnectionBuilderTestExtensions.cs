// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SignalR.Client
{
    public static class HubConnectionBuilderTestExtensions
    {
        // Tests want to override the built in LoggerFactory, it internally calls AddLogging
        // https://github.com/aspnet/Logging/blob/671af986ec3b46dc81e28e4a6c37a9d0ee283c65/src/Microsoft.Extensions.Logging.Testing/AssemblyTestLog.cs#L130
        public static IHubConnectionBuilder WithLoggerFactory(this IHubConnectionBuilder hubConnectionBuilder, ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            hubConnectionBuilder.Services.AddSingleton(loggerFactory);
            return hubConnectionBuilder;
        }
    }
}